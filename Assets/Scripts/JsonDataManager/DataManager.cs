using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using xyz.ca2didi.Unity.JsonDataManager.FS;
using xyz.ca2didi.Unity.JsonDataManager.Struct;

namespace xyz.ca2didi.Unity.JsonDataManager
{
    public class DataManager
    {
        #region StaticManagement
        
        public static DataManager Instance { get; protected set; }

        public static bool IsEnabled => Instance != null;

        public static void SafetyStartChecker()
        {
            if (!DataManager.IsEnabled)
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

        private DataManager SetSpecificConverters([NotNull] params JsonConverter[] converters)
        {
            customConverters = converters;
            return this;
        }

        private DataManager Commit()
        {
            try
            {
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
        
        public static void Stop()
        {
            try
            {
                Instance.OnDisable();
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
        internal JsonConverter[] customConverters;

        public DataContainer Container => _container;
        public bool DevelopmentMode { get; private set; }
        public string RootDirectoryPath => setting.GameRootDirectoryPath;
        public string DataDirectoryPath => $"{setting.GameRootDirectoryPath}{setting.GameDataRelativeDirectoryPath}";
        public int MaxDataCount => setting.MaxGameDataCount;

        #endregion

        protected void OnEnable()
        {}
        
        protected void OnDisable()
        {}
    }
}