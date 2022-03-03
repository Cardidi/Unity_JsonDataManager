using System;

namespace xyz.ca2didi.Unity.JsonDataManager.FS
{
    [AttributeUsage(AttributeTargets.Class)]
    public class JsonTypeBinder : Attribute
    {
        public readonly string JsonElementTag;

        public JsonTypeBinder(string jsonTypeStr)
        {
            if (string.IsNullOrWhiteSpace(jsonTypeStr))
                throw new ArgumentNullException(nameof(jsonTypeStr));
            
            JsonElementTag = jsonTypeStr;
        }
    }
}