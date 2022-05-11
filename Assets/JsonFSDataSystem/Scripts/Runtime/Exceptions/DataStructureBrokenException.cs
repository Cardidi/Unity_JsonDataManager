using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace xyz.ca2didi.Unity.JsonFSDataSystem.Exceptions
{
    public class DataStructureBrokenException : Exception
    {
        private JToken Broken { get; }

        internal DataStructureBrokenException(JToken brokenToken) : base($"Original json data was broken! Plain json: {brokenToken.ToString(Formatting.Indented)}")
        {
            Broken = brokenToken;
        }
    }
}