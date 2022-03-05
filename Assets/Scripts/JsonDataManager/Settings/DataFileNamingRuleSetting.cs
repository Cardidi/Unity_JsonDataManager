using System;
using System.Text.RegularExpressions;

namespace xyz.ca2didi.Unity.JsonDataManager.Settings
{
    public class DataFileNamingRuleSetting
    {
        public string UniverseFileName = "SaveData_Global";
        public string Prefix = "SaveData_";
        public string Postfix = "";
        public string FileType = "json";

        public string GenerateGlobalDataFileName()
            => $"{UniverseFileName}.{FileType}";

        public string GenerateDataFileName(uint id)
            => $"{Prefix}{id.ToString()}{Postfix}.{FileType}";
        
        
        public int MatchDataFileID(string path)
        {
            var regex = new Regex($"{Prefix}(\\d)+{Postfix}.{FileType}$");
            var match = regex.Match(path).Captures;
            if (match.Count <= 0)
                return -1;

            return Int32.Parse(match[0].Value);
        }
    }
}