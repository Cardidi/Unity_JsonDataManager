using System;
using UnityEngine;
using xyz.ca2didi.Unity.JsonDataManager;
using xyz.ca2didi.Unity.JsonDataManager.FS;

namespace Test
{
    public class FSTestingBoot : MonoBehaviour
    {

        private DataFile.Operator<Vector3> position;

        private void Awake()
        {

            DataManager.StartNew().SetDevelopment().CommitAsync();

            var path = FSPath.StaticPathRoot.Forward("/TestPosition.vec3");
            if (DataFile.CreateOrGet(path).As(out position))
                transform.position = position.Read();

        }

        private void OnDestroy()
        {
            position.Write(transform.position);
            DataManager.Instance.Container.WriteStatic();
        }
    }
}