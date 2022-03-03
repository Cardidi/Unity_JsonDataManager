using Newtonsoft.Json;

namespace xyz.ca2didi.Unity.JsonDataManager.Interface
{
    public abstract class BaseData
    {
        public abstract bool Invalid();
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}