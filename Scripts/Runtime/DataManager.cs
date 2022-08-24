using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Ca2didi.JsonFSDataSystem.Json;
using Ca2didi.JsonFSDataSystem.Settings;
using Newtonsoft.Json;
using UnityEngine;

namespace Ca2didi.JsonFSDataSystem
{
    // Type Definitions
    [JsonTypeDefine(typeof(int), "int")]
    [JsonTypeDefine(typeof(float), "float")]
    [JsonTypeDefine(typeof(bool), "bool")]
    [JsonTypeDefine(typeof(string), "string")]
    [JsonTypeDefine(typeof(Vector2), "vec2")]
    [JsonTypeDefine(typeof(Vector3), "vec3")]
    [JsonTypeDefine(typeof(Vector4), "vec4")]
    public class DataManager
    {
        #region StaticManagement
        
        public static DataManager Instance { get; protected set; }

        public static bool IsEnabled { get; private set; }

        internal static bool Booting => Instance != null;

        internal static void StartChecker()
        {
            if (!Booting)
                throw new InvalidOperationException("You must enable DataManager first");
        }
        
        public static void SafetyChecker()
        {
            if (!IsEnabled)
                throw new InvalidOperationException("You must make sure DataManager has booted");
        }

        public static ConfiguredTaskAwaitable<DataManager> CreateNewAsync(DataManagerSetting setting = null, Action<Exception> err = null)
        {
            if (setting == null)
                setting = new DataManagerSetting();
            
            if (Instance != null)
                throw new InvalidOperationException("DataManager has already booted.");

            var ins = new DataManager(setting, err);
            return Task.Run(() => ins.BootContainer(err)).ConfigureAwait(false);
        }

        public static DataManager CreateNew(DataManagerSetting setting = null, Action<Exception> err = null)
        {
            return CreateNewAsync(setting, err).GetAwaiter().GetResult();
        }


        #endregion

        #region LifeCircle
        
        private bool Closing = false;
        
        private DataManager(DataManagerSetting setting, Action<Exception> err)
        {
            this.setting = setting;
            serializer = JsonSerializer.Create(setting.SerializerSettings);
            
            // Init converters
            customConverters = new List<JsonConverter>(setting.CustomConverters);
            
            if (setting.UsingUnitySpecificJsonConverters)
            {
                customConverters.Add(new Vector2Converter());
                customConverters.Add(new Vector3Converter());
                customConverters.Add(new Vector4Converter());
                customConverters.Add(new QuaternionConverter());
            }
            
            foreach (var cvt in customConverters)
            {
                serializer.Converters.Add(cvt);
            }
            
            // Init developmentMode
            if (DevelopmentMode)
            {
                setting.SerializerSettings.Formatting = Formatting.Indented;
            }
        }

        public void Close()
        {
            CloseAsync().GetAwaiter().GetResult();
        }
        
        public ConfiguredTaskAwaitable CloseAsync()
        {
            return Task.Run(async () =>
            {
                IsEnabled = false;
                if (Instance == null) return;

                lock (Instance)
                {
                    if (Closing) return;
                    Closing = true;
                }

                await Container.WriteStaticAsync();
                await Container.DestroyCurrentContainerAsync();

                lock (Instance)
                {
                    Instance = null;
                    Closing = false;
                }
            }).ConfigureAwait(false);
        }

        private DataManager BootContainer(Action<Exception> err = null)
        {
            // Init container
            if (Instance != null) return null;
            try
            {
                Instance = this;
                _container = new DataContainer();
                _container.ScanBinders();
                _container.ScanJsonFile();
            }
            catch (Exception e)
            {
                Instance = null;
                if (err == null) Debug.LogError(e);
                else err.Invoke(e);
                return null;
            }

            lock (Instance) IsEnabled = true;
            return this;
        }

        #endregion

        #region Settings

        private List<Action> flushDataBufferList = new List<Action>();

        internal List<Action> FlushDataBuffer
        {
            get
            {
                lock (flushDataBufferList) return flushDataBufferList;
            }
        }

        internal ConfiguredTaskAwaitable FlushAllData()
        {
            Action[] list;
            lock (flushDataBufferList)
            {
                list = flushDataBufferList.ToArray();
                flushDataBufferList.Clear();
            }

            return Task.Run(() =>
            {
                foreach (var action in list)
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }).ConfigureAwait(false);

        }
        

        private List<Func<DataManagerCallbackTiming, Task>> callbacks = new List<Func<DataManagerCallbackTiming, Task>>();

        /// <summary>
        /// Add a callback while state of data has changed.
        /// </summary>
        /// <param name="cb">callback pointer</param>
        public void AddCallback(Func<DataManagerCallbackTiming, Task> cb) => callbacks.Add(cb);

        /// <summary>
        /// Remove the callback while state of data has changed.
        /// </summary>
        /// <param name="cb">callback pointer</param>
        public bool RemoveCallback(Func<DataManagerCallbackTiming, Task> cb) => callbacks.Remove(cb);

        internal ConfiguredTaskAwaitable DoCallback(DataManagerCallbackTiming timing)
        {
            var cbs = callbacks.ToArray();
            var tsks = new List<Task>();
            foreach (var cb in cbs)
            {
                var t = cb(timing);
                if (t == null) continue;
                if (t.IsCompleted)
                {
                    if (t.IsFaulted)
                        Debug.LogError(t.Exception);
                }
                else
                {
                    tsks.Add(t);
                }
            }

            return Task.WhenAll(tsks.ToArray()).ConfigureAwait(false);
        }

        private Action<Exception> _errorHandle;
        private DataContainer _container;
        
        internal readonly DataManagerSetting setting;
        internal readonly JsonSerializer serializer;
        private readonly List<JsonConverter> customConverters;

        public DataContainer Container => _container;
        public bool DevelopmentMode => setting.UnderDevelopment;

        #endregion

    }

    [Flags]
    public enum DataManagerCallbackTiming
    {
        BeforeWrite = 1,
        AfterRead = 2,
        AfterNew = 4
    }
}