using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace xyz.ca2didi.Unity.JsonDataManager.FS
{
    public class DataContainer
    {

        #region DataPoolManager

        private readonly object _bridgeUsingLocker = new object();
        private DataJsonBridge _staticBridge;
        private List<DataJsonBridge> _bridges;
        private HashSet<int> _numUsage;
        private DataJsonBridge _current;

        public class ContainerTicket
        {
            public string ContainerName => RefBridge.ContainerName;
            public bool IsStatic => RefBridge.Id == -1;
            public FSPath RootPath => RefBridge.ContainerPath;
            public DataFolder RootFolder => RefBridge.RootFolder;
            internal  DataJsonBridge RefBridge { get; private set; }

            internal ContainerTicket(DataJsonBridge brg) => RefBridge = brg;

            internal void Dispose(DataJsonBridge brg)
            {
                if (RefBridge == brg)
                    RefBridge = null;
            }
        }
        
        internal class DataJsonBridge
        {
            /*
             * JSON structure:
             *
             * {
             *      "savetime" : DateTime,
             *      "fs" : {}
             * }
             * 
             */

            internal readonly ContainerTicket Ticket;
            internal readonly int Id;
            internal readonly string ContainerName;
            internal readonly FSPath ContainerPath;

            private DataFolder _folder;
            internal DataFolder RootFolder
            {
                get
                {
                    if (!Loaded)
                        throw new InvalidOperationException(
                            "You can not fetch a root folder from an unloaded container!");

                    return _folder;
                }
                private set => _folder = value;
            }

            internal DateTime SaveTime { get; private set; }
            internal bool Loaded { get; private set; }

            private FileInfo _info;
            private JObject _root;
            private readonly object _swapLocker = new object();
            
            internal DataJsonBridge(int id, FileInfo info, bool newCreation = false)
            {
                Id = id;
                _info = info;
                ContainerName = Id >= 0 ? $"container{Id}" : "static";
                ContainerPath = new FSPath($"{ContainerName}://");
                
                Loaded = newCreation;
                if (newCreation)
                {
                    SaveTime = DateTime.UtcNow;
                    RootFolder = new DataFolder(ContainerPath, out var fs);
                    _root = new JObject();
                    _root.Add(new JProperty("savetime", SaveTime));
                    _root.Add(new JProperty("fs", fs));
                }

                Ticket = new ContainerTicket(this);
            }

            internal Task Load()
            {
                if (Loaded)
                    return Task.FromException(new InvalidOperationException("This container has been loaded!"));
                
                return Task.Run(() =>
                {
                    lock (_swapLocker)
                    {
                        using (var reader = new StreamReader(_info.OpenRead()))
                            _root = JObject.Parse(reader.ReadToEnd());

                        var fs = (_root["fs"] as JProperty)?.Value as JObject;
                        if (fs == null)
                            throw new Exception();

                        SaveTime = (_root["savetime"] as JProperty).Value<DateTime>();
                        RootFolder = new DataFolder(ContainerPath, fs);
                        Loaded = true;
                    }
                });
            }
            
            internal Task WriteDisk()
            {                
                if (!Loaded)
                    return Task.FromException(new InvalidOperationException("You can not write an unloaded container!"));
                
                return Task.Run(() =>
                {
                    lock (_swapLocker)
                    {
                        SaveTime = DateTime.UtcNow;
                        (_root["savetime"] as JProperty).Value = SaveTime;
                        
                        if (_info.Exists)
                            using (var writer = new StreamWriter(_info.OpenWrite()))
                                writer.Write(_root.ToString());
                        else
                            using (var writer = new StreamWriter(_info.Create()))
                                writer.Write(_root.ToString());
                    }
                });
                
            }

        }
        
        public Task ScanJsonFile()
        {
            DataManager.SafetyStartChecker();

            return Task.Run(() =>
            {
                DataJsonBridge staticBrg = null;
                var brg = new List<DataJsonBridge>();
                var setting = DataManager.Instance.setting;
                var path = $"{setting.GameRootDirectoryPath}{setting.GameDataRelativeDirectoryPath}";
                var gdFileName = setting.DataFileNamingRule.GenerateGlobalDataFileName();
                var nums = new HashSet<int>();

                lock (_bridgeUsingLocker)
                {
                    _staticBridge?.Ticket.Dispose(_staticBridge);
                    foreach (var beg in _bridges)
                    {
                        beg.Ticket.Dispose(beg);
                    }
                    
                    // If root directory is existed
                    if (Directory.Exists(path))
                    {
                        var rootDir = new DirectoryInfo(path);

                        // Scan json file in this directory
                        foreach (var inf in rootDir.GetFiles())
                        {
                            int id = setting.DataFileNamingRule.MatchDataFileID(inf.Name);
                            if (id >= 0)
                            {
                                nums.Add(id);
                                var jdBridge = new DataJsonBridge(id, inf);
                                brg.Add(jdBridge);
                            }
                            else if (staticBrg == null && inf.Name == gdFileName)
                            {
                                staticBrg = new DataJsonBridge(-1, inf);
                            }
                        }

                        // If no static json: create it
                        if (staticBrg == null)
                        {
                            var staticInfo =
                                new FileInfo($"{path}/{setting.DataFileNamingRule.GenerateGlobalDataFileName()}");
                            staticBrg = new DataJsonBridge(-1, staticInfo, true);
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(path);
                        var staticInfo =
                            new FileInfo($"{path}/{setting.DataFileNamingRule.GenerateGlobalDataFileName()}");
                        staticBrg = new DataJsonBridge(-1, staticInfo, true);
                    }

                    _staticBridge = staticBrg;
                    _current = null;
                    _numUsage = nums;
                    _bridges = brg;
                }
            });
        }
        
        public DataFolder TranslateContainer(FSPath path)
            => TranslateContainer(path.ContainerName);
        
        public DataFolder TranslateContainer(string containerName)
        {
            DataManager.SafetyStartChecker();

            if (containerName == "static")
                lock (_bridgeUsingLocker)
                    return _staticBridge.RootFolder;

            if (containerName == "current")
                lock (_bridgeUsingLocker)
                    return GetCurrentContainer()?.RootFolder;
            
            lock (_bridgeUsingLocker)
                return _bridges.Find(m => m.ContainerName == containerName)?.RootFolder;
        }

        public ContainerTicket GetCurrentContainer()
        {
            DataManager.SafetyStartChecker();

            lock (_bridgeUsingLocker)
                return _current?.Ticket;
        }

        public bool SetCurrentContainer(ContainerTicket ticket)
        {
            DataManager.SafetyStartChecker();

            if (ticket == null)
                return false;
            
            lock (_bridgeUsingLocker)
            {
                if (ticket == _current?.Ticket)
                    return false;
                
                var brg = _bridges.Find(m => m.Ticket == ticket);
                if (brg != null)
                {
                    _current = brg;
                    return true;
                }

                return false;   
            }
        }

        public bool DeleteContainer(ContainerTicket ticket)
        {
            DataManager.SafetyStartChecker();

            if (ticket == null)
                return false;
            
            lock (_bridgeUsingLocker)
            {
                if (ticket.IsStatic)
                    return false;
                
                if (_current?.Ticket == ticket)
                {
                    _bridges.Remove(_current);
                    _current = null;
                    return true;
                }
                
                return _bridges.Remove(ticket.RefBridge);
            }
        }

        public ContainerTicket[] GetContainers()
        {
            DataManager.SafetyStartChecker();
            
            lock (_bridgeUsingLocker)
            {
                var l = new List<ContainerTicket>();
                l.Add(_staticBridge.Ticket);
                foreach (var b in _bridges)
                {
                    l.Add(b.Ticket);
                }

                return l.ToArray();
            }
        }
        
        public ContainerTicket GenerateNewContainer()
        {
            DataManager.SafetyStartChecker();
            
            lock (_bridgeUsingLocker)
            {
                var setting = DataManager.Instance.setting;
                var path = $"{setting.GameRootDirectoryPath}{setting.GameDataRelativeDirectoryPath}";
                var lim = setting.MaxGameDataCount > 0
                    ? setting.MaxGameDataCount
                    : Int32.MaxValue;
                
                DataJsonBridge item = null;
                
                for (var i = 0; i < lim; i++)
                {
                    if (!_numUsage.Contains(i))
                    {
                        item = new DataJsonBridge(
                            i, 
                            new FileInfo($"{path}/{setting.DataFileNamingRule.GenerateDataFileName((uint)i)}"), 
                            true);
                        
                        _bridges.Add(item);
                        _numUsage.Add(i);
                        break;
                    }
                }

                if (item == null)
                    throw new Exception();

                return item.Ticket;
            }
        }

        #endregion
        
        #region TypeBinderMethods

        private static readonly object _typeBinderLocker = new object();
        private List<DataTypeBinder> binder;
        private Hashtable typeStrMap;

        public Task ScanBinders()
        {
            DataManager.SafetyStartChecker();

            return Task.Run(() =>
            {
                var bid = new List<DataTypeBinder>();
                var map = new Hashtable();

                var typDef = new List<JsonTypeDefine>();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var def = Array.FindAll(asm.GetCustomAttributes(typeof(JsonTypeDefine), false),
                        o => o is JsonTypeDefine) as JsonTypeDefine[];
                    if (def == null)
                        continue;

                    typDef.AddRange(def);
                }
                
                for (var i = 0; i < typDef.Count; i++)
                {
                    var item = new DataTypeBinder(typDef[i]);
                    bid.Add(item);
                    map.Add(item.JsonElement, i);
                }

                lock (_typeBinderLocker)
                {
                    binder = bid;
                    typeStrMap = map;
                }
            });
        }
        
        public DataTypeBinder GetBinder(string typeStr)
        {
            DataManager.SafetyStartChecker();

            lock (_typeBinderLocker)
            {
                if (binder == null)
                    throw new InvalidOperationException("You must scan binder first!");

                var idx = typeStrMap[typeStr];
                return !(idx is int) ? null : binder[(int) idx];
            }
        }

        #endregion
    }

}