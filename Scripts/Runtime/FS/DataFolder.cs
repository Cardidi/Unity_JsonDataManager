using System;
using System.Collections.Generic;
using System.Linq;
using Ca2didi.JsonFSDataSystem.Exceptions;
using Newtonsoft.Json.Linq;

namespace Ca2didi.JsonFSDataSystem.FS
{
    public class DataFolder
    {
        #region GlobalMethods

        public static DataFolder Get(FSPath path)
        {
            DataManager.StartChecker();
            FSCheck(path);
            
            var folder = DataManager.Instance.Container.GetRootFolder(path);
            foreach (var vec in path.DirectoryVector)
            {
                folder = folder.GetFolder(vec);
                if (folder == null)
                    return null;
            }

            return folder;
        }

        public static DataFolder CreateOrGet(FSPath path)
        {
            DataManager.StartChecker();
            FSCheck(path);
            
            var folder = DataManager.Instance.Container.GetRootFolder(path);
            foreach (var vec in path.DirectoryVector)
            {
                folder = folder.CreateOrGetFolder(vec);
            }

            return folder;
        }
        
        public static bool Exists(FSPath path)
        {
            DataManager.StartChecker();
            FSCheck(path);
            
            var folder = DataManager.Instance.Container.GetRootFolder(path);
            foreach (var vec in path.DirectoryVector)
            {
                folder = folder.GetFolder(vec);
                if (folder == null)
                    return false;
            }

            return true;
        }

        #endregion

        #region Runtime

        private DataFolderJsonBridge _bridge;
        private readonly object _fileExecuteLock = new object();
        private List<DataFile> _files = new List<DataFile>();
        private List<DataFolder> _folders = new List<DataFolder>();
                
        private class DataFolderJsonBridge
        {
            internal readonly JObject JThis;
            internal readonly JObject SubFolders, Files;

            internal DataFolderJsonBridge()
            {
                JThis = new JObject();
                SubFolders = new JObject();
                Files = new JObject();
                
                JThis.Add("folders", SubFolders);
                JThis.Add("files", Files);
            }

            internal DataFolderJsonBridge(JObject jThis)
            {
                JThis = jThis;

                SubFolders = jThis["folders"] as JObject;
                Files = jThis["files"] as JObject;
                if (SubFolders == null || Files == null)
                    throw new DataStructureBrokenException(jThis);
            }
        }

        // Create Empty Root Folder
        internal DataFolder(FSPath rootPath, out JObject jObject)
        {
            DataManager.StartChecker();
            
            _bridge = new DataFolderJsonBridge();
            IsRemoved = false;
            Parent = this;
            Path = rootPath;
            jObject = _bridge.JThis;
        }
        
        // Create Empty Folder
        private DataFolder(DataFolder parent, string folderName, out JObject jObject)
        {
            DataManager.StartChecker();

            _bridge = new DataFolderJsonBridge();
            IsRemoved = false;
            Parent = parent;
            Path = parent.Path.Forward($"/{folderName}");
            jObject = _bridge.JThis;
        }
        
        // Create Root Folder from existed data.
        internal DataFolder(FSPath rootPath, JObject jObject)
        {
            DataManager.StartChecker();

            _bridge = new DataFolderJsonBridge(jObject);
            IsRemoved = false;
            Parent = this;
            Path = rootPath;
            
            updateData();
        }
        
        // Create Folder with Data filled
        private DataFolder(DataFolder parent, string folderName, JObject jObject)
        {
            DataManager.StartChecker();

            _bridge = new DataFolderJsonBridge(jObject);
            IsRemoved = false;
            Parent = parent;
            Path = parent.Path.Forward($"/{folderName}");
            
            updateData();
        }

        private void updateData()
        {
            lock (_fileExecuteLock)
            {
                // Update Folders
                var folder = _bridge.SubFolders.Properties().ToArray();
                for (var i = 0; i < folder.Length; i++)
                {
                    _folders.Add(new DataFolder(this, folder[i].Name, (JObject) folder[i].Value));
                }

                // Update Files
                var file = _bridge.Files.Properties().ToArray();
                for (var i = 0; i < file.Length; i++)
                {
                    _files.Add(new DataFile(file[i], this));
                }
            }
        }

        private void RemovedCheck()
        {
            if (IsRemoved)
                throw new InvalidOperationException("Folder was removed! You can not operate it again.");
        }

        private static void FSCheck(FSPath path)
        {
            if (path.IsFilePath)
                throw new NotFolderPathException(path);
        }

        #endregion

        #region FolderInformation

        public string FolderName => Path.DirectoryName;
        public readonly FSPath Path;
        public readonly DataFolder Parent;
        public bool IsRemoved { get; private set; }
        public bool IsEmpty => _files.Count == 0 && _folders.Count == 0;

        #endregion


        #region FileOperation
        
        public static void NotFSPathStringException(string str, string paramName)
        {
            if (FSPath.IsFSPathString(str))
                throw new ArgumentException("You must give a non-FSPath string here!", paramName);
        }
        

