using System;
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
            man.AddAfterReadEvent(() =>
            {
                Debug.Log("After Read");
            });
            
            if (DataFile.CreateOrGet(FSPath.StaticPathRoot.Forward("/.test")).As<TestData>(out var t))
            {
                var d = t.Read();
                Debug.Log($"{d.a}");
            }

            var f = DataFolder.CreateOrGet(FSPath.StaticPathRoot.Forward("/Test/hgsbugfnafnluajcrhavvnhmauhcruscpacocjhurscatvgbyu"));

            f.CreateOrGetFolder("/Bucket");
            for (int i = 0; i < 50; i++)
            {
                f.CreateOrGetFile("int", i.ToString()).As<int>(out var p);
                p.Write(i);
            }
            
            t.Write(new TestData(1, 2));
            man.AddBeforeWriteEvent(() =>
            {
                Debug.Log("Before Write");
            });

            await man.Container.WriteStaticAsync();
        }

        private void OnDestroy()
        {
            DataManager.Instance.CloseContainer();
        }
    }
}