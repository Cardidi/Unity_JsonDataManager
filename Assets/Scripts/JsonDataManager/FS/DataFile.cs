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
                return DataFolder.GetFolder(path)?.GetChildFile(path.FileIdentify, path.FileType);
            throw new Exception();
        }

        public static DataFile GetOrCreateFile(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
                return DataFolder.CreateOrGetFolder(path).CreateOrGetChildFile(path.FileType, path.FileName);
            throw new Exception();
        }
        
        public static bool ExistsFile(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
            {
                var f = DataFolder.GetFolder(path);
                return f == null ? false : f.ExistsChildFile(path.FileType, path.FileName);
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

        protected readonly object _jsonTransitLock = new object();
        internal readonly DataTypeBinder TypeBinder;
        internal bool IsDirty { get; set; }

        #endregion

        #region Runtime

        protected BaseData _origin;
        protected JProperty jEmpty { get; }
        protected JProperty jData { get; }
        
        private protected DataFile(){}

        // Create existed file
        internal DataFile(JProperty jProperty, DataFolder parent)
        {
            DataManager.SafetyStartChecker();

            if (parent.Path.IsFilePath)
                throw new Exception();
            
            var fName = jProperty.Name;
            IsDirty = false;
            IsRemoved = false;
            Parent = parent;
            Path = parent.Path.NavToward($"/{fName}");

            var rootObj = jProperty.Value as JObject;
            var typeStr = (rootObj["type"] as JProperty).Value<string>();
            
            TypeBinder = DataManager.Instance.Container.GetBinder(typeStr);
            if (TypeBinder == null)
                throw new Exception();
            
            jData = rootObj["data"] as JProperty;
            jEmpty = rootObj["empty"] as JProperty;
            
            if (!jEmpty.Value<bool>())
                _origin = ToData();
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
            
            IsDirty = false;
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
            jEmpty = new JProperty("empty", true);
            jData = new JProperty("data");
            jRootObj.Add(new JProperty("type", TypeBinder.JsonElement));
            jRootObj.Add(jEmpty);
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

        protected JObject ToJObj()
        {
            lock (_jsonTransitLock)
            {
                if (_origin == null)
                {
                    jEmpty.Value = true;
                    jData.Remove();
                    return null;
                }

                var value = JObject.FromObject(_origin, DataManager.Instance.serializer);
                jData.Value = value;
                jEmpty.Value = false;
                return value;
            }
        }

        protected BaseData ToData()
        {
            lock (_jsonTransitLock)
            {
                if (jData.Count == 0)
                {
                    jEmpty.Value = true;
                    return null;
                }

                var obj =
                    DataManager.Instance.serializer.Deserialize(new JTokenReader(jData.Value), ObjectType) as BaseData;
                _origin = obj;
                jEmpty.Value = _origin == null;
                return _origin;
            }
        }
        

        #endregion

        public bool OperateAs<T>(out DataFile<T> file) where T : BaseData
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();

            lock (_jsonTransitLock)
            {
                if (_origin is T)
                {
                    file = this as DataFile<T>;
                    return file != null;
                }

                file = null;
                return false;
            }
        }
    }

    public class DataFile<T> : DataFile where T : BaseData
    {
        private DataFile(){}

        private T _obj
        {
            get => _origin as T;
            set => _origin = value;
        }

        public T Read()
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();
            
            lock (_jsonTransitLock)
                return _obj;
        }

        public void Write(T obj)
        {
            DataManager.SafetyStartChecker();
            RemovedCheck();
            
            if (obj.Invalid())
                throw new ArgumentException("New data is not valid!");
            
            IsDirty = true;
            lock (_jsonTransitLock)
                _obj = obj;
            ToJObj();
        }
    }
}