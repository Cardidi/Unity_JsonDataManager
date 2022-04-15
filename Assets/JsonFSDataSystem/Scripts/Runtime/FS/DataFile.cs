using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace xyz.ca2didi.Unity.JsonFSDataSystem.FS
{
    public class DataFile
    {
        #region GlobalMethods
        
        public static DataFile Get(FSPath path)
        {
            DataManager.StartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
                return DataFolder.Get(path.Backward())?.GetFile(path.FileIdentify, path.FileType);
            throw new Exception();
        }

        public static DataFile CreateOrGet(FSPath path)
        {
            DataManager.StartChecker();
            FSCheck(path);
            
            if (path.IsFilePath)
                return DataFolder.CreateOrGet(path.Backward()).
                    CreateOrGetFile(path.FileType, path.FileIdentify);
            throw new Exception();
        }
        
        public static bool Exists(FSPath path)
        {
            DataManager.StartChecker();
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
        public readonly bool IsStatic;
        public bool IsDirty { get; internal set; }
        public bool IsRemoved { get; internal set; }
        public bool IsEmpty => jEmpty.ToObject<bool>(DataManager.Instance.serializer);

        private readonly object _jsonTransitLock = new object();
        private readonly DataTypeBinder TypeBinder;
        
        #endregion

        #region Runtime

        private object cached;
        private JValue jEmpty { get; }
        internal JProperty jData { get; }
        
        private DataFile(){}
        
        // Create existed file
        internal DataFile(JProperty jProperty, DataFolder parent)
        {
            DataManager.StartChecker();

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

            IsStatic = FSPath.IsStaticPath(Path);
        }
 
        // Create new file
        internal DataFile(string identify, string typeStr, DataFolder parent, out JProperty jProperty)
        {
            DataManager.StartChecker();
            
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
            
            IsStatic = FSPath.IsStaticPath(Path);
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

        internal void Empty()
        {
            lock (_jsonTransitLock)
            {
                jData.Value = JValue.CreateNull();
                jEmpty.Value = true;
                NotDirty();
            }
        }

        internal void ObjToJson(object obj)
        {
            lock (_jsonTransitLock)
            {
                jData.Value = JValue.FromObject(obj, DataManager.Instance.serializer);
                jEmpty.Value = false;
                NotDirty();
            }
        }

        internal object JsonToObj()
        {
            lock (_jsonTransitLock)
            {
                return jData.Value.ToObject(ObjectType, DataManager.Instance.serializer);
            }
        }

        private void DirtyEvent()
        {
            ObjToJson(cached);
        }

        internal void Dropped()
        {
            IsRemoved = true;
            cached = null;
            NotDirty();
        }
        
        #endregion

        private readonly object sharedRWLock = new object();

        public class Operator<T>
        {
            private Operator() {}

            public bool IsRemoved => !file.IsRemoved;

            public bool IsEmpty => File.IsEmpty;
            public bool IsDirty => File.IsDirty;
            private readonly DataFile file;
            public DataFile File
            {
                get
                {
                    file.RemovedCheck();
                    return file;
                }
            }

            internal Operator(DataFile file)
            {
                this.file = file;
            }

            /// <summary>
            /// Read data in this file.<br/>
            /// It advised that check empty before you call this function.
            /// </summary>
            /// <returns>If you trying to get a empty data, it will return null or default(T).</returns>
            /// <exception cref="InvalidOperationException">If you trying to write into a removed file, it will called.</exception>
            public T Read()
            {
                file.RemovedCheck();

                lock (file.sharedRWLock)
                {
                    if (file.IsEmpty)
                        return default;
                    
                    if (file.cached == null) return (T) file.JsonToObj();
                    return (T) file.cached;
                }
            }

            /// <summary>
            /// Write data to file.
            /// </summary>
            /// <exception cref="InvalidOperationException">If you trying to write into a removed file, it will called.</exception>
            public void Write(T obj)
            {
                file.RemovedCheck();
                
                lock (file.sharedRWLock)
                {
                    if (obj == null)
                        file.Empty();
                    else
                    {
                        file.cached = obj;
                        file.Dirty();
                    }
                        
                }
            }
            
            /// <summary>
            /// Make this file empty.
            /// </summary>
            /// <exception cref="InvalidOperationException">If you trying to write into a removed file, it will called.</exception>
            public void Empty()
            {
                file.RemovedCheck();

                lock (file._jsonTransitLock)
                {
                    if (!file.IsEmpty)
                    {
                        file.Empty();
                    }
                }
                
            }
            
            /// <summary>
            /// Mark this data to dirty that it will save this data.<br/>
            /// This function will be called if you have write anything into Data File except null.
            /// </summary>
            public void Dirty()
            {
                lock (file.sharedRWLock) file.Dirty();
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
            DataManager.StartChecker();
            RemovedCheck();

            lock (_jsonTransitLock)
            {

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

        /// <summary>
        /// Mark this data to dirty that it will save this data.<br/>
        /// This function will be called if you have write anything into Data File.
        /// </summary>
        public void Dirty()
        {
            RemovedCheck();
            IsDirty = true;
            if (IsStatic)
                DataManager.Instance.StaticDirtyFileRegister += DirtyEvent;
            else
                DataManager.Instance.CurrentDirtyFileRegister += DirtyEvent;
        }

        private void NotDirty()
        {
            IsDirty = false;
            if (IsStatic)
                DataManager.Instance.StaticDirtyFileRegister -= DirtyEvent;
            else
                DataManager.Instance.CurrentDirtyFileRegister -= DirtyEvent;
        }
    }
}