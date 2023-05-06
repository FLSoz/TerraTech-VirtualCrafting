using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VirtualCrafting.ModdedContent;

namespace VirtualCrafting.Model
{
    public enum VirtualChunkModdedType : byte
    {
        VANILLA = 0,
        MODDED = 1
    }

    public class VirtualChunkTypeComparer : IEqualityComparer<VirtualChunkDescriptor>
    {
        public bool Equals(VirtualChunkDescriptor x, VirtualChunkDescriptor y)
        {
            if (x != null)
            {
                return x.Equals(y);
            }
            else
            {
                return y == null;
            }
        }

        public int GetHashCode(VirtualChunkDescriptor obj)
        {
            if (obj != null)
            {
                return obj.GetHashCode();
            }
            return -1;
        }
    }

    public class VirtualChunkDescriptor : IVirtualItemDescriptor
    {
        public readonly VirtualChunkModdedType ModdedType;
        public ChunkTypes ChunkID { get; private set; }
        public readonly string ModdedChunkID;

        public VirtualItemType ItemType => VirtualItemType.CHUNK;
        public string ID {
            get {
                string id = ModdedChunkID;
                if (ModdedType == VirtualChunkModdedType.VANILLA)
                {
                    id = ChunkID.ToString();
                }
                return $"CHUNK:{id}";
            }
        }

        public VirtualChunkDescriptor(ChunkTypes vanillaID)
        {
            this.ChunkID = vanillaID;
            this.ModdedType = VirtualChunkModdedType.VANILLA;
        }

        public VirtualChunkDescriptor(string moddedID)
        {
            this.ModdedChunkID = moddedID;
            this.ModdedType = VirtualChunkModdedType.MODDED;
            this.ChunkID = (ChunkTypes)Singleton.Manager<ManVirtualModdedContent>.inst.GetChunkID(moddedID);
        }

        internal void SetChunkID(ChunkTypes sessionID)
        {
            this.ChunkID = sessionID;
        }
        public static bool operator ==(VirtualChunkDescriptor a, VirtualChunkDescriptor b) => a.Equals(b);
        public static bool operator !=(VirtualChunkDescriptor a, VirtualChunkDescriptor b) => !a.Equals(b);

        public override bool Equals(object obj)
        {
            if (obj is VirtualChunkDescriptor other && other != null && this.ModdedType == other.ModdedType)
            {
                switch (this.ModdedType)
                {
                    case VirtualChunkModdedType.VANILLA:
                        return this.ChunkID == other.ChunkID;
                    case VirtualChunkModdedType.MODDED:
                        return this.ModdedChunkID == other.ModdedChunkID;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            switch (this.ModdedType)
            {
                case VirtualChunkModdedType.VANILLA:
                    return (int)this.ChunkID;
                case VirtualChunkModdedType.MODDED:
                    return Singleton.Manager<ManVirtualModdedContent>.inst.GetChunkID(this.ModdedChunkID);
            }
            return -1;
        }
    }

    public class VirtualChunk : IVirtualItem
    {
        public VirtualItemType ItemType { get => VirtualItemType.CHUNK; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public Sprite Sprite { get; private set; }

        public IVirtualItemDescriptor ItemID { get; private set; }

        public void LinkItemID(IVirtualItemDescriptor itemID)
        {
            this.ItemID = itemID;
        }

        public ChunkRarity Rarity { get; private set; }

        public ChunkCategory[] Category { get; private set; }

        public VirtualChunk(string name, string description, Sprite sprite, ChunkRarity rarity, ChunkCategory[] category)
        {
            Name = name;
            Description = description;
            Sprite = sprite;
            Rarity = rarity;
            Category = category;
        }
    }
}
