using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace xyz.ca2didi.Unity.JsonFSDataSystem.Json
{
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            new JObject
            {
                {"x", value.x},
                {"y", value.y}
            }.WriteTo(writer);
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.ReadFrom(reader);
            var x = obj["x"]?.ToObject<float>(serializer) ?? 0;
            var y = obj["y"]?.ToObject<float>(serializer) ?? 0;

            if (hasExistingValue)
            {
                existingValue.Set(x, y);
                return existingValue;
            }

            return new Vector2(x, y);
        }
    }
    
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            new JObject
            {
                {"x", value.x},
                {"y", value.y},
                {"z", value.z}
            }.WriteTo(writer);
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.ReadFrom(reader);
            var x = obj["x"]?.ToObject<float>(serializer) ?? 0;
            var y = obj["y"]?.ToObject<float>(serializer) ?? 0;
            var z = obj["z"]?.ToObject<float>(serializer) ?? 0;

            if (hasExistingValue)
            {
                existingValue.Set(x, y, z);
                return existingValue;
            }

            return new Vector3(x, y, z);
        }
    }
    
    public class Vector4Converter : JsonConverter<Vector4>
    {
        public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
        {
            new JObject
            {
                {"x", value.x},
                {"y", value.y},
                {"z", value.z},
                {"w", value.w}
            }.WriteTo(writer);
        }

        public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.ReadFrom(reader);
            var x = obj["x"]?.ToObject<float>(serializer) ?? 0;
            var y = obj["y"]?.ToObject<float>(serializer) ?? 0;
            var z = obj["z"]?.ToObject<float>(serializer) ?? 0;
            var w = obj["w"]?.ToObject<float>(serializer) ?? 0;

            if (hasExistingValue)
            {
                existingValue.Set(x, y, z, w);
                return existingValue;
            }

            return new Vector4(x, y, z, w);
        }
    }
}