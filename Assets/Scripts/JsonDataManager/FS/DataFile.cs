using System;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace xyz.ca2didi.Unity.JsonDataManager.FS
{

    public class DataFile
    {
        #region GlobalMethods
        
        public static DataFile GetFile(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
                return DataFolder.GetFolder(path.NavBackward())?.GetChildFile(path.FileIdentify, path.FileType);
            throw new Exception();
        }

        public static DataFile CreateOrGetFile(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
                return DataFolder.CreateOrGetFolder(path.NavBackward()).
                    CreateOrGetChildFile(path.FileType, path.FileIdentify);
            throw new Exception();
        }
        
        public static bool ExistsFile(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
            {
                var f = DataFolder.GetFolder(path.NavBackward());
                return f == null ? false : f.ExistsChildFile(path.FileType, path.FileIdentify);
            }
            throw new Exception();
            
        }

        #endregion

        #region FileInformation

        public Type ObjectType => TypeBinder.ActualType;
        public string FileName => Path.FileName;
        public readonly FSPath Path;
        public readonly DataFolder Parent;
        public bool IsRemoved { get; internal set; }
        public bool IsEmpty => jEmpty.ToObject<bool>(DataManager.Instance.serializer);

        private readonly object _jsonTransitLock = new object();
        private readonly DataTypeBinder TypeBinder;
        
        #endregion

        #region Runtime

        private JValue jEmpty { get; }
        private JProperty jData { get; }
        
        private DataFile(){}
        
        // Create existed file
        internal DataFile(JProperty jProperty, DataFolder parent)
        {
            DataManager.SafetyStartChecker();

            if (parent.Path.IsFilePath)
                throw new Exception();
            
            var fName = jProperty.Name;
            IsRemoved = false;
            Parent = parent;
            Path = parent.Path.NavToward($"/{fName}");

            var rootObj = jProperty.Value as JObject;
            var typeStr = ((JValue) rootObj["type"]).ToObject<string>(DataManager.Instance.serializer);
            
            TypeBinder = DataManager.Instance.Container.GetBinder(typeStr);
            if (TypeBinder == null)
                throw new Exception();
            
            jData = (JProperty) ((JValue) rootObj["data"]).Parent;
            jEmpty = (JValue) rootObj["empty"];
            
            JsonToObj();
        }
 
        // Create new file
        internal DataFile(string identify, string typeStr, DataFolder parent, out JProperty jProperty)
        {
            DataManager.SafetyStartChecker();
            
            if (parent.Path.IsFilePath)
                throw new Exception();
            
            typeStr = typeStr.Trim();
            TypeBinder = DataManager.Instance.Container.GetBinder(typeStr);
            if (TypeBinder == null)
                throw new Exception();
            
            IsRemoved = false;
            Parent = parent;
            Path = parent.Path.NavToward($"{identify}.{typeStr}");

            /*
             * JSON structure
             * 
             * "fileName.fileType" : {
             *      "type" : "JsonElementType",
             *      "empty" : false,
             *      "data" : {}
             * }
             * 
             */
            var jRootObj = new JObject();
            jEmpty = new JValue(true);
            jData = new JProperty("data", JValue.CreateNull());
            
            jRootObj.Add(new JProperty("type", TypeBinder.JsonElement));
            jRootObj.Add(new JProperty("empty", jEmpty));
            jRootObj.Add(jData);
            jProperty = new JProperty($"{Path.FileName}", jRootObj);
        }

        protected void RemovedCheck()
        {
            if (IsRemoved)
                throw new InvalidOperationException("File was removed! You can not operate it again.");
        }

        protected static void FSCheck(FSPath path)
        {
            if (!path.IsFilePath)
                throw new Exception();
        }

        internal void Empty()
        {
            lock (_jsonTransitLock)
            {
                jData.Value = JValue.CreateNull();
                jEmpty.Value = true;
            }
        }

        internal void ObjToJSon(object obj)
        {
            lock (_jsonTransitLock)
            {
                jData.Value = JValue.FromObject(obj, DataManager.Instance.serializer);
                jEmpty.Value = false;
            }
        }

        internal object JsonToObj()
        {
            lock (_jsonTransitLock)
            {
                return jData.Value.ToObject(ObjectType, DataManager.Instance.serializer);
            }
        }
        

        #endregion

        internal readonly object sharedRWLock = new object();

        public class DataFileOperator<T>
        {
            private DataFileOperator() {}

            public bool CanReadWrite => !File.IsRemoved;
            public bool IsEmpty => File.IsEmpty;
            public readonly DataFile File;

            internal DataFileOperator(DataFile file)
            {
                File = file;
            }

            public T Read()
            {
                if (File.IsRemoved)
                    throw new InvalidOperationException("File was removed! You can not operate it again.");

                lock (File.sharedRWLock)
                {
                    if (File.IsEmpty)
                        return default;

                    return (T) File.JsonToObj();
                }
            }

            public void Write(T obj)
            {
                if (File.IsRemoved)
                    throw new InvalidOperationException("File was removed! You can not operate it again.");
                
                lock (File.sharedRWLock)
                {
                    if (obj == null)
                        File.Empty();
                    
                    File.ObjToJSon(obj);
                }
            }

            public void Clean()
            {
                if (File.IsRemoved)
                    throw new InvalidOperationException("File was removed! You can not operate it again.");

                lock (File._jsonTransitLock)
                {
                    File.Empty();
                }
                
            }
            
        }

        public bool OperateAs<T>(out DataFileOperator<T> file)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_jsonTransitLock)
            {
                
                if (ObjectType == typeof(T))
                {
                    file = new DataFileOperator<T>(this);
                    return file != null;
                }

                file = null;
                return false;
            }
        }
    }
}