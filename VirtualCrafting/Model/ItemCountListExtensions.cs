using UnityEngine.Networking;

namespace VirtualCrafting.Model
{
    internal static class ItemCountListExtensions
    {
        public static void Write(this NetworkWriter writer, ref ItemCountList list)
        {
            list.WriteTo(writer);
        }

        public static void Read(this NetworkReader reader, ref ItemCountList list)
        {
            list.Clear();
            list.ReadFrom(reader);
        }
    }
}
