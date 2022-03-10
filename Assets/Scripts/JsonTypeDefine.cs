using System;

namespace xyz.ca2didi.Unity.JsonDataManager
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class JsonTypeDefine : Attribute
    {

        public readonly Type CorType;
        public readonly string JsonElementTag;

        public JsonTypeDefine(Type typ, string jsonTypeStr)
        {
            if (typ == null && !typ.IsAbstract && !typ.IsInterface)
                throw new ArgumentException($"{nameof(typ)} is not a non-abstract class type!");

            // if (!typ.IsSerializable)
            //     throw new ArgumentException($"{nameof(typ)} is not a serializable class type!");
            
            if (string.IsNullOrWhiteSpace(jsonTypeStr))
                throw new ArgumentNullException(nameof(jsonTypeStr));
            
            JsonElementTag = jsonTypeStr;
            CorType = typ;
        }
    }
}