using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Ca2didi.JsonFSDataSystem.Exceptions;
using Ca2didi.JsonFSDataSystem.FS;
using Newtonsoft.Json.Linq;

namespace Ca2didi.JsonFSDataSystem
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
            Root.DeleteAllFiles();
            Root.DeleteAllFolders();
            
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
        
        
        public async Task WriteAsync(ContainerTicket ticket, string description = "")
        {
            if (ticket == null)
                throw new ArgumentNullException($"{nameof(ticket)}");
            DataManager.StartChecker();
            DeleteSafetyChecker();

            var sta = FSPath.IsStaticPath(ticket.RootPath);
            await DataManager.Instance.DoCallback(DataManagerCallbackTiming.BeforeWrite);
            
            await Task.Run(async () =>
            {
                await DataManager.Instance.FlushAllData();
                SaveTime = DateTime.UtcNow;
                Description = description;
                UpdateInternal((JObject) ticket.FSObject.DeepClone());

                if (fInf.Exists) fInf.Delete();
                WriteInternal(fInf.Create());
            });
        }

        public bool Delete(bool ignoreStatic = false)
        {
            DataManager.StartChecker();
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
            DataManager.StartChecker();
            DeleteSafetyChecker();
            
            if (jFS.Value.Type != JTokenType.Null)
            {
                return new ContainerTicket(
                    IsStatic ? FSPath.StaticPathRoot : FSPath.CurrentPathRoot,
                    (JObject) ((JProperty) jFS.DeepClone()).Value);
            }
            
            return new ContainerTicket(IsStatic ? FSPath.StaticPathRoot : FSPath.CurrentPathRoot);
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
            if (id < 0) ID = -1;
            else ID = id;

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
                jDescript.Value = JValue.CreateNull();
                jCreateTime.Value = JValue.CreateNull();
                jSaveTime.Value = JValue.CreateNull();
                jFS.Value = JValue.CreateNull();
                
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
                jDescript = new JProperty("Description", JValue.CreateNull());
                jRoot.Add(jDescript);
                
                jCreateTime = new JProperty("CreateTime", JValue.CreateNull());
                jRoot.Add(jCreateTime);
                
                jSaveTime = new JProperty("SaveTime", JValue.CreateNull());
                jRoot.Add(jSaveTime);
                
                jFS = new JProperty("FS", JValue.CreateNull());
                jRoot.Add(jFS);
            }
        }

        private void UpdateInternal(JObject fsRoot)
        {
            lock (_fsOpeLocker)
            {
                jDescript.Value = JValue.CreateNull();
                jCreateTime.Value = JValue.CreateNull();
                jSaveTime.Value = JValue.CreateNull();
                jFS.Value = JValue.CreateNull();
                
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
                    var ser = DataManager.Instance.serializer;
                    jRoot = JObject.Parse(json);

                    var jDes = jRoot["Description"];
                    if (jDes != null)
                    {
                        jDescript = (JProperty) jDes.Parent;
                        Description = jDescript.Value.ToObject<string>(ser);
                    }

                    var jST = jRoot["SaveTime"];
                    if (jST != null)
                    {
                        jSaveTime = (JProperty) jST.Parent;
                        SaveTime = jSaveTime.Value.ToObject<DateTime>(ser);
                    }

                    var jCT = jRoot["CreateTime"];
                    if (jCT != null)
                    {
                        jCreateTime = (JProperty) jCT.Parent;
                        CreateTime = jCreateTime.Value.ToObject<DateTime>(ser);
                    }

                    var jF = jRoot["FS"];
                    if (jF != null)
                        jFS = (JProperty) jF.Parent;
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
                DataManager.StartChecker();
                DiskScanSafetyChecker();
                return _staticContainer;
            }
        }

        public ContainerTicket CurrentContainer
        {
            get
            {
                DataManager.StartChecker();
                DiskScanSafetyChecker();
                return _currentContainer;
            }
        }

        /// <summary>
        /// Create a new container and set it as current container.It will automatically dispose older current container.
        /// </summary>
        /// <returns>New container</returns>
        public async Task<ContainerTicket> NewContainerAsync()
        {
            DataManager.StartChecker();
            DiskScanSafetyChecker();

            var dispose = DisposeCurrentContainerAsync();
            
            _currentContainer = await Task.Run(() => new ContainerTicket(FSPath.CurrentPathRoot));
            await DataManager.Instance.DoCallback(DataManagerCallbackTiming.AfterNew);
            
            await dispose;
            return _currentContainer;
        }

        /// <summary>
        /// Use an existed disk ticket to create a container with data filled and set it as current container.<br/>It will automatically dispose older current container.
        /// </summary>
        /// <param name="ticket">New container with data</param>
        /// <returns>If ticket is refer to a static or disposed container, it will return null.</returns>
        /// <exception cref="ArgumentNullException">If ticket is null, report it.</exception>
        public ConfiguredTaskAwaitable<ContainerTicket> UseContainerAsync(DiskTicket ticket)
        {
            DataManager.StartChecker();
            DiskScanSafetyChecker();
            return Task.Run(async () =>
            {
                if (ticket == null)
                    throw new ArgumentNullException(nameof(ticket));

                if (ticket.IsStatic || ticket.IsDisposed)
                    return null;

                var dispose = DisposeCurrentContainerAsync();

                _currentContainer = await Task.Run(ticket.Construct);
                await DataManager.Instance.DoCallback(DataManagerCallbackTiming.AfterRead);

                await dispose;
                return _currentContainer;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Destroy current container.It will automatically dispose older current container.
        /// </summary>
        public ConfiguredTaskAwaitable DestroyCurrentContainerAsync()
        {
            DataManager.StartChecker();
            DiskScanSafetyChecker();

            return DisposeCurrentContainerAsync();
        }

        private ConfiguredTaskAwaitable DisposeCurrentContainerAsync()
        {
            if (_currentContainer != null)
            {
                var c = _currentContainer;
                _currentContainer = null;
                return Task.Run(c.Dispose).ConfigureAwait(false);
            }
            
            return Task.CompletedTask.ConfigureAwait(false);
        }
        
        /// <summary>
        /// Using FSPath to get root folder
        /// </summary>
        /// <returns>Root folder this FSPath refer to.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The container of this FSPath is invalid.</exception>
        public DataFolder GetRootFolder(FSPath path)
        {
            DataManager.StartChecker();
            DiskScanSafetyChecker();
            
            if (path.ContainerName == "static")
                return _staticContainer.Root;

            if (path.ContainerName == "current")
            {
                if (_currentContainer == null)
                    throw new InvalidOperationException(
                        "You must set a container or create a new container to access current://");
                
                return _currentContainer.Root;
            }

            throw new FSPathNotExistException(path);
        }

        /// <summary>
        /// Clean dirty flag of all files and make sure changes has been committed to json tree.
        /// </summary>
        /// <returns></returns>
        public ConfiguredTaskAwaitable RefreshContainer() => DataManager.Instance.FlushAllData();

        #endregion
        
        #region DiskManage

        private readonly object _diskLocker = new object();
        private List<DiskTicket> _diskTicket;
        private DiskTicket _staticDiskTicket;
        private HashSet<int> _usedNum;
        
        /// <summary>
        /// A shortcut to write static data.
        /// </summary>
        public async Task WriteStaticAsync()
        {
            DataManager.StartChecker();
            
            await _staticDiskTicket.WriteAsync(_staticContainer);
        }

        /// <summary>
        /// Create a new disk ticket to save container.
        /// </summary>
        /// <returns>If meet limitation of disk ticket count, it will return null.</returns>
        public DiskTicket CreateDiskTicket()
        {
            DataManager.StartChecker();
            
            lock (_diskLocker)
            {
                var setting = DataManager.Instance.setting;
                var path = $"{setting.GameRootDirectoryPath}{setting.GameDataRelativeDirectoryPath}";
                var lim = setting.MaxGameDataCount > 0
                    ? setting.MaxGameDataCount : Int32.MaxValue - 1;

                var i = 0;
                for (; _usedNum.Contains(i) && i <= lim ; i++);
                if (i > lim) return null;
                _usedNum.Add(i);

                var inf = new FileInfo($"{path}/{setting.DataFileNamingRule.GenerateDataFileName((uint) i)}");
                var ticket = new DiskTicket(i, inf);
                
                _diskTicket.Add(ticket);
                return ticket;
            }
        }
        
        public DiskTicket[] GetAllDiskTickets()
        {
            DataManager.StartChecker();
            DiskScanSafetyChecker();
            
            return _diskTicket.FindAll(m => !m.IsDisposed).ToArray();
        }

        public DiskTicket GetStaticDiskTicket()
        {
            DataManager.StartChecker();
            DiskScanSafetyChecker();
            return _staticDiskTicket;
        }

        internal void ScanJsonFile()
        {
            DiskTicket stct = null;
            var ts = new List<DiskTicket>();
            var setting = DataManager.Instance.setting;
            var path = $"{setting.GameRootDirectoryPath}{setting.GameDataRelativeDirectoryPath}";
            var gdFileName = setting.DataFileNamingRule.GenerateGlobalDataFileName();
            var nums = new HashSet<int>();

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
                        stct = new DiskTicket(-1, inf);
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
                _staticContainer?.Dispose();
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

        internal void ScanBinders()
        {
            var bid = new List<DataTypeBinder>();
            var map = new Hashtable();

            var typDef = new List<JsonTypeDefine>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in asm.GetTypes())
            {
                var attr = type.GetCustomAttributes(typeof(JsonTypeDefine), false) as JsonTypeDefine[];
                if (attr == null && attr.Length == 0)
                    continue;

                typDef.AddRange(attr);
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
        }

        /// <summary>
        /// Find a binder via it's type string.
        /// </summary>
        /// <returns>If not exist, return null.</returns>
        public DataTypeBinder GetBinder(string typeStr)
        {
            DataManager.StartChecker();
            BinderScanChecker();

            lock (_typeBinderLocker)
            {
                var idx = typeStrMap[typeStr];
                return (idx is int) ? binder[(int) idx] : null;
            }
        }

        /// <summary>
        /// Add type binder in runtime
        /// </summary>
        /// <param name="typ">Type</param>
        /// <param name="typeStr">Type string</param>
        /// <returns>If string is invalid, return null</returns>
        public DataTypeBinder AddBinder(Type typ, string typeStr)
        {
            DataManager.StartChecker();
            BinderScanChecker();

            typeStr = typeStr.Trim();
            if (string.IsNullOrEmpty(typeStr))
                return null;

            lock (_typeBinderLocker)
            {
                if (typeStrMap.Contains(typeStr))
                    return null;

                var idx = binder.FindIndex(m => m.ActualType == typ);
                if (idx < 0)
                {
                    var b = new DataTypeBinder(new JsonTypeDefine(typ, typeStr));
                    typeStrMap.Add(typeStr, binder.Count);
                    binder.Add(b);

                    return b;
                }
                
                typeStrMap.Add(typeStr, idx);
                return binder[idx];
            }
        }
        
        public DataTypeBinder[] GetBinders(Predicate<DataTypeBinder> match = null)
        {            
            DataManager.StartChecker();
            BinderScanChecker();

            lock (_typeBinderLocker)
            {
                if (match == null)
                    return binder.ToArray();

                return binder.FindAll(match).ToArray();
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