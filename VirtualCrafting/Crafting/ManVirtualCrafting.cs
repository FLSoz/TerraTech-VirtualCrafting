using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using VirtualCrafting.Model;
using VirtualCrafting.Inventory;

namespace VirtualCrafting.Crafting
{
    public class ManVirtualCrafting : Singleton.Manager<ManVirtualCrafting>, Mode.IManagerModeEvents
    {
        public bool Active { get; private set; }

        public NetPlayer ViewingPlayer { get; private set; }

        internal readonly CraftingOrderResolver orderResolver = new CraftingOrderResolver();

        internal VirtualNetInventory m_SharedInventory = null;

        public IVirtualInventory Inventory { get => this.m_SaveData.m_Inventory; }

        #region Patches
        // We enable this in Campaign, RnD, CoOp Campaign, and that's it
        internal static class PatchModeHandling
        {
            [HarmonyPatch(typeof(ModeMain), "SetupModeLoadSaveListeners")]
            internal static class PatchCampaignSetup {
                [HarmonyPostfix]
                internal static void Postfix(ModeMain __instance) { __instance.SubscribeToEvents(Singleton.Manager<ManVirtualCrafting>.inst); }
            }
            [HarmonyPatch(typeof(ModeMisc), "SetupModeLoadSaveListeners")]
            internal static class PatchRnDSetup
            {
                [HarmonyPostfix]
                internal static void Postfix(ModeMisc __instance) { __instance.SubscribeToEvents(Singleton.Manager<ManVirtualCrafting>.inst); }
            }
            [HarmonyPatch(typeof(ModeCoOpCampaign), "SetupModeLoadSaveListeners")]
            internal static class PatchCoOpSetup
            {
                [HarmonyPostfix]
                internal static void Postfix(ModeCoOpCampaign __instance) { __instance.SubscribeToEvents(Singleton.Manager<ManVirtualCrafting>.inst); }
            }
            [HarmonyPatch(typeof(ModeMain), "CleanupModeLoadSaveListeners")]
            internal static class PatchCampaignCleanup
            {
                [HarmonyPostfix]
                internal static void Postfix(ModeMain __instance) { __instance.UnsubscribeFromEvents(Singleton.Manager<ManVirtualCrafting>.inst); }
            }
            [HarmonyPatch(typeof(ModeMisc), "CleanupModeLoadSaveListeners")]
            internal static class PatchRnDCleanup
            {
                [HarmonyPostfix]
                internal static void Postfix(ModeMisc __instance) { __instance.UnsubscribeFromEvents(Singleton.Manager<ManVirtualCrafting>.inst); }
            }
            [HarmonyPatch(typeof(ModeCoOpCampaign), "CleanupModeLoadSaveListeners")]
            internal static class PatchCoOpCleanup
            {
                [HarmonyPostfix]
                internal static void Postfix(ModeCoOpCampaign __instance) { __instance.UnsubscribeFromEvents(Singleton.Manager<ManVirtualCrafting>.inst); }
            }
        }

        // We don't just hijack the network prefab, because that would also impact PvP, which we do not want
        [HarmonyPatch(typeof(ModeCoOpCampaign), "OnServerHostStarted")]
        internal static class PatchCoOpCampaignStartup {
            internal static void SetupNetPrefab(GameObject netPrefab)
            {
                VirtualNetInventory virtualInventory = netPrefab.GetComponent<VirtualNetInventory>();
                if (virtualInventory == null)
                {
                    virtualInventory = netPrefab.AddComponent<VirtualNetInventory>();
                }
                NetInventory netInventory = netPrefab.GetComponent<NetInventory>();
                virtualInventory.BlockInventory = netInventory;
                virtualInventory.ServerSetIsSharedInventory(true);
                Singleton.Manager<ManVirtualCrafting>.inst.m_SharedInventory = virtualInventory;
            }

            // We add our own VirtualNetInventory to the object on server startup
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> newInstructions = new List<CodeInstruction>();
                // We also need the last 3 instructions before the call to NetworkServer::Spawn in order to get the GameObject to add our stuff to
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Call && (MethodInfo) instruction.operand == AccessTools.Method(typeof(NetworkServer), "Spawn", new Type[] { typeof(GameObject) }))
                    {
                        // Add our setup call
                        newInstructions.Add(new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo((GameObject prefab) => SetupNetPrefab(prefab))));

