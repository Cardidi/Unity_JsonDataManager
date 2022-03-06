using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using xyz.ca2didi.Unity.JsonDataManager.FS;

namespace xyz.ca2didi.Unity.JsonDataManager
{

    /// <summary>
    /// Class to handle container data.
    /// </summary>
    public class ContainerTicket
    {
        internal JObject FSObject { get; private set; }

        public FSPath RootPath { get; private set; }

        public DataFolder Root { get; private set; }
        

        public bool IsDisposed => Root == null;

        internal ContainerTicket(FSPath rootPath)
        {
            RootPath = rootPath;
            Root = new DataFolder(rootPath, out var fsObj);
            FSObject = fsObj;
        }

        internal ContainerTicket(FSPath rootPath, JObject fsRoot)
        {
            RootPath = rootPath;
            Root = new DataFolder(rootPath, fsRoot);
            FSObject = fsRoot;
        }

        internal void Dispose()
        {
            Root.DeleteAllChildFiles();
            Root.DeleteAllChildFolders();
            
            FSObject = null;
            RootPath = default;
            Root = null;
        }
    }

    /// <summary>
    /// Class to refer data json in disk, and manage it to between ContainerTicket and Disk.
    /// </summary>
    public class DiskTicket
    {
        public readonly int ID;

        public bool IsStatic => ID < 0;
        
        public bool IsDisposed { get; private set; }

        public string Description { get; private set; }
        
        public DateTime SaveTime { get; private set; }
        
        public DateTime CreateTime { get; private set; }
        
        
        public Task WriteAsync(ContainerTicket ticket, string description = "")
        {
            DataManager.SafetyStartChecker();
            DeleteSafetyChecker();
            
            return Task.Run(() => Write(ticket, description));
        }

        public void Write(ContainerTicket ticket, string description = "")
        {

            if (ticket == null)
                throw new NullReferenceException($"{nameof(ticket)}");

            SaveTime = DateTime.UtcNow;
            Description = description;
            UpdateInternal(ticket.FSObject);
            
            if (fInf.Exists) fInf.Delete();
            WriteInternal(fInf.Create());
        }

        public bool Delete(bool ignoreStatic = false)
        {
            DataManager.SafetyStartChecker();
            DeleteSafetyChecker();
            
            if (IsStatic && ignoreStatic)
                return false;
            
            lock (_fsOpeLocker)
                if (fInf.Exists) fInf.Delete();

            DisposeInternal();
            
            return true;
        }

        internal ContainerTicket Construct()
        {
            DataManager.SafetyStartChecker();
            DeleteSafetyChecker();
            
            if (jFS.HasValues)
            {
                return new ContainerTicket(
                    IsStatic ? FSPath.StaticContainerFSPathRoot : FSPath.CurrentContainerFSPathRoot,
                    (JObject) jFS.Value);
            }
            else
            {
                return new ContainerTicket(IsStatic ? FSPath.StaticContainerFSPathRoot : FSPath.CurrentContainerFSPathRoot);
            }
        }

        private FileInfo fInf;

        private readonly object _fsOpeLocker = new object();
        private JObject jRoot;
        private JProperty jDescript;
        private JProperty jSaveTime;
        private JProperty jCreateTime;
        private JProperty jFS;

        internal DiskTicket(int id, FileInfo info)
        {
            if (id < 0)
                id = -1;

            fInf = info;
            if (fInf.Exists)
            {
                ReadInternal(fInf.OpenRead());
            }
            else
            {
                CreateTime = SaveTime = DateTime.UtcNow;
                Description = "";
                CreateInternal();
            }
        }

        private void DeleteSafetyChecker()
        {
            if (IsDisposed)
                throw new InvalidOperationException(
                    "You can not operate this to an deleted object! You should remove reference to this object.");
        }

        private void DisposeInternal()
        {
            lock (_fsOpeLocker)
            {
                fInf = null;
                
                jRoot.RemoveAll();
                jDescript.RemoveAll();
                jCreateTime.RemoveAll();
                jSaveTime.RemoveAll();
                jFS.RemoveAll();
                
                jRoot = null;
                jDescript = null;
                jCreateTime = null;
                jSaveTime = null;
                jFS = null;
                
                Description = null;
                SaveTime = default;
                CreateTime = default;
                
                IsDisposed = true;
            }
        }
        
        private void CreateInternal()
        {
            lock (_fsOpeLocker)
            {
                jRoot = new JObject();
                jDescript = new JProperty("Description");
                jRoot.Add(jDescript);
                
                jCreateTime = new JProperty("CreateTime");
                jRoot.Add(jCreateTime);
                
                jSaveTime = new JProperty("SaveTime");
                jRoot.Add(jSaveTime);
                
                jFS = new JProperty("FS");
                jRoot.Add(jFS);
            }
        }

