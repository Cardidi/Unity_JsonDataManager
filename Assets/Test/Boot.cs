using System;
using System.Threading.Tasks;
using UnityEngine;
using xyz.ca2didi.Unity.JsonFSDataSystem;
using xyz.ca2didi.Unity.JsonFSDataSystem.FS;

namespace xyz.ca2didi.Unity.Test
{
    public class Boot : MonoBehaviour
    {
        private async void Awake()
        {
            var man = await DataManager.CreateNew().BootContainerAsync();
            man.AddCallback(timing =>
            {
                switch (timing)
                {
                    case DataManagerCallbackTiming.AfterReadCurrent:
                    case DataManagerCallbackTiming.AfterReadStatic:
                        Debug.Log("Loaded");break;
                    
                    case DataManagerCallbackTiming.BeforeWriteCurrent:
                    case DataManagerCallbackTiming.BeforeWriteStatic:
                        Debug.Log("Save");break;
                }

                return null;
            });
            
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

        private async void OnDestroy()
        {
            await DataManager.Instance.CloseContainerAsync();
        }
    }
}