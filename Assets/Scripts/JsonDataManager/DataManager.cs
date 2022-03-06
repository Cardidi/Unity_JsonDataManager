using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using xyz.ca2didi.Unity.JsonDataManager.FS;
using xyz.ca2didi.Unity.JsonDataManager.Settings;

namespace xyz.ca2didi.Unity.JsonDataManager
{
    public class DataManager
    {
        #region StaticManagement
        
        public static DataManager Instance { get; protected set; }

        public static bool IsEnabled => Instance != null;

        public static void SafetyStartChecker()
        {
            if (!IsEnabled)
                throw new InvalidOperationException("You must enable DataManager first to using Json Data FS.");
        }

        
        public static DataManager StartNew([NotNull] DataManagerSetting setting)
        {
            if (Instance != null)
                throw new InvalidOperationException("DataManager has already booted.");

            var ins = new DataManager(setting);
            return ins;
        }

        public static DataManager StartNew()
        {
            if (Instance != null)
                throw new InvalidOperationException("DataManager has already booted.");

            var ins = new DataManager(new DataManagerSetting());
            return ins;
        }

        private DataManager(DataManagerSetting setting)
        {
            // Creation method here
            this.setting = setting;
            serializer = JsonSerializer.Create(setting.SerializerSettings);
            DevelopmentMode = false;
        }

        private DataManager SetErrorHandle([NotNull] Action<Exception> handle)
        {
            _errorHandle = handle;
            return this;
        }
        

        private DataManager SetDevelopment(bool dev = true)
        {
            DevelopmentMode = dev;
            return this;
        }

        private DataManager AddSpecificConverters([NotNull] params JsonConverter[] converters)
        {
            customConverters.AddRange(converters);
            return this;
        }

        private DataManager Commit()
        {
            try
            {
                foreach (var cvt in customConverters)
                {
                    serializer.Converters.Add(cvt);
                }

                OnEnable();
            }
            catch (Exception e)
            {
                if (_errorHandle != null)
                {
                    _errorHandle(e);
                }
                else
                {
                    Debug.LogError(e);
                    return null;
                }
            }

            Instance = this;
            return this;
        }
        
        public void Stop()
        {
            try
            {
                OnDisable();
            }
            catch (Exception e)
            {
                if (Instance._errorHandle != null)
                {
                    Instance._errorHandle(e);
                }
                else
                {
                    Debug.LogError(e);
                }
            }
            
            Instance = null;
        }
        
        #endregion

        #region Settings

        private Action<Exception> _errorHandle;
        private DataContainer _container;
        
        internal readonly DataManagerSetting setting;
        internal readonly JsonSerializer serializer;
        internal List<JsonConverter> customConverters;

        public DataContainer Container => _container;
        public bool DevelopmentMode { get; private set; }

        #endregion

        protected async void OnEnable()
        {
            _container = new DataContainer();
            await _container.ScanBinders();
            await _container.ScanJsonFile();
        }
        
        protected void OnDisable()
        {}
    }
}