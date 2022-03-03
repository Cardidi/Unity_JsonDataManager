using System;
using System.Diagnostics;
using UnityEngine;
using xyz.ca2didi.Unity.JsonDataManager.FS;
using Debug = UnityEngine.Debug;

namespace xyz.ca2didi.Unity.Test
{
    public class FSPathTester : MonoBehaviour
    {
        public string URL;

        private void OnEnable()
        {
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                var fs = new FSPath(URL);
            }
            Debug.Log(watch.ElapsedMilliseconds);
        }
    }
}