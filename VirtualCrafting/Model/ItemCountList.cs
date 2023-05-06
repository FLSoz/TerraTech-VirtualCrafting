using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using VirtualCrafting.ModdedContent;

namespace VirtualCrafting.Model
{
    internal class VirtualItemComparer : IEqualityComparer<IVirtualItemDescriptor>
    {
        public bool Equals(IVirtualItemDescriptor x, IVirtualItemDescriptor y)
        {
            if (x.ItemType == y.ItemType)
            {
                switch(x.ItemType) {
                    case VirtualItemType.BLOCK:
                    case VirtualItemType.CHUNK:
                        return (x as VirtualChunkDescriptor).Equals(y as VirtualChunkDescriptor);
                    default:
                        VirtualCraftingMod.logger.Fatal($"Found invalid item type {x.ItemType}");
                        break;
                }
            }
            return false;
        }

        public int GetHashCode(IVirtualItemDescriptor obj)
        {
            throw new NotImplementedException();
        }
    }

    internal class ItemCountList
    {
        public ItemCountList()
        {
            this.m_Counts = new Dictionary<IVirtualItemDescriptor, int>(new VirtualItemComparer());
        }

        public int Count
        {
            get
            {
                return this.m_Counts.Count;
            }
        }

        public Dictionary<IVirtualItemDescriptor, int>.KeyCollection Keys
        {
            get
            {
                return this.m_Counts.Keys;
            }
        }

        public Dictionary<IVirtualItemDescriptor, int>.Enumerator GetEnumerator()
        {
            return this.m_Counts.GetEnumerator();
        }

        public int GetQuantity(IVirtualItemDescriptor blockType)
        {
            int result;
            this.m_Counts.TryGetValue(blockType, out result);
            return result;
        }

        public void SetQuantity(IVirtualItemDescriptor blockType, int count, bool keepZeroes = false)
        {
            if (count == 0 && !keepZeroes)
            {
                this.m_Counts.Remove(blockType);
                return;
            }
            this.m_Counts[blockType] = count;
        }

        public int ConsumeItem(IVirtualItemDescriptor blockType, int count)
        {
            VirtualCraftingMod.logger.Assert(
                count != -1,
                "Inventory - Attempting to remove INFINITE quantity of blocks of type " + blockType + ". Not currently supported!"
            );
            int num = this.GetQuantity(blockType);
            if (num != -1)
            {
                if (count > num)
                {
                    VirtualCraftingMod.logger.Error(string.Concat(new object[]
                    {
                    "Inventory - Attempting to remove ",
                    count,
                    " blocks of type ",
                    blockType,
                    ", but only have ",
                    num,
                    " available"
                    }));
                }
                count = Mathf.Min(count, num);
                num -= count;
            }
            this.SetQuantity(blockType, num, false);
            return num;
        }

        public int AddItem(IVirtualItemDescriptor blockType, int count)
        {
            int num = this.GetQuantity(blockType);
            if (num == -1 || count == -1)
            {
                num = -1;
            }
            else
            {
                num += count;
            }
            this.m_Counts[blockType] = num;
            return num;
        }

        public void Clear()
        {
            this.m_Counts.Clear();
        }

        public void CreateZeroedCopyOf(ref ItemCountList list)
        {
            this.m_Counts.Clear();
            foreach (IVirtualItemDescriptor key in list.m_Counts.Keys)
            {
                this.m_Counts.Add(key, 0);
            }
        }

        public void UpdateCountsFrom(ItemCountList other)
        {
            foreach (KeyValuePair<IVirtualItemDescriptor, int> keyValuePair in other.m_Counts)
            {
                this.SetQuantity(keyValuePair.Key, keyValuePair.Value, false);
            }
        }

        #region Network serialization
        public void WriteTo(NetworkWriter writer)
        {
            writer.Write(this.m_Counts.Count);
            foreach (KeyValuePair<IVirtualItemDescriptor, int> keyValuePair in this.m_Counts)
            {
                string ID = keyValuePair.Key.ID;
                writer.Write(ID);
                writer.Write(keyValuePair.Value);
            }
        }

        public void ReadFrom(NetworkReader reader)
        {
            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                string ID = reader.ReadString();
                IVirtualItemDescriptor descriptor = Singleton.Manager<ManVirtualModdedContent>.inst.GetItemDescriptorFromID(ID);
                if (descriptor != null)
                {
                    this.SetQuantity(descriptor, reader.ReadInt32(), true);
                }
                else
                {

                }
            }
        }
        #endregion

        [JsonProperty]
        private Dictionary<IVirtualItemDescriptor, int> m_Counts;
    }
}
