using Ca2didi.JsonFSDataSystem;
using UnityEngine;

namespace Test
{
    public class Boot : MonoBehaviour
    {
        private void Awake()
        {
            var man = DataManager.CreateNew();
            
            // if (DataFile.CreateOrGet(FSPath.StaticPathRoot + ".test").As<TestData>(out var t))
            // {
            //     var d = t.Read();
            //     Debug.Log($"{d.a}");
            // }
            //
            // var f = DataFolder.CreateOrGet(FSPath.StaticPathRoot + "Test/hgsbugfnafnluaj");
            //
            // f.CreateOrGetFolder("Bucket");
            // for (int i = 0; i < 50; i++)
            // {
            //     f.CreateOrGetFile("int", i.ToString()).As<int>(out var p);
            //     p.Write(i);
            // }
            //
            // t.Write(new TestData(1, 2));
        }
        
    }
}