using System;
using JetBrains.Annotations;

namespace xyz.ca2didi.Unity.JsonDataManager
{
    public class DataTypeBinder
    {
        public System.Type ActualType => Define.CorType;
        public string JsonElement => Define.JsonElementTag;
        private readonly JsonTypeDefine Define;
        
        internal DataTypeBinder([NotNull] JsonTypeDefine define)
        {
            Define = define;
        }

        public bool IsValidFileTypeString(string typeStr)
            => typeStr.Equals(JsonElement);

    }
}