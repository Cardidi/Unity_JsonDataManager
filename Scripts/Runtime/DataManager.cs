using System;
using System.Collections.Generic;
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
    [JsonTypeDefine(typeof(string), "str")]
    [JsonTypeDefine(typeof(Vector2), "vec2")]
    [JsonTypeDefine(typeof(Vector3), "vec3")]
    [JsonTypeDefine(typeof(Vector4), "vec4")]
    
    public class DataManager
    {
        #region StaticManagement
        
        public static DataManager Instance { get; protected set; }

        public static bool IsEnabled => Instance != null;

        internal static void SafetyStartChecker()
        {
            if (!IsEnabled)
                throw new InvalidOperationException("You must enable DataManager first to using Json Data FS.");
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
        
        public static DataManager StartNew([NotNull] DataManagerSetting setting, Action<Exception> err = null)
        {
            if (Instance != null)
                throw new InvalidOperationException("DataManager has already booted.");

            err ??= e => Debug.LogError(e);
            var ins = new DataManager(setting, err);
            ins.BootContainer(err);
            return ins;
        }

        public static DataManager StartNew(Action<Exception> err = null)
        {
            if (Instance != null)
                throw new InvalidOperationException("DataManager has already booted.");

            err ??= e => Debug.LogError(e);
            var ins = new DataManager(new DataManagerSetting(), err);
            ins.BootContainer(err);
            return ins;
        }

        private DataManager(DataManagerSetting setting, Action<Exception> err)
        {
            Instance = this;
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
        
        public void BootContainer(Action<Exception> err = null)
        {            
            // Init container
            try
            {
                _container = new DataContainer();
                _container.ScanBinders();
                _container.ScanJsonFile();
            }
            catch (Exception e)
            {
                Instance = null;
                if (err == null)
                    throw e;
                err.Invoke(e);
            }
            
        }

        #endregion

        #region Settings

        private Action<Exception> _errorHandle;
        private DataContainer _container;
        
        internal readonly DataManagerSetting setting;
        internal readonly JsonSerializer serializer;
        private readonly List<JsonConverter> customConverters;

        public DataContainer Container => _container;
        public bool DevelopmentMode => setting.UnderDevelopment;

        #endregion

    }
}