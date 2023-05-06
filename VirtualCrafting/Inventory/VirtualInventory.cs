using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtualCrafting.Crafting;
using VirtualCrafting.Model;

namespace VirtualCrafting.Inventory
{
    internal class VirtualInventory : IVirtualInventory
    {
        public IInventory<BlockTypes> GetBlockInventory()
        {
            return Singleton.Manager<ManPlayer>.inst.PlayerInventory;
        }
        public void Clear()
        {
            this.m_ItemCounts.Clear();
        }

        IEnumerator<KeyValuePair<IVirtualItemDescriptor, int>> IVirtualInventory.GetEnumerator()
        {
            return this.m_ItemCounts.GetEnumerator();
        }

        protected void OnItemInventoryChanged(IVirtualItemDescriptor type, int quantityOfType)
        {
            // We do not touch PlayerInventory here, because we assume we've already added directly to it
            this.InventoryChanged.Send(type, quantityOfType);
        }

        public int GetQuantity(IVirtualItemDescriptor item)
        {
            if (item.ItemType == VirtualItemType.BLOCK)
            {
                return this.GetBlockInventory().GetQuantity(GetBlockTypeFromItemDescriptor(item));
            }
            return this.m_ItemCounts.GetQuantity(item);
        }

        public int GetUnreservedQuantity(IVirtualItemDescriptor item)
        {
            return this.GetQuantity(item);
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
            if (item.ItemType == VirtualItemType.BLOCK)
            {
                return this.GetBlockInventory().IsAvailableToLocalPlayer(GetBlockTypeFromItemDescriptor(item));
            }
            return this.GetQuantity(item) != 0;
        }

        public int GetNumReserved(IVirtualItemDescriptor item)
        {
            return 0;
        }

        public bool CanReserveItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            if (item.ItemType == VirtualItemType.BLOCK)
            {
                return this.GetBlockInventory().CanReserveItem(netPlayerID, GetBlockTypeFromItemDescriptor(item));
            }
            return this.GetQuantity(item) != 0;
        }

        public bool HostReserveItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            return this.CanReserveItem(netPlayerID, item);
        }

        public bool CancelReserveItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            return true;
        }

        public bool HasReservedItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            return true;
        }

        public bool CanConsumeItem(int netPlayerID, IVirtualItemDescriptor item)
        {
            if (item.ItemType == VirtualItemType.BLOCK)
            {
                return this.GetBlockInventory().CanConsumeItem(netPlayerID, GetBlockTypeFromItemDescriptor(item));
            }
            return true;
        }

        public int HostConsumeItem(int netPlayerID, IVirtualItemDescriptor item, int count = 1)
        {
            int newQuantity;
            if (item.ItemType == VirtualItemType.BLOCK)
            {
                newQuantity = this.GetBlockInventory().HostConsumeItem(netPlayerID, GetBlockTypeFromItemDescriptor(item), count);
            }
            else
            {
                newQuantity = this.m_ItemCounts.ConsumeItem(item, count);
            }
            this.OnItemInventoryChanged(item, newQuantity);
            return newQuantity;
        }

        public void HostAddItem(IVirtualItemDescriptor item, int count = 1)
        {
            int newQuantity;
            if (item.ItemType == VirtualItemType.BLOCK) {
                IInventory<BlockTypes> inventory = this.GetBlockInventory();
                BlockTypes blockType = GetBlockTypeFromItemDescriptor(item);
                inventory.HostAddItem(blockType, count);
                newQuantity = inventory.GetQuantity(blockType);
            }
            else
            {
                newQuantity = this.m_ItemCounts.AddItem(item, count);
            }
            this.OnItemInventoryChanged(item, newQuantity);
        }

        public void SetItemCount(IVirtualItemDescriptor item, int count)
        {
            if (item.ItemType == VirtualItemType.BLOCK)
            {
                this.GetBlockInventory().SetBlockCount(GetBlockTypeFromItemDescriptor(item), count);
            }
            else
            {
                this.m_ItemCounts.SetQuantity(item, count);
            }
            this.OnItemInventoryChanged(item, count);
        }

        static private BlockTypes GetBlockTypeFromItemDescriptor(IVirtualItemDescriptor item)
        {
            return (BlockTypes) item.GetHashCode();
        }

        [NonSerialized]
        public Event<IVirtualItemDescriptor, int> InventoryChanged;
        [JsonProperty]
        private ItemCountList m_ItemCounts = new ItemCountList();
    }
}
