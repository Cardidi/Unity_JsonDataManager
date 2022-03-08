using Newtonsoft.Json;
using UnityEngine;

namespace xyz.ca2didi.Unity.JsonDataManager.Settings
{
    public class DataManagerSetting
    {
        public string GameRootDirectoryPath = Application.persistentDataPath;
        public string GameDataRelativeDirectoryPath = "/Save";

        public int MaxGameDataCount = 0;

        public DataFileNamingRuleSetting DataFileNamingRule = new DataFileNamingRuleSetting();
        public JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();
    }
}