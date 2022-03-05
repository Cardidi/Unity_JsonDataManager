using System;


namespace xyz.ca2didi.Unity.JsonDataManager.FS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public class JsonTypeDefine : Attribute
    {
        private static readonly Type baseType = typeof(BaseData);

        public readonly Type CorType;
        public readonly string JsonElementTag;

        public JsonTypeDefine(Type typ, string jsonTypeStr)
        {
            if (typ == null && !typ.IsAbstract && !typ.IsInterface)
                throw new ArgumentException($"{nameof(typ)} is not a non-abstract class type!");
            
            if (typ.IsSubclassOf(baseType))
                throw new ArgumentException($"{nameof(typ)} is a subclass of BaseData!");
            
            // if (!typ.IsSerializable)
            //     throw new ArgumentException($"{nameof(typ)} is not a serializable class type!");
            
            if (string.IsNullOrWhiteSpace(jsonTypeStr))
                throw new ArgumentNullException(nameof(jsonTypeStr));
            
            JsonElementTag = jsonTypeStr;
            CorType = typ;
        }
    }
}