using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VirtualCrafting.Model;
using VirtualCrafting.ModdedContent;
using VirtualCrafting.Crafting;

namespace VirtualCrafting.Inventory
{
    internal class VirtualNetInventory : NetworkBehaviour, IVirtualInventory
    {
        #region NetBehaviour stuff
        public bool IsSharedInventory { get; private set; }

        [Server]
        public void ServerSetIsSharedInventory(bool set)
        {
            if (!NetworkServer.active)
            {
                VirtualCraftingMod.logger.Warn("[Server] function 'System.Void NetVirtualInventory::ServerSetIsSharedInventory(System.Boolean)' called on client");
                return;
            }
            this.IsSharedInventory = set;
        }

        [Server]
        public void OnServerRegisterUser(NetPlayer player)
        {
            if (!NetworkServer.active)
            {
                VirtualCraftingMod.logger.Warn("[Server] function 'System.Void NetVirtualInventory::OnServerRegisterUser(NetPlayer)' called on client");
                return;
            }
            this.m_Players.Add(player.PlayerID);
            base.SetDirtyBit(1U);
        }

        [Server]
        public void OnServerUnregisterUser(NetPlayer player)
        {
            if (!NetworkServer.active)
            {
                VirtualCraftingMod.logger.Warn("[Server] function 'System.Void NetVirtualInventory::OnServerUnregisterUser(NetPlayer)' called on client");
                return;
            }
            this.m_Players.Remove(player.PlayerID);
            base.SetDirtyBit(1U);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!Singleton.Manager<ManNetwork>.inst.IsServer)
            {
                // should be the only time we see this, so this should always be true
                if (Singleton.Manager<ManGameMode>.inst.IsCurrent<ModeCoOpCampaign>() && this.IsSharedInventory)
                {
                    Singleton.Manager<ManVirtualCrafting>.inst.SetSharedInventory(this);
                }
                this.UpdateSubscribedPlayers();
            }
        }

        private void UpdateSubscribedPlayers()
        {
            foreach (int playerID in this.m_Players)
            {
                NetPlayer netPlayer = Singleton.Manager<ManNetwork>.inst.FindPlayerByPlayerID(playerID);
                if (netPlayer != null)
                {
                    Singleton.Manager<ManVirtualCrafting>.inst.SetNetInventory(netPlayer, this);
                }
            }
        }

