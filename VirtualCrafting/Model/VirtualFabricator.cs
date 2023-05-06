using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VirtualCrafting.ModdedContent;

namespace VirtualCrafting.Model
{
    // TODO: implement fabricators
    public class VirtualFabricatorDescriptor
    {
        public readonly string FabricatorID;

        public VirtualFabricatorDescriptor(string fabricatorID)
        {
            FabricatorID = fabricatorID;
        }

        public VirtualItemType ItemType => VirtualItemType.BLOCK;

        public static bool operator ==(VirtualFabricatorDescriptor a, VirtualFabricatorDescriptor b) => a.Equals(b);
        public static bool operator !=(VirtualFabricatorDescriptor a, VirtualFabricatorDescriptor b) => !a.Equals(b);

        public override bool Equals(object y)
        {
            if (y is VirtualFabricatorDescriptor other && other != null) {
                return FabricatorID.Equals(other.FabricatorID);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Singleton.Manager<ManVirtualModdedContent>.inst.GetSessionIDForFabricator(FabricatorID);
        }
    }

    public class VirtualFabricator : IVirtualCraftingBlock
    {
        public VirtualItemType ItemType { get => VirtualItemType.BLOCK; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public Sprite Sprite { get; private set;  }

        public VirtualBlockDescriptor UnderlyingBlock { get; private set; }

        public IVirtualItemDescriptor ItemID { get; private set; }

        public void LinkItemID(IVirtualItemDescriptor itemID)
        {
            this.ItemID = itemID;
        }

        public VirtualFabricator(string name, string description, Sprite sprite, VirtualBlockDescriptor underlyingBlock)
        {
            Name = name;
            Description = description;
            Sprite = sprite;
            UnderlyingBlock = underlyingBlock;

            // setup the fabricator to actually be useful
            SetupRecipes();
        }

        private void SetupRecipes()
        {

        }
    }
}
