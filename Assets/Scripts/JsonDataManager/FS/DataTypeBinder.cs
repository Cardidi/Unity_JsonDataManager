using System;
using JetBrains.Annotations;

namespace xyz.ca2didi.Unity.JsonDataManager.FS
{
    public class DataTypeBinder
    {
        public Type ActualType { get; }
        public string JsonElement { get; }
        public string ReferName { get; }
        
        
        public DataTypeBinder([NotNull] Type type,[NotNull] string jsonElement, string refName = null)
        {
            if (string.IsNullOrEmpty(jsonElement))
                throw new ArgumentNullException(nameof(jsonElement));

            if (string.IsNullOrEmpty(refName))
                refName = jsonElement;

            ActualType = type;
            JsonElement = jsonElement;
            ReferName = refName;
        }
    }
}