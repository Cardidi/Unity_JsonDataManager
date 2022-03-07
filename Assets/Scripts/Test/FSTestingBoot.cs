using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using xyz.ca2didi.Unity.JsonDataManager;
using xyz.ca2didi.Unity.JsonDataManager.FS;

namespace Test
{
    public class FSTestingBoot : MonoBehaviour
    {
        private void Awake()
        {

            var man = DataManager.StartNew().CommitAsync();

            var dt = man.Container.GetAllDiskTickets();
            if (dt.Length > 0)
            {
                man.Container.UseContainer(dt[0]);
                
            }
            else
            {
                var contianer = man.Container.NewContainer();
                var disk = man.Container.CreateDiskTicket();
                var folder = DataFolder.CreateOrGetFolder(new FSPath("/Test/A"));
                folder.CreateOrGetChildFolder("SectionB");
            
                disk.Write(contianer, "A testing object.");
            }
            man.Container.WriteStatic();

        }
    }
}