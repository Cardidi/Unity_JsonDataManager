using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        internal static void SafetyStartChecker()
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
            customConverters = new List<JsonConverter>();
            DevelopmentMode = false;
        }

        public DataManager SetErrorHandle([NotNull] Action<Exception> handle)
        {
            _errorHandle = handle;
            return this;
        }
        

        public DataManager SetDevelopment(bool dev = true)
        {
            DevelopmentMode = dev;
            return this;
        }

        public DataManager AddSpecificConverters([NotNull] params JsonConverter[] converters)
        {
            customConverters.AddRange(converters);
            return this;
        }

        public DataManager CommitAsync()
        {
            Instance = this;
            try
            {
                foreach (var cvt in customConverters)
                {
                    serializer.Converters.Add(cvt);
                }
                
                _container = new DataContainer();
                _container.ScanBinders();
                _container.ScanJsonFile();
            }
            catch (Exception e)
            {
                Instance = null;
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

            return this;
        }
        
        public void Stop()
        {
            try
            {
                
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
        private readonly List<JsonConverter> customConverters;

        public DataContainer Container => _container;
        public bool DevelopmentMode { get; private set; }

        #endregion
        
    }
}