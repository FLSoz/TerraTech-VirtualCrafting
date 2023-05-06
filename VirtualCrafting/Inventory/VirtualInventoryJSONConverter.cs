using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCrafting.Model;
using VirtualCrafting.ModdedContent;

namespace VirtualCrafting.Inventory
{
    internal class VirtualInventoryJSONConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IVirtualInventory);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                IVirtualInventory inventory = (IVirtualInventory)existingValue;
                inventory.Clear();
                JObject jobject = JObject.Load(reader);
                VirtualCraftingMod.logger.Assert(jobject != null && jobject.HasValues, "Inventory JSON is incorrect");
                foreach (JToken jtoken in ((IEnumerable<JToken>)jobject["m_InventoryList"]))
                {
                    JObject jobject2 = (JObject)jtoken;
                    string ID = jobject2["m_ItemID"].ToObject<string>();
                    IVirtualItemDescriptor itemDesc = Singleton.Manager<ManVirtualModdedContent>.inst.GetItemDescriptorFromID(ID);
                    int count = jobject2["m_Quantity"].ToObject<int>();
                    if (itemDesc != null)
                    {
                        VirtualCraftingMod.logger.Trace($"Adding item descriptor {ID} with quantity {count}");
                        inventory.SetItemCount(itemDesc, count);
                    }
                    else
                    {
                        VirtualCraftingMod.logger.Error($"Failed to find item descriptor {ID} with quantity {count}");
                    }
                }
                return inventory;
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IVirtualInventory inventory = (IVirtualInventory)value;
            if (inventory != null)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("m_InventoryList");
                writer.WriteStartArray();
                foreach (KeyValuePair<IVirtualItemDescriptor, int> keyValuePair in inventory)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("m_ItemID");
                    writer.WriteValue(keyValuePair.Key.ID);
                    writer.WritePropertyName("m_Quantity");
                    writer.WriteValue(keyValuePair.Value);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }
    }

}
