using Newtonsoft.Json;
using UnityEngine;

namespace xyz.ca2didi.Unity.JsonDataManager.Struct
{
    public class DataManagerSetting
    {
        public string GameRootDirectoryPath = Application.persistentDataPath;
        public string GameDataRelativeDirectoryPath = "\\Save";

        public int MaxGameDataCount = -1;

        public DataFileNamingRuleSetting DataFileNamingRule = new DataFileNamingRuleSetting();
        public JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();
    }
}