using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VirtualCrafting.Crafting;
using VirtualCrafting.ModdedContent;
using VirtualCrafting.Model;

namespace VirtualCrafting.Modules
{
    [RequireComponent(typeof(ModuleItemHolder))]
    internal class ModuleChunkVirtualizer : Module
    {
        private void OnAttached()
        {
            base.block.tank.Holders.RegisterOperation(this.m_Holder, new Func<TechHolders.OperationResult>(this.OnCycle), -9);
        }

        private void OnDetaching()
        {
            base.block.tank.Holders.UnregisterOperations(this.m_Holder);
        }

        private TechHolders.OperationResult OnCycle()
        {
            TechHolders.OperationResult operationResult = TechHolders.OperationResult.None;
            if (!this.m_Holder.IsEmpty)
            {
                Visible firstItem = this.m_Holder.SingleStack.FirstItem;
                if (!firstItem.TakenThisHeartbeat)
                {
                    bool notifyRelease = true;

                    ItemTypeInfo itemType = firstItem.m_ItemType;

                    if (itemType != null)
                    {
                        IVirtualItemDescriptor descriptor = null;
                        if (itemType.ObjectType == ObjectTypes.Chunk)
                        {
                            descriptor = Singleton.Manager<ManVirtualModdedContent>.inst.GetItemDescriptorFromHash(VirtualItemType.CHUNK, itemType.ItemType);
                        }
                        else if (itemType.ObjectType == ObjectTypes.Block)
                        {
                            descriptor = Singleton.Manager<ManVirtualModdedContent>.inst.GetItemDescriptorFromHash(VirtualItemType.BLOCK, itemType.ItemType);
                        }
                        if (descriptor != null)
                        {
                            if (Singleton.Manager<ManNetwork>.inst.IsServer)
                            {
                                firstItem.ServerDestroy();
                                // We have no separate inventories, because only in CoOP
                                Singleton.Manager<ManVirtualCrafting>.inst.m_SharedInventory.HostAddItem(descriptor);
                            }
                            else
                            {
                                firstItem.trans.Recycle(true);
                                if (!Singleton.Manager<ManNetwork>.inst.IsMultiplayer())
                                {
                                    Singleton.Manager<ManVirtualCrafting>.inst.PlayerInventory.HostAddItem(descriptor);
                                }
                            }
                        }
                        else
                        {
                            VirtualCraftingMod.logger.Error($"INVALID ObjectType for item {firstItem.name}: {itemType.ObjectType}");
                            firstItem.SetHolder(null, notifyRelease, false, true);
                            firstItem.SetLockTimout(Visible.LockTimerTypes.ItemCollection, this.m_CollectionTimeout);
                        }
                    }
                    else
                    {
                        VirtualCraftingMod.logger.Error($"NULL m_ItemType for item {firstItem.name}");
                    }
                    operationResult = TechHolders.OperationResult.Effect;
                }
            }
            if (this.m_Holder.IsEmpty)
            {
                foreach (ModuleItemHolder.Stack stack in this.m_Holder.SingleStack.connectedNeighbourStacks)
                {
                    if (stack != null)
                    {
                        TechHolders.OperationResult operationResult2 = TechHolders.OperationResult.None;
                        foreach (Visible item in stack.IterateItemsIncludingLinkedStacks(0))
                        {
                            operationResult2 = this.m_Holder.SingleStack.TryTakeOnHeartbeat(item);
                            if (operationResult2 != TechHolders.OperationResult.None)
                            {
                                operationResult = TechHolders.CombineOperationResults(operationResult, operationResult2);
                                break;
                            }
                        }
                        if (operationResult2 != TechHolders.OperationResult.None)
                        {
                            break;
                        }
                    }
                }
            }
            return operationResult;
        }

        private void OnPool()
        {
            base.block.AttachedEvent.Subscribe(new Action(this.OnAttached));
            base.block.DetachingEvent.Subscribe(new Action(this.OnDetaching));
            base.block.BlockUpdate.Subscribe(new Action(this.OnUpdate));
            this.m_Holder = base.GetComponent<ModuleItemHolder>();
        }

        private void OnUpdate()
        {
            if (this.m_PullArrowPrefab != null && base.block.tank)
            {
                ModuleItemHolder.Stack singleStack = this.m_Holder.SingleStack;
                for (int i = 0; i < singleStack.connectedNeighbourStacks.Length; i++)
                {
                    if (singleStack.connectedNeighbourStacks[i] != null)
                    {
                        base.block.tank.Holders.UpdateStackArrow(singleStack, i, true, this.m_PullArrowPrefab, 5);
                    }
                }
            }
        }

        [Tooltip("Time before this item can be collected by ModuleItemPickup (eg Recievers and TractorPads)")]
        [SerializeField]
        private float m_CollectionTimeout = 1f;
        [SerializeField]
        private Transform m_PullArrowPrefab;
        private ModuleItemHolder m_Holder;
    }
}
