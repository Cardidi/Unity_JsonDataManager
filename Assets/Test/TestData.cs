using System;
using Ca2didi.JsonFSDataSystem;
using Newtonsoft.Json;

namespace Test
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