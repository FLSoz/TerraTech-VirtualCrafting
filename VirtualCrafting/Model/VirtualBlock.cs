using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VirtualCrafting.ModdedContent;

namespace VirtualCrafting.Model
{
    public enum VirtualBlockModdedType : byte
    {
        VANILLA = 0,
        OFFICIAL = 1,
        LEGACY = 2
    }

    public class VirtualBlockTypeComparer : IEqualityComparer<VirtualBlockDescriptor>
    {
        public bool Equals(VirtualBlockDescriptor x, VirtualBlockDescriptor y)
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

        public int GetHashCode(VirtualBlockDescriptor obj)
        {
            if (obj != null)
            {
                return obj.GetHashCode();
            }
            return -1;
        }
    }

    public class VirtualBlockDescriptor : IVirtualItemDescriptor
    {
        public readonly VirtualBlockModdedType ModdedType;
        public BlockTypes SessionID { get; private set; }
        public readonly string OfficialID;
        public int LegacyID { get; private set; }
        public string ID {
            get {
                string id = OfficialID;
                if (ModdedType == VirtualBlockModdedType.VANILLA)
                {
                    id = SessionID.ToString();
                }
                return $"BLOCK:{id}";
            }
        }

        public VirtualBlockDescriptor(BlockTypes vanillaID)
        {
            this.ModdedType = VirtualBlockModdedType.VANILLA;
            this.SessionID = vanillaID;
        }

        public VirtualBlockDescriptor(int LegacyID)
        {
            this.ModdedType = VirtualBlockModdedType.LEGACY;
            this.LegacyID = LegacyID;
            this.SessionID = (BlockTypes)Singleton.Manager<ManVirtualModdedContent>.inst.GetSessionIDForBlock(LegacyID);
        }

        public VirtualBlockDescriptor(string officialID)
        {
            this.ModdedType = VirtualBlockModdedType.OFFICIAL;
            this.OfficialID = officialID;
            this.LegacyID = Singleton.Manager<ManVirtualModdedContent>.inst.GetLegacyBlockID(officialID);
            this.SessionID = (BlockTypes)Singleton.Manager<ManVirtualModdedContent>.inst.GetSessionIDForBlock(officialID);
        }

        internal void SetSessionID(BlockTypes sessionID)
        {
            this.SessionID = sessionID;
        }

        internal void SetLegacyID(int legacyID)
        {
            this.LegacyID = legacyID;
        }

        public VirtualItemType ItemType => VirtualItemType.BLOCK;

        public static bool operator ==(VirtualBlockDescriptor a, VirtualBlockDescriptor b) => a.Equals(b);
        public static bool operator !=(VirtualBlockDescriptor a, VirtualBlockDescriptor b) => !a.Equals(b);

        public override bool Equals(object b)
        {
            if (b is VirtualBlockDescriptor other && other != null)
            {
                if (ModdedType == other.ModdedType)
                {
                    switch (ModdedType)
                    {
                        case VirtualBlockModdedType.VANILLA:
                            return SessionID == other.SessionID;
                        case VirtualBlockModdedType.OFFICIAL:
                            return OfficialID == other.OfficialID;
                        case VirtualBlockModdedType.LEGACY:
                            return LegacyID == other.LegacyID;
                        default:
                            return false;
                    }
                }
                else if (ModdedType == VirtualBlockModdedType.OFFICIAL && other.ModdedType == VirtualBlockModdedType.LEGACY)
                {
                    return LegacyID == other.LegacyID;
                }
                else if (ModdedType == VirtualBlockModdedType.LEGACY && other.ModdedType == VirtualBlockModdedType.OFFICIAL)
                {
                    return LegacyID == other.LegacyID;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            switch (ModdedType)
            {
                case VirtualBlockModdedType.VANILLA:
                case VirtualBlockModdedType.OFFICIAL:
                case VirtualBlockModdedType.LEGACY:
                    return (int)SessionID;
                default:
                    return -1;
            }
        }
    }

    public class VirtualBlock : IVirtualItem
    {
        public VirtualItemType ItemType { get => VirtualItemType.BLOCK; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public Sprite Sprite { get; private set; }

        public IVirtualItemDescriptor ItemID { get; private set; }

        public void LinkItemID(IVirtualItemDescriptor itemID)
        {
            this.ItemID = itemID;
        }

        public VirtualBlock(string name, string description, Sprite sprite)
        {
            Name = name;
            Description = description;
            Sprite = sprite;
        }
    }
}