        private void UpdateInternal(JObject fsRoot)
        {
            lock (_fsOpeLocker)
            {
                jDescript.RemoveAll();
                jCreateTime.RemoveAll();
                jSaveTime.RemoveAll();
                jFS.RemoveAll();
                
                jDescript.Value = new JValue(Description);
                jCreateTime.Value = new JValue(CreateTime);
                jSaveTime.Value = new JValue(SaveTime);
                jFS.Value = fsRoot;
            }
        }

        private void ReadInternal(FileStream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                lock (_fsOpeLocker)
                {
                    var json = reader.ReadToEnd();
                    jRoot = JObject.Parse(json);
                    
                    jDescript.RemoveAll();
                    jCreateTime.RemoveAll();
                    jSaveTime.RemoveAll();
                    jFS.RemoveAll();
                        
                    jDescript.Value = ((JProperty) jRoot["Description"]).Value;
                    Description = jDescript.Value<string>();
                        
                    jSaveTime.Value = ((JProperty) jRoot["SaveTime"]).Value;
                    SaveTime = jSaveTime.Value<DateTime>();
                        
                    jCreateTime.Value = ((JProperty) jRoot["CreateTime"]).Value;
                    CreateTime = jCreateTime.Value<DateTime>();
                        
                    jFS.Value = ((JProperty) jRoot["FS"]).Value;
                }
            }
        }

