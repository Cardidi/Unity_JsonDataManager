using System;
using xyz.ca2didi.Unity.JsonDataManager.Interface;


namespace xyz.ca2didi.Unity.JsonDataManager.FS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class JsonTypeDefine : Attribute
    {
        private static Type baseType = typeof(BaseData);

        public readonly Type CorType;
        public readonly string JsonElementTag;

        public JsonTypeDefine(Type typ, string jsonTypeStr)
        {
            if (typ == null || typ.IsSubclassOf(baseType))
                throw new ArgumentException(nameof(typ));
            
            if (string.IsNullOrWhiteSpace(jsonTypeStr))
                throw new ArgumentNullException(nameof(jsonTypeStr));
            
            JsonElementTag = jsonTypeStr;
            CorType = typ;
        }
    }
}