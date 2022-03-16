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
            if (DataFile.CreateOrGet(FSPath.StaticPathRoot.Forward("/.test")).As<TestData>(out var t))
            {
                var d = t.Read();
                Debug.Log($"{d.a}");
            }
            
            t.Write(new TestData(1, 2));
            man.Container.WriteStatic();
        }
    }
}