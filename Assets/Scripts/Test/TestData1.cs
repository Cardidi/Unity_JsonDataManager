using xyz.ca2didi.Unity.JsonDataManager;

namespace Test
{
    [JsonTypeDefine(typeof(TestData1),"Test1")]
    public class TestData1 : BaseData
    {
        public override bool Invalid()
        {
            throw new System.NotImplementedException();
        }
    }

    [JsonTypeDefine(typeof(TestData2),"Test2")]
    public class TestData2 : BaseData
    {
        public override bool Invalid()
        {
            throw new System.NotImplementedException();
        }
    }
}