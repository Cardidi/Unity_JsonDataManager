// using System;
// using JetBrains.Annotations;
// using Newtonsoft.Json;
// using UnityEngine;
// using xyz.ca2didi.Unity.JsonDataManager.Interface;
// using xyz.ca2didi.Unity.JsonDataManager.Struct;
//
// namespace xyz.ca2didi.Unity.JsonDataManager
// {
//     public class DataManager
//     {
//
//         public static DataManager Instance { get; protected set; }
//
//         public static bool IsEnabled => Instance != null;
//
//         public static DataManager StartNew([NotNull] DataManagerSetting setting)
//         {
//             if (Instance != null)
//                 throw new InvalidOperationException("DataManager has already booted.");
//
//             var ins = new DataManager(setting);
//             return ins;
//         }
//
//         public static DataManager StartNew()
//         {
//             if (Instance != null)
//                 throw new InvalidOperationException("DataManager has already booted.");
//
//             var ins = new DataManager(new DataManagerSetting());
//             return ins;
//         }
//
//         private DataManager(DataManagerSetting setting)
//         {
//             // Creation method here
//             _setting = setting;
//             _serializer = JsonSerializer.Create(setting.SerializerSettings);
//             DevelopmentMode = false;
//         }
//
//         private DataManager SetErrorHandle([NotNull] Action<Exception> handle)
//         {
//             _errorHandle = handle;
//             return this;
//         }
//         
//         //private DataManager<TDataContainer, TStaticDataContainer> SetDefaultDataFactory(Action<DataBinder<TDataContainer>>)
//
//         private DataManager SetDevelopment(bool dev = true)
//         {
//             DevelopmentMode = dev;
//             return this;
//         }
//
//         private DataManager SetSpecificConverters([NotNull] params JsonConverter[] converters)
//         {
//             _customConverters = converters;
//             return this;
//         }
//
//         private DataManager Commit()
//         {
//             try
//             {
//                 OnEnable();
//             }
//             catch (Exception e)
//             {
//                 if (_errorHandle != null)
//                 {
//                     _errorHandle(e);
//                 }
//                 else
//                 {
//                     Debug.LogError(e);
//                     return null;
//                 }
//             }
//
//             Instance = this;
//             return this;
//         }
//         
//         public static void Stop()
//         {
//             try
//             {
//                 Instance.OnDisable();
//             }
//             catch (Exception e)
//             {
//                 if (Instance._errorHandle != null)
//                 {
//                     Instance._errorHandle(e);
//                 }
//                 else
//                 {
//                     Debug.LogError(e);
//                 }
//             }
//             
//             Instance = null;
//         }
//         
//         #endregion
//
//         #region Settings
//
//         private readonly DataManagerSetting _setting;
//         private readonly JsonSerializer _serializer;
//         private Action<Exception> _errorHandle;
//         private JsonConverter[] _customConverters;
//
//         public bool DevelopmentMode { get; private set; }
//         public string RootDirectoryPath => _setting.GameRootDirectoryPath;
//         public string DataDirectoryPath => $"{_setting.GameRootDirectoryPath}{_setting.GameDataRelativeDirectoryPath}";
//         public int MaxDataCount => _setting.MaxGameDataCount;
//
//         #endregion
//
//         protected void OnEnable()
//         {}
//         
//         protected void OnDisable()
//         {}
//     }
// }