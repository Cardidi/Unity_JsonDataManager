using System.Diagnostics;
using UnityEngine;
using xyz.ca2didi.Unity.JsonDataManager.FS;
using Debug = UnityEngine.Debug;

namespace Test
{
    public class FSPathTester : MonoBehaviour
    {
        public string URL;

        private void OnEnable()
        {
            var watch = Stopwatch.StartNew();
            FSPath fs;
            for (int i = 0; i < 1000; i++)
            {
                fs = new FSPath(URL);
            }
            Debug.Log(fs);
            Debug.Log(watch.ElapsedMilliseconds);
            //Debug.Log(new FSPath(URL));
        }

        private void OnValidate()
        {
            // var watch = Stopwatch.StartNew();
            // var types =
            //     AppDomain.CurrentDomain.GetAssemblies()
            //         .SelectMany(x => x.GetTypes()).Where(y => typeof(BaseData).IsAssignableFrom(y) && !y.IsAbstract);
            //
            // Debug.Log(watch.ElapsedMilliseconds);
            // foreach (var Type in types)
            // {
            //     Debug.Log($"type {Type.FullName} is BaseData");
            //     foreach (var o in Type.GetCustomAttributes(false))
            //     {
            //         var binder = o as JsonTypeBinder;
            //         if (binder != null)
            //         {
            //             Debug.Log($"Json element type is {binder.JsonElementTag}");
            //         }
            //     }
            // }
        }
    }
}