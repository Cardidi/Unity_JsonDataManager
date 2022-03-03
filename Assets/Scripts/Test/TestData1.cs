using xyz.ca2didi.Unity.JsonDataManager.FS;
using xyz.ca2didi.Unity.JsonDataManager.Interface;

[JsonTypeBinder("Test1")]
public class TestData1 : BaseData
{
    public override bool Invalid()
    {
        throw new System.NotImplementedException();
    }
}

[JsonTypeBinder("Test2")]
public class TestData2 : BaseData
{
    public override bool Invalid()
    {
        throw new System.NotImplementedException();
    }
}