using System;
using Newtonsoft.Json;
using xyz.ca2didi.Unity.JsonFSDataSystem;

namespace xyz.ca2didi.Unity.Test
{
    [Serializable, JsonTypeDefine(typeof(TestData), "test")]
    public class TestData
    {
        public int a;
        public int b;
        [NonSerialized]
        public char ai;

        [JsonConstructor]
        public TestData(int A, int B)
        {
            a = A;
            b = B;
        }
    }
}