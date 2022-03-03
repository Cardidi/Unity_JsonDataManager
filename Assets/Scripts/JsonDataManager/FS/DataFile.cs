// using JetBrains.Annotations;
// using xyz.ca2didi.Unity.JsonDataManager.Interface;
//
// namespace xyz.ca2didi.Unity.JsonDataManager.FS
// {
//     public class DataFile
//     {
//         
//         public static DataFile GetFile(string path)
//         {}
//
//         public static DataFile CreateFile(string path)
//         {}
//         
//         public static DataFile GetOrCreateFile(string path)
//         {}
//         
//         public static bool ExistsFile(string path)
//         {}
//
//         public string FullName => $"{FileName}.{FileType}";
//         public string FileName { get; }
//         public string FileType => TypeBinder.ReferName;
//         public string Path => $"{Parent.Path}/{FullName}";
//         public DataFolder Parent { get; }
//
//         internal DataTypeBinder TypeBinder { get; }
//         protected BaseData _origin;
//         
//         private protected DataFile(){}
//
//         private DataFile(string name, DataFolder parent ,DataTypeBinder binder)
//         {
//             FileName = name;
//             TypeBinder = binder;
//             Parent = parent;
//         }
//
//         public bool OperateAs<T>(out DataFile<T> file) where T : BaseData
//         {
//             if (_origin is T)
//             {
//                 file = this as DataFile<T>;
//                 return file != null;
//             }
//
//             file = null;
//             return false;
//         }
//     }
//
//     public class DataFile<T> : DataFile where T : BaseData
//     {
//         private DataFile(){}
//
//         private T _obj
//         {
//             get => (T) _origin;
//             set => _origin = value;
//         }
//         
//         public T Read()
//         {}
//
//         public T Write([NotNull] T obj)
//         {}
//     }
// }