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
            
            await man.Container.UseContainerAsync(man.Container.GetAllDiskTickets()[0]);
            
            if (DataFile.CreateOrGet(FSPath.CurrentPathRoot.Forward("/.test")).As<TestData>(out var t))
            {
                var d = t.Read();
                Debug.Log($"{d.a}");
            }
            
            t.Write(new TestData(1, 2));
            man.AddBeforeWriteEvent(() =>
            {
                Debug.Log("Before Write");
            });

            await man.Container.CreateDiskTicket().WriteAsync(man.Container.CurrentContainer);
        }
    }
}