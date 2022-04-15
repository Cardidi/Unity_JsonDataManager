using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using xyz.ca2didi.Unity.JsonFSDataSystem.Json;
using xyz.ca2didi.Unity.JsonFSDataSystem.Settings;

namespace xyz.ca2didi.Unity.JsonFSDataSystem
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

        public static DataManager CreateNew([NotNull] DataManagerSetting setting, Action<Exception> err = null)
        {
            if (Instance != null)
                throw new InvalidOperationException("DataManager has already booted.");

            err ??= e => Debug.LogError(e);
            var ins = new DataManager(setting, err);
            return ins;
        }
        
        public static DataManager CreateNew(Action<Exception> err = null)
        {
            if (Instance != null)
                throw new InvalidOperationException("DataManager has already booted.");

            err ??= e => Debug.LogError(e);
            var ins = new DataManager(new DataManagerSetting(), err);
            return ins;
        }
        

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

        private bool Closing = false;

        public async Task CloseContainerAsync()
        {
            IsEnabled = false;
            if (Instance == null) return;
            
            await Task.Run(async () =>
            {
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
            });
        }

        public async Task<DataManager> BootContainerAsync(Action<Exception> err = null)
        {            
            // Init container
            if (Instance != null) return Instance;
            try
            {
               Instance = this;
               _container = new DataContainer();
               await _container.ScanBinders();
               await _container.ScanJsonFile();
            }
            catch (Exception e)
            {
                Instance = null;
                if (err == null) throw e;
                err.Invoke(e);
                return null;
            }

            lock (Instance) IsEnabled = true;
            return this;
        }

        #endregion

        #region Settings

        internal Action StaticDirtyFileRegister, CurrentDirtyFileRegister;

        private List<Func<DataManagerCallbackTiming, Task>> callbacks = new List<Func<DataManagerCallbackTiming, Task>>();

        public void AddCallback(Func<DataManagerCallbackTiming, Task> cb) => callbacks.Add(cb);

        public bool RemoveCallback(Func<DataManagerCallbackTiming, Task> cb) => callbacks.Remove(cb);

        internal Task DoCallback(DataManagerCallbackTiming timing)
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

            return Task.WhenAll(tsks.ToArray());
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
        BeforeWriteCurrent = 1,
        BeforeWriteStatic = 2,
        AfterReadCurrent = 4,
        AfterReadStatic = 8
    }
}