        // Uses Unity NetworkBehaviour to serialize data across all clients?
        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            uint num = initialState ? uint.MaxValue : base.syncVarDirtyBits;
            writer.Write(num);
            if ((num & 1U) != 0U)
            {
                writer.Write(this.m_Players.Count);
                for (int i = 0; i < this.m_Players.Count; i++)
                {
                    writer.Write(this.m_Players[i]);
                }
            }
            if ((num & 2U) != 0U)
            {
                writer.Write(this.m_ItemReservations.Count);
                foreach (int netPlayerID in this.m_ItemReservations.Keys)
                {
                    writer.Write(netPlayerID);
                    IVirtualItemDescriptor descriptor = this.m_ItemReservations[netPlayerID];
                    writer.Write(descriptor.ID);
                }
            }
            if ((num & 4U) != 0U)
            {
                if (initialState)
                {
                    writer.Write(ref this.m_ItemCounts);
                }
                else
                {
                    writer.Write(ref this.m_DirtyItemCounts);
                }
            }
            if ((num & 8U) != 0U)
            {
                writer.Write(this.IsSharedInventory);
            }
            if (!initialState)
            {
                this.m_DirtyItemCounts.Clear();
            }
            return num > 0U;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            uint num = reader.ReadUInt32();
            if ((num & 1U) != 0U)
            {
                this.m_Players.Clear();
                int num2 = reader.ReadInt32();
                for (int i = 0; i < num2; i++)
                {
                    this.m_Players.Add(reader.ReadInt32());
                }
                this.UpdateSubscribedPlayers();
            }
            if ((num & 2U) != 0U)
            {
                this.m_ItemReservations.Clear();
                int num3 = reader.ReadInt32();
                for (int j = 0; j < num3; j++)
                {
                    int netPlayerID = reader.ReadInt32();
                    string ID = reader.ReadString();
                    this.m_ItemReservations.Add(netPlayerID, Singleton.Manager<ManVirtualModdedContent>.inst.GetItemDescriptorFromID(ID));
                }
                // TODO: update whenever reservations for things other than chunks are added
                this.InventoryChanged.Send(NullChunkDescriptor, 0);
                this.InventoryChanged.Send(NullBlockDescriptor, 0);
            }
            if ((num & 4U) != 0U)
            {
                if (initialState)
                {
                    reader.Read(ref this.m_ItemCounts);
                }
                else
                {
                    reader.Read(ref this.m_DirtyItemCounts);
                    foreach (KeyValuePair<IVirtualItemDescriptor, int> keyValuePair in this.m_DirtyItemCounts)
                    {
                        this.OnItemInventoryChanged(keyValuePair.Key, keyValuePair.Value);
                    }
                    this.m_ItemCounts.UpdateCountsFrom(this.m_DirtyItemCounts);
                }
            }
            if ((num & 8U) != 0U)
            {
                this.IsSharedInventory = reader.ReadBoolean();
            }
        }
        #endregion

        internal void LoadFrom(ItemCountList savedItemCounts)
        {
            this.Clear();
            foreach (KeyValuePair<IVirtualItemDescriptor, int> keyValuePair in savedItemCounts)
            {
                if (keyValuePair.Value != 0)
                {
                    this.m_ItemCounts.SetQuantity(keyValuePair.Key, keyValuePair.Value, false);
                    this.m_DirtyItemCounts.SetQuantity(keyValuePair.Key, keyValuePair.Value, true);
                }
            }
        }

        private void OnRecycle()
        {
            this.m_Players.Clear();
            this.m_ItemReservations.Clear();
            this.m_ItemCounts.Clear();
            this.m_DirtyItemCounts.Clear();
            this.IsSharedInventory = false;
        }

        private static readonly FieldInfo m_SaveDataJSON = AccessTools.Field(typeof(ManSaveGame.State), "m_SaveDataJSON");
        private static readonly FieldInfo s_JSONSerialisationSettings = AccessTools.Field(typeof(ManSaveGame), "s_JSONSerialisationSettings");
        private const string SaveJSONIdentifier = "VCNetworkInventory";
        public void Save(ManSaveGame.State saveState)
        {
            Dictionary<string, string> saveJSON = (Dictionary<string, string>)m_SaveDataJSON.GetValue(saveState);
            try {
                JsonSerializerSettings serializerSettings = (JsonSerializerSettings)s_JSONSerialisationSettings.GetValue(null);
                string text = JsonConvert.SerializeObject(this.m_ItemCounts, serializerSettings);
                saveJSON.Add(SaveJSONIdentifier, text);
                return;
            }
            catch (Exception ex)
            {
                VirtualCraftingMod.logger.Error(ex, "Exception when trying to save data");
            }
            saveJSON.Add(SaveJSONIdentifier, "");
        }
        public void Load(ManSaveGame.State loadState)
        {
            Dictionary<string, string> saveJSON = (Dictionary<string, string>)m_SaveDataJSON.GetValue(loadState);
            ItemCountList saveData = new ItemCountList();
            if (saveJSON.TryGetValue(SaveJSONIdentifier, out string text) && !text.NullOrEmpty())
            {
                try
                {
                    JsonSerializerSettings serializerSettings = (JsonSerializerSettings)s_JSONSerialisationSettings.GetValue(null);
                    saveData = JsonConvert.DeserializeObject<ItemCountList>(text, serializerSettings);
                }
                catch (Exception ex) {
                    VirtualCraftingMod.logger.Error(ex, "Exception when trying to load save data");
                }
                return;
            } else if (text.NullOrEmpty())
            {
                VirtualCraftingMod.logger.Error("Loaded null or empty VC save data");
            }
            this.m_ItemCounts = saveData;
        }

        #region IVirtualInventory
        public IInventory<BlockTypes> GetBlockInventory()
        {
            return this.BlockInventory;
        }

        private bool UseBlockInventory(IVirtualItemDescriptor item)
        {
            if (item.ItemType == VirtualItemType.BLOCK)
            {
                BlockTypes sessionID = Singleton.Manager<ManVirtualModdedContent>.inst.GetSessionIDForBlock((VirtualBlockDescriptor) item);
                return Singleton.Manager<ManSpawn>.inst.IsBlockAllowedInCurrentGameMode(sessionID);
            }
            return false;
        }

        public void Clear()
        {
            this.m_DirtyItemCounts.CreateZeroedCopyOf(ref this.m_ItemCounts);
            base.SetDirtyBit(4U);
            this.m_ItemCounts.Clear();
        }

        public int GetQuantity(IVirtualItemDescriptor item)
        {
            if (this.UseBlockInventory(item))
            {
                return this.GetBlockInventory().GetQuantity(GetBlockTypeFromItemDescriptor(item));
            }
            else
            {
                return this.m_ItemCounts.GetQuantity(item);
            }
        }

        public int GetUnreservedQuantity(IVirtualItemDescriptor item)
        {
            int quantity = this.GetQuantity(item);
            if (quantity != -1)
            {
                return quantity - this.GetNumReserved(item);
            }
            return quantity;
        }

        public void SubscribeToInventoryChanged(Action<IVirtualItemDescriptor, int> _delegate)
        {
            this.InventoryChanged.Subscribe(_delegate);
        }

        public void UnsubscribeToInventoryChanged(Action<IVirtualItemDescriptor, int> _delegate)
        {
            this.InventoryChanged.Unsubscribe(_delegate);
        }

        public bool IsAvailableToLocalPlayer(IVirtualItemDescriptor item)
        {
            return this.HasReservedItem(Singleton.Manager<ManNetwork>.inst.MyPlayer.PlayerID, item) || this.GetQuantity(item) == -1 || this.GetUnreservedQuantity(item) > 0;
        }

        private static IVirtualItemDescriptor NullChunkDescriptor = new VirtualChunkDescriptor(null);
        private static IVirtualItemDescriptor NullBlockDescriptor = new VirtualBlockDescriptor(null);
        private void SendNullDescriptorUpdate(IVirtualItemDescriptor item)
        {
            if (item.ItemType == VirtualItemType.BLOCK)
            {
                this.InventoryChanged.Send(NullBlockDescriptor, 0);
            }
            else
            {
                this.InventoryChanged.Send(NullChunkDescriptor, 0);
            }
        }

        public int GetNumReserved(IVirtualItemDescriptor item)
        {
            if (this.UseBlockInventory(item))
            {
                return this.GetBlockInventory().GetNumReserved(GetBlockTypeFromItemDescriptor(item));
            }
            else
            {
                return this.m_ItemReservations.Where(p => p.Value == item).Count();
            }
        }

        public bool CanReserveItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            return this.GetUnreservedQuantity(item) > 0;
        }

        public bool HostReserveItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            if (this.UseBlockInventory(item))
            {
                bool reserved = this.GetBlockInventory().HostReserveItem(netPlayerID, GetBlockTypeFromItemDescriptor(item));
                this.SendNullDescriptorUpdate(item);
                return reserved;
            }
            else
            {
                VirtualCraftingMod.logger.Assert(ManNetwork.IsHost, "Can't call authoritative methods on VirtualNetInventory from clients");
                VirtualCraftingMod.logger.Assert(this.m_Players.Contains(netPlayerID), "This player doesn't have access to this inventory");
                base.SetDirtyBit(2U);
                IVirtualItemDescriptor itemDescriptor;
                if (this.m_ItemReservations.TryGetValue(netPlayerID, out itemDescriptor))
                {
                    VirtualCraftingMod.logger.Error(string.Concat(new object[]
                    {
                        "Player ",
                        netPlayerID,
                        " tried to reserve more than one block type. Already reserved: ",
                        itemDescriptor,
                        ", Attempting to reserve: ",
                        itemDescriptor.ID
                    }));
                    return false;
                }
                if (this.CanReserveItem(netPlayerID, item))
                {
                    this.m_ItemReservations.Add(netPlayerID, item);
                    this.SendNullDescriptorUpdate(item);
                    return true;
                }
                return false;
            }
        }

        public bool CancelReserveItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            if (this.UseBlockInventory(item))
            {
                bool reserveCancelled = this.GetBlockInventory().CancelReserveItem(netPlayerID, GetBlockTypeFromItemDescriptor(item));
                this.SendNullDescriptorUpdate(item);
                return reserveCancelled;
            }
            else
            {
                base.SetDirtyBit(2U);
                IVirtualItemDescriptor itemDescriptor;
                if (this.m_ItemReservations.TryGetValue(netPlayerID, out itemDescriptor))
                {
                    VirtualCraftingMod.logger.Assert(item == itemDescriptor, string.Concat(new object[]
                    {
                        "Player ",
                        netPlayerID,
                        " unreserved their item, but got forgot _which_ item. Reserved: ",
                        itemDescriptor.ID,
                        ", Attempting to reserve: ",
                        item.ID
                    }));
                    this.m_ItemReservations.Remove(netPlayerID);
                    this.SendNullDescriptorUpdate(item);
                    return true;
                }
                VirtualCraftingMod.logger.Error(string.Concat(new object[]
                {
                    "Player ",
                    netPlayerID,
                    " tried to cancel an item reservation for ",
                    item.ID,
                    " that they don't have"
                }));
                return false;
            }
        }

        public bool HasReservedItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            if (this.UseBlockInventory(item))
            {
                return this.GetBlockInventory().HasReservedItem(netPlayerID, GetBlockTypeFromItemDescriptor(item));
            }
            else
            {
                return this.m_ItemReservations.TryGetValue(netPlayerID, out IVirtualItemDescriptor itemDescriptor) && item == itemDescriptor;
            }
        }

        public bool CanConsumeItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            if (this.UseBlockInventory(item))
            {
                return this.GetBlockInventory().CanConsumeItem(netPlayerID, GetBlockTypeFromItemDescriptor(item));
            }
            else
            {
                int quantity = this.m_ItemCounts.GetQuantity(item);
                if (netPlayerID == -1)
                {
                    return quantity == -1 || quantity > 0;
                }
                return this.m_ItemReservations.TryGetValue(netPlayerID, out IVirtualItemDescriptor reserved) && reserved == item && (quantity == -1 || quantity > 0);
            }
        }

        public int HostConsumeItem(int netPlayerID, IVirtualItemDescriptor item, int count = 1)
        {
            int num = 0;
            if (this.UseBlockInventory(item))
            {
                num =this.GetBlockInventory().HostConsumeItem(netPlayerID, GetBlockTypeFromItemDescriptor(item), count);
            }
            else
            {
                VirtualCraftingMod.logger.Assert(ManNetwork.IsHost, "Can't call authoritative methods on VirtualNetInventory from clients");
                VirtualCraftingMod.logger.Assert(this.m_Players.Contains(netPlayerID), "This player doesn't have access to this inventory");
                if (this.CanConsumeItem(netPlayerID, item))
                {
                    num = this.m_ItemCounts.ConsumeItem(item, count);
                    if (netPlayerID != -1)
                    {
                        this.CancelReserveItem(netPlayerID, item);
                    }
                }
            }
            this.OnItemInventoryChanged(item, count);
            return num;
        }

        public void HostAddItem(IVirtualItemDescriptor item, int count = 1)
        {
            int quantity;
            if (this.UseBlockInventory(item))
            {
                this.GetBlockInventory().HostAddItem(GetBlockTypeFromItemDescriptor(item), count);
                quantity = this.GetBlockInventory().GetQuantity(GetBlockTypeFromItemDescriptor(item));
            }
            else
            {
                VirtualCraftingMod.logger.Assert(ManNetwork.IsHost, "Can't call authoritative methods on VirtualNetInventory from clients");
                quantity = this.m_ItemCounts.AddItem(item, count);
            }
            this.OnItemInventoryChanged(item, quantity);
        }

        public void SetItemCount(IVirtualItemDescriptor item, int count)
        {
            if (this.UseBlockInventory(item))
            {
                this.GetBlockInventory().SetBlockCount(GetBlockTypeFromItemDescriptor(item), count);
            }
            else
            {
                this.m_ItemCounts.SetQuantity(item, count, false);
            }
            this.OnItemInventoryChanged(item, count);
        }

        public IEnumerator<KeyValuePair<IVirtualItemDescriptor, int>> GetEnumerator()
        {
            return this.m_ItemCounts.GetEnumerator();
        }

        protected void OnItemInventoryChanged(IVirtualItemDescriptor type, int quantityOfType)
        {
            // We do not touch PlayerInventory here, because we assume we've already added directly to it
            this.InventoryChanged.Send(type, quantityOfType);
            // don't do network update when block change - already handled
            if (base.isServer && type.ItemType != VirtualItemType.BLOCK)
            {
                base.SetDirtyBit(4U);
                this.m_DirtyItemCounts.SetQuantity(type, quantityOfType, true);
            }
        }
        #endregion

        internal void OnBlockInventoryChanged(BlockTypes type, int quantityOfType)
        {
            IVirtualItemDescriptor descriptor = Singleton.Manager<ManVirtualModdedContent>.inst.GetItemDescriptorFromHash(VirtualItemType.BLOCK, (int) type);
            this.OnItemInventoryChanged(descriptor, quantityOfType);
        }

        internal ItemCountList GetItemCountsForLoading ()
        {
            return this.m_ItemCounts;
        }

        static private BlockTypes GetBlockTypeFromItemDescriptor(IVirtualItemDescriptor item)
        {
            return (BlockTypes)item.GetHashCode();
        }


        [NonSerialized]
        public Event<IVirtualItemDescriptor, int> InventoryChanged;

        [NonSerialized]
        internal NetInventory BlockInventory;

        private List<int> m_Players = new List<int>();

        [JsonProperty]
        private Dictionary<int, IVirtualItemDescriptor> m_ItemReservations = new Dictionary<int, IVirtualItemDescriptor>();

        [JsonProperty]
        private ItemCountList m_ItemCounts = new ItemCountList();

        private ItemCountList m_DirtyItemCounts = new ItemCountList();

        private const uint kSer_PlayerList = 1U;

        private const uint kSer_ItemReservations = 2U;

        private const uint kSer_ItemCounts = 4U;

        private const uint kSer_IsSharedInventory = 8U;

        private const uint kSer_OrderList = 16U;

        private const uint kSer_AllFlagMask = 4294967295U;
    }
}
