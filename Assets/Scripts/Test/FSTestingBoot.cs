using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using xyz.ca2didi.Unity.JsonDataManager;
using xyz.ca2didi.Unity.JsonDataManager.FS;

namespace Test
{
    [JsonTypeDefine(typeof(int), "int")]
    [JsonTypeDefine(typeof(float), "float")]
    [JsonTypeDefine(typeof(bool), "bool")]
    [JsonTypeDefine(typeof(string), "str")]
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
                
                /*
                 * DataFolder.CreateOrGetDataFolder(new FSPath());
                 * current://Actor/Player/DeathTimes.int
                 *
                 * current://EventsTicket/Main/Scene1/Disabled/TalkWithPlayer_1.trigger
                 * 
                 */
                
                var contianer = man.Container.NewContainer();
                var disk = man.Container.CreateDiskTicket();
                var folder = DataFolder.CreateOrGetFolder(new FSPath("/EventTicket/Main"));
            
                disk.Write(contianer, "A testing object.");
            }

            var file = DataFile.CreateOrGetFile(FSPath.CurrentContainerFSPathRoot.NavToward("/day.int"));
            if (file.OperateAs<int>(out var day))
            {
                if (!day.IsEmpty)
                    Debug.Log(day.Read());
                day.Write(10);
            }

            dt[0].Write(DataManager.Instance.Container.CurrentContainer);
            man.Container.WriteStatic();
        }
    }
}