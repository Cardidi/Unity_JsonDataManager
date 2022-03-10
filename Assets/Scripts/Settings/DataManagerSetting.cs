using System;
using Newtonsoft.Json;
using UnityEngine;

namespace xyz.ca2didi.Unity.JsonDataManager.Settings
{
    public class DataManagerSetting
    {
        /// <summary>
        /// Root directory of data. Default value is <see cref="Application.persistentDataPath"/> .
        /// </summary>
        public string GameRootDirectoryPath = Application.persistentDataPath;
        
        /// <summary>
        /// Relative directory of data. The final path of data is <see cref="GameRootDirectoryPath"/>/<see cref="GameDataRelativeDirectoryPath"/>.>
        /// </summary>
        public string GameDataRelativeDirectoryPath = "/Save";

        /// <summary>
        /// Custom json converter to (de)serialize types which is not support by Newtonsoft.Json
        /// </summary>
        public JsonConverter[] CustomConverters;
        
        /// <summary>
        /// Enable json converters which specific designed for unity class.
        /// </summary>
        public bool UsingUnitySpecificJsonConverters = true;
        
        /// <summary>
        /// Enable to see more log info.
        /// </summary>
        public bool UnderDevelopment = false;
        
        /// <summary>
        /// The max number of data you can save. If value is less than 1, it means max number is <see cref="Int32.MaxValue"/>.
        /// </summary>
        public int MaxGameDataCount = 0;
        
        /// <summary>
        /// Data file naming rule.
        /// </summary>
        public DataFileNamingRuleSetting DataFileNamingRule = new DataFileNamingRuleSetting();
        
        /// <summary>
        /// Json serializer setting object. Better not modify it!
        /// </summary>
        public JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();
    }
}