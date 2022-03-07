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
            var regex = new Regex($"^{Prefix}(\\d)+{Postfix}.{FileType}$");
            var match = regex.Match(path);
            if (!match.Success)
                return -1;

            return Int32.Parse(match.Groups[1].Value);
        }
    }
}