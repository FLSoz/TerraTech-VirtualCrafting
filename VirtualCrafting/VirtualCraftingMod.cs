using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTCustomNetworkingWrapper;
using UnityEngine;
using VirtualCrafting.JSONLoaders;
using VirtualCrafting.Crafting;
using VirtualCrafting.Model;
using VirtualCrafting.ModdedContent;
using VirtualCrafting.Networking;

namespace VirtualCrafting
{
    public class VirtualCraftingMod : ModBase
    {
        internal const string HarmonyID = "flsoz.ttsmm.virtual.crafting.mod";
        internal static readonly Harmony harmony = new Harmony(HarmonyID);
        private static bool Inited = false;

        internal static bool DEBUG = true;

        internal static Logger logger;
        internal static void ConfigureLogger()
        {
            logger = new Logger("VirtualCrafting");
            logger.Info("Logger is setup");
        }

        public override void EarlyInit()
        {
            if (!Inited)
            {
                Inited = true;
                ConfigureLogger();

                // Custom Networking
                CustomNetworkingWrapper<VirtualCraftingMessage> wrapper = ManCustomNetHandler.GetNetworkingWrapper<VirtualCraftingMessage>(
                    "VirtualCrafting", NetworkingManager.ReceiveAsClient, NetworkingManager.ReceiveAsHost
                );
                ManCustomNetHandler.RegisterNetworkingWrapper(wrapper);

                // setup
                Singleton.instance.gameObject.AddComponent<ManVirtualCrafting>();
                Singleton.instance.gameObject.AddComponent<ManVirtualModdedContent>();
                Singleton.Manager<ManVirtualModdedContent>.inst.Setup();
            }
        }

        public override bool HasEarlyInit()
        {
            return true;
        }

        public override void Init()
        {
            harmony.PatchAll();
            Singleton.Manager<ManVirtualModdedContent>.inst.Init();
            JSONBlockLoader.RegisterModuleLoader(new JSONVirtualChunkSourceLoader());
            JSONBlockLoader.RegisterModuleLoader(new JSONChunkVirtualizerLoader());
        }

        public override void DeInit()
        {
            harmony.UnpatchAll(HarmonyID);
            Singleton.Manager<ManVirtualModdedContent>.inst.Reset();
        }

        public static int LateInitOrder = 10;

        public void LateInit()
        {
            Singleton.Manager<ManVirtualModdedContent>.inst.LateInit();
        }
    }
}