        private void WriteInternal(FileStream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                var ser = DataManager.Instance.serializer;
                lock (_fsOpeLocker)
                {
                    writer.Write(jRoot.ToString(ser.Formatting, ser.Converters.ToArray()));
                    writer.Flush();
                    writer.Close();
                }
            }
        }

        internal void Dispose()
        {
            if (!IsDisposed) DisposeInternal();
        }
    }
    
    /// <summary>
    /// The controller class to handle with DiskTicket and ContainerTicket.
    /// </summary>
    public class DataContainer
    {
        #region ContainerManage
        
        private ContainerTicket _staticContainer, _currentContainer;
        
        public bool HasActiveCurrentContainer => _currentContainer != null;

        public ContainerTicket StaticContainer
        {
            get
            {
                DataManager.SafetyStartChecker();
                DiskScanSafetyChecker();
                return _staticContainer;
            }
        }

        public ContainerTicket CurrentContainer
        {
            get
            {
                DataManager.SafetyStartChecker();
                DiskScanSafetyChecker();
                return _currentContainer;
            }
        }

        /// <summary>
        /// Create a new container and set it as current container.It will automatically dispose older current container.
        /// </summary>
        /// <returns>New container</returns>
        public ContainerTicket NewContainer()
        {
            DataManager.SafetyStartChecker();
            DiskScanSafetyChecker();

            lock (_diskLocker)
            {
                _currentContainer?.Dispose();
                _currentContainer = new ContainerTicket(FSPath.CurrentContainerFSPathRoot);
                return _currentContainer;
            }
        }

        /// <summary>
        /// Use an existed disk ticket to create a container with data filled and set it as current container.It will automatically dispose older current container.
        /// </summary>
        /// <param name="ticket">New container with data</param>
        /// <returns>If ticket is refer to a static or disposed container, it will return null.</returns>
        /// <exception cref="ArgumentNullException">If ticket is null, report it.</exception>
        public ContainerTicket UseContainer(DiskTicket ticket)
        {
            DataManager.SafetyStartChecker();
            DiskScanSafetyChecker();
            if (ticket == null)
                throw new ArgumentNullException(nameof(ticket));

            if (ticket.IsStatic || ticket.IsDisposed)
                return null;
            
            lock (_diskLocker)
            {
                _currentContainer?.Dispose();
                _currentContainer = ticket.Construct();
                return _currentContainer;
            }
        }

        /// <summary>
        /// Destroy current container.It will automatically dispose older current container.
        /// </summary>
        public void DestroyCurrentContainer()
        {
            DataManager.SafetyStartChecker();
            DiskScanSafetyChecker();
            
            lock (_diskLocker)
            {
                _currentContainer?.Dispose();
                _currentContainer = null;
            }
        }
        
        /// <summary>
        /// Using FSPath to get root folder
        /// </summary>
        /// <returns>Root folder this FSPath refer to.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The container of this FSPath is invalid.</exception>
        public DataFolder GetRootFolder(FSPath path)
        {
            DataManager.SafetyStartChecker();
            DiskScanSafetyChecker();
            
            if (path.ContainerName == "static")
                return _staticContainer.Root;

            if (path.ContainerName == "current")
                return _currentContainer?.Root;

            throw new ArgumentOutOfRangeException(nameof(path));
        }

        #endregion
        
        #region DiskManage

        private readonly object _diskLocker = new object();
        private List<DiskTicket> _diskTicket;
        private DiskTicket _staticDiskTicket;
        private HashSet<int> _usedNum;

        /// <summary>
        /// Create a new disk ticket to save container.
        /// </summary>
        /// <returns>If meet limitation of disk ticket count, it will return null.</returns>
        public DiskTicket CreateDiskTicket()
        {
            DataManager.SafetyStartChecker();
            
            lock (_diskLocker)
            {
                var setting = DataManager.Instance.setting;
                var path = $"{setting.GameRootDirectoryPath}{setting.GameDataRelativeDirectoryPath}";
                var lim = setting.MaxGameDataCount > 0
                    ? setting.MaxGameDataCount
                    : Int32.MaxValue - 1;

                var i = 0;
                for (; _usedNum.Contains(i) && i <= lim ; i++);
                if (i > lim) return null;

                var inf = new FileInfo($"{path}/{setting.DataFileNamingRule.GenerateDataFileName((uint) i)}");
                var ticket = new DiskTicket(i, inf);
                
                _diskTicket.Add(ticket);
                return ticket;
            }
        }
        
        public DiskTicket[] GetAllDiskTickets()
        {
            DataManager.SafetyStartChecker();
            DiskScanSafetyChecker();
            
            return _diskTicket.FindAll(m => !m.IsDisposed).ToArray();
        }

        public DiskTicket GetStaticDiskTicket()
        {
            DataManager.SafetyStartChecker();
            DiskScanSafetyChecker();
            return _staticDiskTicket;
        }

        public Task ScanJsonFile()
        {
            DataManager.SafetyStartChecker();
            
            DiskTicket stct = null;
            var ts = new List<DiskTicket>();
            var setting = DataManager.Instance.setting;
            var path = $"{setting.GameRootDirectoryPath}{setting.GameDataRelativeDirectoryPath}";
            var gdFileName = setting.DataFileNamingRule.GenerateGlobalDataFileName();
            var nums = new HashSet<int>();

            return Task.Run(() =>
            {
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
                            var ticket = new DiskTicket(id, inf);
                            ts.Add(ticket);
                        }
                        else if (inf.Name == gdFileName)
                        {
                            if (stct == null)
                                stct = new DiskTicket(-1, inf);
                            else
                                throw new Exception();
                        }
                    }

                    // If no static json: create it
                    if (stct == null)
                    {
                        stct = new DiskTicket(
                            -1, 
                            new FileInfo($"{path}/{setting.DataFileNamingRule.GenerateGlobalDataFileName()}"));
                    }

                }
                else
                {
                    Directory.CreateDirectory(path);  
                    stct = new DiskTicket(
                        -1, 
                        new FileInfo($"{path}/{setting.DataFileNamingRule.GenerateGlobalDataFileName()}"));
                }
                
                lock (_diskLocker)
                {
                    _staticDiskTicket?.Dispose();
                    _staticDiskTicket = stct;
                    _staticContainer.Dispose();
                    _staticContainer = stct.Construct();
                    
                    _currentContainer?.Dispose();
                    _currentContainer = null;
                    
                    if (_diskTicket != null)
                    {
                        foreach (var t in _diskTicket)
                            t.Dispose();
                        _diskTicket.Clear();
                    }
                    _diskTicket = ts;
                    
                    _usedNum?.Clear();
                    _usedNum = nums;
                }
            });
        }

        private void DiskScanSafetyChecker()
        {
            if (binder == null)
                throw new InvalidOperationException("You must scan json file first to use disk ticket!");
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
                    binder?.Clear();
                    binder = bid;
                    
                    typeStrMap?.Clear();
                    typeStrMap = map;
                }
            });
        }
        
        public DataTypeBinder GetBinder(string typeStr)
        {
            DataManager.SafetyStartChecker();
            BinderScanChecker();

            lock (_typeBinderLocker)
            {
                if (binder == null)
                    throw new InvalidOperationException("You must scan binder first!");

                var idx = typeStrMap[typeStr];
                return (idx is int) ? binder[(int) idx] : null;
            }
        }

        private void BinderScanChecker()
        {
            if (binder == null)
                throw new InvalidOperationException("You must scan binder first to use binder");
        }

        #endregion
    }
}