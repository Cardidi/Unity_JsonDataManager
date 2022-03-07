using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace xyz.ca2didi.Unity.JsonDataManager.FS
{
    public class DataFolder
    {
        #region GlobalMethods
        
        
        public static DataFolder GetFolder(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            var folder = DataManager.Instance.Container.GetRootFolder(path);
            foreach (var vec in path.DirectoryVector)
            {
                folder = folder.GetChildFolder(vec);
                if (folder == null)
                    return null;
            }

            return folder;
        }

        public static DataFolder CreateOrGetFolder(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            var folder = DataManager.Instance.Container.GetRootFolder(path);
            foreach (var vec in path.DirectoryVector)
            {
                folder = folder.CreateOrGetChildFolder(vec);
            }

            return folder;
        }
        
        public static bool ExistsFolder(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            var folder = DataManager.Instance.Container.GetRootFolder(path);
            foreach (var vec in path.DirectoryVector)
            {
                folder = folder.GetChildFolder(vec);
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
                    throw new Exception();
            }
        }

        // Create Empty Root Folder
        internal DataFolder(FSPath rootPath, out JObject jObject)
        {
            DataManager.SafetyStartChecker();

            IsRemoved = false;
            Parent = this;
            Path = rootPath;
            _bridge = new DataFolderJsonBridge();
            jObject = _bridge.JThis;
        }
        
        // Create Empty Folder
        private DataFolder(DataFolder parent, string folderName, out JObject jObject)
        {
            DataManager.SafetyStartChecker();

            IsRemoved = false;
            Parent = parent;
            Path = parent.Path.NavToward($"/{folderName}");
            _bridge = new DataFolderJsonBridge();
            jObject = _bridge.JThis;
        }
        
        // Create Root Folder from existed data.
        internal DataFolder(FSPath rootPath, JObject jObject)
        {
            DataManager.SafetyStartChecker();

            IsRemoved = false;
            Parent = this;
            Path = rootPath;
            _bridge = new DataFolderJsonBridge(jObject);
            
            updateData();
        }
        
        // Create Folder with Data filled
        private DataFolder(DataFolder parent, string folderName, JObject jObject)
        {
            DataManager.SafetyStartChecker();

            IsRemoved = false;
            Parent = parent;
            Path = parent.Path.NavToward($"/{folderName}");
            _bridge = new DataFolderJsonBridge(jObject);
            
            updateData();
        }

        private void updateData()
        {
            lock (_fileExecuteLock)
            {
                // Update Folders
                foreach (var jToken in _bridge.SubFolders.Properties())
                {
                    _folders.Add(new DataFolder(this, jToken.Name, (JObject) jToken.Value));
                }
                
                // Update Files
                foreach (var jToken in _bridge.Files.Properties())
                {
                    _files.Add(new DataFile(jToken, this));
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
                throw new Exception();
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

        public DataFile CreateOrGetChildFile(string typeStr, string identify = "")
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

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

        public bool ExistsChildFile(string typeStr, string identify)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
            {
                return _files.Exists(m => m.Path.FileIdentify == identify && m.Path.FileType == typeStr);
            }
        }
        
        public bool DeleteChildFile(string identify, string typeStr)
        {            
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
            {
                var idx = _files.FindIndex(m => m.Path.FileIdentify == identify && m.Path.FileType == typeStr);
                if (idx < 0)
                    return false;

                var file = _files[idx];
                _files.RemoveAt(idx);
                _bridge.Files.Remove(file.FileName);
                file.IsDirty = false;
                file.IsRemoved = true;

                return true;
            }
        }
        
        public void DeleteAllChildFiles()
        {            
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
            {
                _bridge.Files.RemoveAll();
                foreach (var file in _files)
                {
                    file.IsDirty = false;
                    file.IsRemoved = true;
                }

                _files.Clear();
            }
            
        }
        
        public DataFile GetChildFile(string identify, string typeStr)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();
            
            lock (_fileExecuteLock)
                return _files.Find(m => m.Path.FileIdentify == identify && m.Path.FileType == typeStr);
        }
        
        public DataFile[] GetChildFiles(Predicate<DataFile> match)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();
            
            lock (_fileExecuteLock)
                return _files.FindAll(match).ToArray();
        }
        
        public DataFile[] GetChildFiles()
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
                return _files.ToArray();
        }

        #endregion

        #region FolderOperation

        public DataFolder CreateOrGetChildFolder(string name)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            if (string.IsNullOrEmpty(name))
                throw new Exception();
            
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

        public bool DeleteChildFolder(string name)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception();
            
            lock (_fileExecuteLock)
            {
                var idx = _folders.FindIndex(m => m.FolderName == name);
                if (idx < 0)
                    return false;

                var folder = _folders[idx];
                _folders.RemoveAt(idx);
                _bridge.SubFolders.Remove(folder.FolderName);
                
                folder.DeleteAllChildFiles();
                folder.DeleteAllChildFolders();

                folder.IsRemoved = true;
            }

            return true;
        }

        public void DeleteAllChildFolders()
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
            {
                _bridge.SubFolders.RemoveAll();
                foreach (var folder in _folders)
                {
                    folder.DeleteAllChildFiles();
                    folder.DeleteAllChildFolders();
                    folder.IsRemoved = true;
                }

                _folders.Clear();
            }
        }
        

        public bool ExistsChildFolder(string name)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception();
            
            lock (_fileExecuteLock)
                return _folders.FindIndex(m => m.FolderName == name) >= 0;
        }
        
        
        public DataFolder GetChildFolder(string name)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception();

            lock (_fileExecuteLock)
                return _folders.Find(m => m.FolderName == name);
        }

        public DataFolder[] GetChildFolders(Predicate<DataFolder> match)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
                return _folders.FindAll(match).ToArray();
        }


        public DataFolder[] GetChildFolders()
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_fileExecuteLock)
                return _folders.ToArray();
        }

        #endregion
        
    }
}