        public DataFile CreateOrGetFile(string typeStr, string identify = "")
        {
            DataManager.StartChecker();
            RemovedCheck();
            NotFSPathStringException(typeStr, nameof(typeStr));
            NotFSPathStringException(identify, nameof(identify));

            lock (_fileExecuteLock)
            {
                var idx = _files.FindIndex(m => m.Path.FileIdentify == identify && m.Path.FileType == typeStr);
                if (idx >= 0)
                    return _files[idx];

                var file = new DataFile(identify, typeStr, this, out var property);
                _bridge.Files.Add(property);
                _files.Add(file);

                return file;
            }
        }

        public bool ExistsFile(string typeStr, string identify)
        {
            DataManager.StartChecker();
            RemovedCheck();
            NotFSPathStringException(typeStr, nameof(typeStr));
            NotFSPathStringException(identify, nameof(identify));

            lock (_fileExecuteLock)
            {
                return _files.Exists(m => m.Path.FileIdentify == identify && m.Path.FileType == typeStr);
            }
        }
        
        public bool DeleteFile(string identify, string typeStr)
        {            
            DataManager.StartChecker();
            RemovedCheck();
            NotFSPathStringException(typeStr, nameof(typeStr));
            NotFSPathStringException(identify, nameof(identify));

            lock (_fileExecuteLock)
            {
                var idx = _files.FindIndex(m => m.Path.FileIdentify == identify && m.Path.FileType == typeStr);
                if (idx < 0)
                    return false;

                var file = _files[idx];
                _files.RemoveAt(idx);
                _bridge.Files.Remove(file.FileName);
                file.Dropped();

                return true;
            }
        }
        
        public void DeleteAllFiles()
        {            
            DataManager.StartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
            {
                _bridge.Files.RemoveAll();
                foreach (var file in _files)
                    file.Dropped();

                _files.Clear();
            }
            
        }
        
        public DataFile GetFile(string identify, string typeStr)
        {
            DataManager.StartChecker();
            RemovedCheck();
            NotFSPathStringException(typeStr, nameof(typeStr));
            NotFSPathStringException(identify, nameof(identify));
            
            lock (_fileExecuteLock)
                return _files.Find(m => m.Path.FileIdentify == identify && m.Path.FileType == typeStr);
        }
        
        public DataFile[] GetFiles(Predicate<DataFile> match)
        {
            DataManager.StartChecker();
            RemovedCheck();
            
            lock (_fileExecuteLock)
                return _files.FindAll(match).ToArray();
        }
        
        public DataFile[] GetFiles()
        {
            DataManager.StartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
                return _files.ToArray();
        }

        #endregion

        #region FolderOperation

        public DataFolder CreateOrGetFolder(string name)
        {
            DataManager.StartChecker();
            RemovedCheck();
            NotFSPathStringException(name, nameof(name));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            
            lock (_fileExecuteLock)
            {
                var idx = _folders.FindIndex(m => m.FolderName == name);
                if (idx >= 0)
                    return _folders[idx];

                var f = new DataFolder(this, name, out var jF);
                _bridge.SubFolders.Add(new JProperty(name, jF));
                _folders.Add(f);
                return f;
            }
        }

        public bool DeleteFolder(string name)
        {
            DataManager.StartChecker();
            RemovedCheck();
            NotFSPathStringException(name, nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            
            lock (_fileExecuteLock)
            {
                var idx = _folders.FindIndex(m => m.FolderName == name);
                if (idx < 0)
                    return false;

                var folder = _folders[idx];
                _folders.RemoveAt(idx);
                _bridge.SubFolders.Remove(folder.FolderName);
                
                folder.DeleteAllFiles();
                folder.DeleteAllFolders();

                folder.IsRemoved = true;
            }

            return true;
        }

        public void DeleteAllFolders()
        {
            DataManager.StartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
            {
                _bridge.SubFolders.RemoveAll();
                foreach (var folder in _folders)
                {
                    folder.DeleteAllFiles();
                    folder.DeleteAllFolders();
                    folder.IsRemoved = true;
                }

                _folders.Clear();
            }
        }
        

        public bool ExistsFolder(string name)
        {
            DataManager.StartChecker();
            RemovedCheck();
            NotFSPathStringException(name, nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            
            lock (_fileExecuteLock)
                return _folders.FindIndex(m => m.FolderName == name) >= 0;
        }
        
        
        public DataFolder GetFolder(string name)
        {
            DataManager.StartChecker();
            RemovedCheck();
            NotFSPathStringException(name, nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            lock (_fileExecuteLock)
                return _folders.Find(m => m.FolderName == name);
        }

        public DataFolder[] GetFolders(Predicate<DataFolder> match)
        {
            DataManager.StartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
                return _folders.FindAll(match).ToArray();
        }


        public DataFolder[] GetFolders()
        {
            DataManager.StartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
                return _folders.ToArray();
        }

        #endregion
        
    }
}