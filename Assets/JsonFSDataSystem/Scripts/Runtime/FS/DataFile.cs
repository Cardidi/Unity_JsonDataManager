using System;
using Newtonsoft.Json.Linq;

namespace xyz.ca2didi.Unity.JsonFSDataSystem.FS
{

    public class DataFile
    {
        #region GlobalMethods
        
        public static DataFile Get(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
                return DataFolder.Get(path.Backward())?.GetFile(path.FileIdentify, path.FileType);
            throw new Exception();
        }

        public static DataFile CreateOrGet(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
                return DataFolder.CreateOrGet(path.Backward()).
                    CreateOrGetFile(path.FileType, path.FileIdentify);
            throw new Exception();
        }
        
        public static bool Exists(FSPath path)
        {
            DataManager.SafetyStartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
            {
                var f = DataFolder.Get(path.Backward());
                return f?.ExistsFile(path.FileType, path.FileIdentify) ?? false;
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
            Path = parent.Path.Forward($"/{fName}");

            var rootObj = jProperty.Value as JObject;
            var typeStr = ((JValue) rootObj["type"]).ToObject<string>(DataManager.Instance.serializer);
            
            TypeBinder = DataManager.Instance.Container.GetBinder(typeStr);
            if (TypeBinder == null)
                throw new Exception();
            
            jData = (JProperty) rootObj["data"].Parent;
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
            Path = parent.Path.Forward($"{identify}.{typeStr}");

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

        private void RemovedCheck()
        {
            if (IsRemoved)
                throw new InvalidOperationException("File was removed! You can not operate it again.");
        }

        private static void FSCheck(FSPath path)
        {
            if (!path.IsFilePath)
                throw new Exception();
        }

        private void Empty()
        {
            lock (_jsonTransitLock)
            {
                jData.Value = JValue.CreateNull();
                jEmpty.Value = true;
            }
        }

        private void ObjToJson(object obj)
        {
            lock (_jsonTransitLock)
            {
                jData.Value = JValue.FromObject(obj, DataManager.Instance.serializer);
                jEmpty.Value = false;
            }
        }

        private object JsonToObj()
        {
            lock (_jsonTransitLock)
            {
                return jData.Value.ToObject(ObjectType, DataManager.Instance.serializer);
            }
        }
        

        #endregion

        private readonly object sharedRWLock = new object();

        public class Operator<T>
        {
            private Operator() {}

            public bool CanReadWrite => !File.IsRemoved;
            public bool IsEmpty => File.IsEmpty;
            public readonly DataFile File;

            internal Operator(DataFile file)
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
                    
                    File.ObjToJson(obj);
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

        /// <summary>
        /// Use this data as a kind of type.
        /// </summary>
        /// <param name="opt">The data operator this call return.</param>
        /// <param name="defaultValue">Default value give to this data if data is empty.</param>
        /// <typeparam name="T">The type trying to convert to.</typeparam>
        /// <returns>If data is empty, return false. This value will not be effect by defaultValue.</returns>
        /// <exception cref="InvalidCastException">Type gived in T is not correct.</exception>
        public bool As<T>(out Operator<T> opt, T defaultValue = default)
        {
            DataManager.SafetyStartChecker();

            lock (_jsonTransitLock)
            {
                RemovedCheck();

                if (ObjectType != typeof(T)) throw new InvalidCastException();

                var ept = !IsEmpty;
                if (IsEmpty && defaultValue != null)
                {
                    jData.Value = JValue.FromObject(defaultValue, DataManager.Instance.serializer);
                    jEmpty.Value = false;
                }
                
                opt = new Operator<T>(this);
                return ept;

            }
        }
    }
}