using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VirtualCrafting.Model
{
    public enum VirtualItemType : byte
    {
        BLOCK = 0,
        CHUNK = 1
    }
    public interface IVirtualItemDescriptor {
        VirtualItemType ItemType { get; }
        string ID { get; }
        bool Equals(object obj);
    }
    internal interface IVirtualItem
    {
        void LinkItemID(IVirtualItemDescriptor itemID);
        IVirtualItemDescriptor ItemID { get; }
        VirtualItemType ItemType { get; }
        Sprite Sprite { get; }
        string Name { get; }
        string Description { get; }
    }
}
