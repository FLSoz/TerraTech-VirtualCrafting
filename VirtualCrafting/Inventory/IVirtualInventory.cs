using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCrafting.Model
{
    // Virtual Inventory will send out changed event when *anything* associated with it changes
    // This includes the virtual chunk inventory, as well as the actual block inventory
    public interface IVirtualInventory
    {
        IInventory<BlockTypes> GetBlockInventory();

        IEnumerator<KeyValuePair<IVirtualItemDescriptor, int>> GetEnumerator();

        int GetQuantity(IVirtualItemDescriptor item);

        int GetUnreservedQuantity(IVirtualItemDescriptor item);

        void SubscribeToInventoryChanged(Action<IVirtualItemDescriptor, int> _delegate);

        void UnsubscribeToInventoryChanged(Action<IVirtualItemDescriptor, int> _delegate);

        bool IsAvailableToLocalPlayer(IVirtualItemDescriptor item);

        int GetNumReserved(IVirtualItemDescriptor item);

        bool CanReserveItem(int netPlayerID, IVirtualItemDescriptor item);

        bool HostReserveItem(int netPlayerID, IVirtualItemDescriptor item);

        bool CancelReserveItem(int netPlayerID, IVirtualItemDescriptor item);

        bool HasReservedItem(int netPlayerID, IVirtualItemDescriptor item);

        bool CanConsumeItem(int netPlayerID, IVirtualItemDescriptor item);

        int HostConsumeItem(int netPlayerID, IVirtualItemDescriptor item, int count = 1);

        void HostAddItem(IVirtualItemDescriptor item, int count = 1);

        void SetItemCount(IVirtualItemDescriptor item, int count);

        void Clear();
    }
}
