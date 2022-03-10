using System;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace xyz.ca2didi.Unity.JsonFSDataSystem.Json
{
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            new JObject
            {
                {"w", value.W},
                {"x", value.X},
                {"y", value.Y},
                {"z", value.Z}
            }.WriteTo(writer);
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {           
            var obj = JObject.ReadFrom(reader); 
            var x = obj["x"]?.ToObject<float>(serializer) ?? 0;
            var y = obj["y"]?.ToObject<float>(serializer) ?? 0;
            var z = obj["z"]?.ToObject<float>(serializer) ?? 0;
            var w = obj["w"]?.ToObject<float>(serializer) ?? 0;

            if (hasExistingValue)
            {
                existingValue.W = w;
                existingValue.X = x;
                existingValue.Y = y;
                existingValue.Z = z;
                return existingValue;
            }

            return new Quaternion(x, y, z, w);
        }
    }
}