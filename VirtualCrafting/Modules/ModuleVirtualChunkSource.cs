using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VirtualCrafting.Crafting;
using VirtualCrafting.Model;

namespace VirtualCrafting.Modules
{
    [RequireComponent(typeof(ModuleItemHolder))]
    public class ModuleVirtualChunkSource : Module, ItemSearchHandler
    {
        private void OnAttached()
        {
            // base.block.tank.Holders.RegisterOperation(this.m_Holder, new Func<TechHolders.OperationResult>(this.OnPull), 5);
            base.block.tank.Holders.RegisterOperation(this.m_Holder, new Func<TechHolders.OperationResult>(this.OnPush), 15);
        }

        private TechHolders.OperationResult OnPush()
        {
            TechHolders.OperationResult operationResult = TechHolders.OperationResult.None;
            foreach (ModuleItemHolder.Stack stack in this.m_Holder.Stacks)
            {
                for (int i = stack.NumItems - 1; i >= 0; i--)
                {
                    Visible item = stack.items[i];
                    ModuleItemHolder.Stack requestedItemNextHop = base.block.tank.Holders.GetRequestedItemNextHop(item);
                    if (requestedItemNextHop != null)
                    {
                        operationResult = TechHolders.CombineOperationResults(operationResult, requestedItemNextHop.TryTakeOnHeartbeat(item));
                    }
                }
            }
            return operationResult;
        }

        private void OnDetaching()
        {
            base.block.tank.Holders.UnregisterOperations(this.m_Holder);
            this.m_CurrentItemType.Set(ObjectTypes.Null, 0);
        }

        private void OnReleaseItem(Visible item, ModuleItemHolder.Stack fromStack, ModuleItemHolder.Stack toStack)
        {
            if (this.m_Holder.IsEmpty)
            {
                this.m_CurrentItemType.Set(ObjectTypes.Null, 0);
            }
        }

        private bool CanAcceptItem(Visible item, ModuleItemHolder.Stack fromStack, ModuleItemHolder.Stack toStack, ModuleItemHolder.PassType passType)
        {
            return false;
        }

        private bool CanReleaseItem(Visible item, ModuleItemHolder.Stack fromStack, ModuleItemHolder.Stack toStack, ModuleItemHolder.PassType passType)
        {
            return (passType & ModuleItemHolder.PassType.Pass) == (ModuleItemHolder.PassType)0 || base.block.tank.Holders.GetRequestedItemNextHop(item) == toStack || (toStack != null && toStack.myHolder.IsFlag(ModuleItemHolder.Flags.TakeFromSilo));
        }

        private void PrePool()
        {
            ModuleItemHolder component = base.GetComponent<ModuleItemHolder>();
            component.OverrideStackCapacity(Mathf.CeilToInt((float)(this.m_Capacity / component.NumStacks)) + component.NumStacks);
        }

        private void OnPool()
        {
            base.block.AttachedEvent.Subscribe(new Action(this.OnAttached));
            base.block.DetachingEvent.Subscribe(new Action(this.OnDetaching));
            base.block.BlockUpdate.Subscribe(new Action(this.OnUpdate));
            this.m_Holder = base.GetComponent<ModuleItemHolder>();
            this.m_Holder.SetAcceptFilterCallback(new Func<Visible, ModuleItemHolder.Stack, ModuleItemHolder.Stack, ModuleItemHolder.PassType, bool>(this.CanAcceptItem), false);
            this.m_Holder.SetReleaseFilterCallback(new Func<Visible, ModuleItemHolder.Stack, ModuleItemHolder.Stack, ModuleItemHolder.PassType, bool>(this.CanReleaseItem), false);
            this.m_Holder.ReleaseItemEvent.Subscribe(new Action<Visible, ModuleItemHolder.Stack, ModuleItemHolder.Stack>(this.OnReleaseItem));
            this.m_Holder.ItemRequestHandler = this;
        }

        private void OnSpawn()
        {
            this.m_CurrentItemType.Set(ObjectTypes.Null, 0);
            this.m_LastRequestHeartbeat = -1;
        }

        private void OnUpdate()
        {
            if (this.m_Holder.Antenna != null)
            {
                bool flag = base.block.tank != null && this.m_LastRequestHeartbeat == base.block.tank.Holders.HeartbeatCount;
                this.m_Holder.Antenna.RequestDeploy = flag;
                this.m_Holder.Antenna.RequestGlow = flag;
            }
        }

        #region ItemSearchHandler
        public void HandleExpandSearch(ItemSearcher builder, ModuleItemHolder.Stack entryStack, ModuleItemHolder.Stack prevStack, out ItemSearchAvailableItems availItems)
        {
            availItems = ItemSearchAvailableItems.Processed;
        }

        public void HandleSearchRequest()
        {
            this.m_LastRequestHeartbeat = base.block.tank.Holders.HeartbeatCount;
        }

        public bool WantsToKnowAboutSearchRequest()
        {
            return true;
        }

        public void HandleCollectItems(ItemSearchCollector collector, bool processed)
        {
            // offer entire V inventory. Assume this is singleplayer
            foreach (KeyValuePair<IVirtualItemDescriptor, int> p in Singleton.Manager<ManVirtualCrafting>.inst.PlayerInventory)
            {
                IVirtualItemDescriptor desc = p.Key;
                int count = p.Value;
                if (desc.ItemType == VirtualItemType.CHUNK && ((VirtualChunkDescriptor) desc).ModdedType == VirtualChunkModdedType.VANILLA)
                {
                    for (int i = 0; i < count; i++)
                    {
                        collector.OfferAnonItem(new ItemTypeInfo(ObjectTypes.Chunk, desc.GetHashCode()));
                    }
                }
            }
        }

        private int m_LastRequestHeartbeat;
        #endregion ItemSearchHandler

        [SerializeField]
        private int m_Capacity;

        [SerializeField]
        private bool m_SingleType;

        private ModuleItemHolder m_Holder;

        private ItemTypeInfo m_CurrentItemType = new ItemTypeInfo(ObjectTypes.Null, 0);

    }
}