                        // duplicate the process to get our net prefab
                        for (int i = 4; i > 1; i--)
                        {
                            newInstructions.Add(newInstructions[newInstructions.Count - i]);
                        }
                    }
                    newInstructions.Add(instruction);
                }
                return newInstructions;
            }
        }
        #endregion

        internal void SetNetInventory(NetPlayer netPlayer, VirtualNetInventory inventory)
        {
            if (Singleton.Manager<ManNetwork>.inst.IsServer && inventory.IsNotNull())
            {
                VirtualCraftingMod.logger.Assert(this.m_SharedInventory == null, "Should only call NetPlayer.SetInventory once on the server");
            }
            if (netPlayer.IsActuallyLocalPlayer)
            {
                if (this.m_SharedInventory != null)
                {
                    netPlayer.Inventory.InventoryChanged.Unsubscribe(this.m_SharedInventory.OnBlockInventoryChanged);
                    this.m_SharedInventory.UnsubscribeToInventoryChanged(this.OnInventoryChangedListener);
                }

                // this will overwrite shared inventory multiple times, but it should be okay because it will always be the same obj
                this.m_SharedInventory = inventory;

                if (this.m_SharedInventory != null)
                {
                    // Subscribe ourselves to the underlying inventory change
                    netPlayer.Inventory.InventoryChanged.Subscribe(this.m_SharedInventory.OnBlockInventoryChanged);
                    this.m_SharedInventory.SubscribeToInventoryChanged(this.OnInventoryChangedListener);
                }
            }
        }

        internal void SetSharedInventory(VirtualNetInventory inventory)
        {
            VirtualCraftingMod.logger.Assert(!Singleton.Manager<ManNetwork>.inst.IsServer, "This should be client only");
            // this.m_SharedInventory = inventory;
            if (Singleton.Manager<ManNetwork>.inst.MyPlayer.IsNotNull())
            {
                VirtualCraftingMod.logger.Info("Fixing up player inventory.");
                this.SetNetInventory(Singleton.Manager<ManNetwork>.inst.MyPlayer, inventory);
            }
        }

        #region Mode interface

        internal Event<IVirtualItemDescriptor, int> OnInventoryChanged;
        internal void OnInventoryChangedListener(IVirtualItemDescriptor itemType, int quantity)
        {
            this.OnInventoryChanged.Send(itemType, quantity);
        }

        private static readonly FieldInfo m_CurrentMode = AccessTools.Field(typeof(ManGameMode), "m_CurrentMode");
        private static readonly FieldInfo m_SaveDataJSON = AccessTools.Field(typeof(ManSaveGame.State), "m_SaveDataJSON");
        private static readonly FieldInfo s_JSONSerialisationSettings = AccessTools.Field(typeof(ManSaveGame), "s_JSONSerialisationSettings");
        private const string SaveJSONIdentifier = "VCSPInventory";

        // We are assuming gamemode events happen after Client/Server is setup
        public void ModeStart(ManSaveGame.State optionalLoadState)
        {
            Active = true;
            ManVirtualCrafting.SaveData saveData = null;
            if (optionalLoadState != null)
            {
                Mode currMode = (Mode)m_CurrentMode.GetValue(Singleton.Manager<ManGameMode>.inst);
                if (currMode.IsMultiplayer)
                {
                    if (Singleton.Manager<ManNetwork>.inst.IsServer)
                    {
                        this.m_SharedInventory.Load(optionalLoadState);
                    }
                }
                else
                {
                    Dictionary<string, string> saveJSON = (Dictionary<string, string>)m_SaveDataJSON.GetValue(optionalLoadState);
                    if (saveJSON.TryGetValue(SaveJSONIdentifier, out string text) && !text.NullOrEmpty())
                    {
                        try
                        {
                            JsonSerializerSettings serializerSettings = (JsonSerializerSettings)s_JSONSerialisationSettings.GetValue(null);
                            saveData = JsonConvert.DeserializeObject<SaveData>(text, serializerSettings);
                        }
                        catch (Exception ex)
                        {
                            VirtualCraftingMod.logger.Error(ex, "Exception when trying to load save data");
                        }
                        return;
                    }
                    else if (text.NullOrEmpty())
                    {
                        VirtualCraftingMod.logger.Error("Loaded null or empty VC save data");
                    }

                    if (saveData != null)
                    {
                        this.m_SaveData = saveData;
                        return;
                    }
                }
            }
            this.m_SaveData = new ManVirtualCrafting.SaveData();
        }

        public void ModeExit()
        {
            this.Reset();
            Active = false;
            this.Inventory.UnsubscribeToInventoryChanged(this.OnInventoryChangedListener);
        }

        public void Save(ManSaveGame.State saveState)
        {
            Dictionary<string, string> saveJSON = (Dictionary<string, string>)m_SaveDataJSON.GetValue(saveState);
            try
            {
                JsonSerializerSettings serializerSettings = (JsonSerializerSettings)s_JSONSerialisationSettings.GetValue(null);
                string text = JsonConvert.SerializeObject(this.m_SaveData, serializerSettings);
                saveJSON.Add(SaveJSONIdentifier, text);
                return;
            }
            catch (Exception ex)
            {
                VirtualCraftingMod.logger.Error(ex, "Exception when trying to save data");
            }
            saveJSON.Add(SaveJSONIdentifier, "");

            if (this.m_SharedInventory != null)
            {
                this.m_SharedInventory.Save(saveState);
            }
        }

        public void Reset()
        {
            this.m_SaveData = new ManVirtualCrafting.SaveData();
            this.m_SharedInventory = null;
        }
        #endregion

        private ManVirtualCrafting.SaveData m_SaveData = new ManVirtualCrafting.SaveData();

        private class SaveData
        {
            [JsonConverter(typeof(VirtualInventoryJSONConverter))]
            public IVirtualInventory m_Inventory = new VirtualInventory();
        }
    }
}
