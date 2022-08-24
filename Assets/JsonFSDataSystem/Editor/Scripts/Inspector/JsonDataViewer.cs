using System.IO;
using Ca2didi.JsonFSDataSystem.FS;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Ca2didi.JsonFSDataSystem.Editor.Inspector
{
    public class JsonDataViewer : EditorWindow
    {
        private static float minWidth = 700, minHeight = 500;
        private static JsonDataViewer activeWindow;

        [MenuItem("Tools/Json Data Viewer")]
        private static void ShowWindow()
        {
            if (activeWindow == null)
            {
                activeWindow = GetWindow<JsonDataViewer>();
                activeWindow.titleContent = new GUIContent
                {
                    text = "Json Data Viewer"
                };
                activeWindow.minSize = new Vector2(minWidth, minHeight);
            }
            activeWindow.Show();
        }

        private void OnDestroy()
        {
            if (activeWindow == this) activeWindow = null;
        }

        private string blankDataText = "~";
        
        private void OnGUI()
        {
            if (!Application.isPlaying && DataManager.IsEnabled)
            {
                Debug.LogWarning(
                    "DataManager was closed by Json Data Viewer. Please check if your code have not handle with DataManager closing call.");
                DataManager.Instance.CloseAsync();
            }
            
            DrawToolbar();
            GUILayout.Space(2f);
            if (DataManager.IsEnabled)
            {
                if (currentFolder == null && DataManager.Instance.Container.StaticContainer != null)
                    currentFolder = DataManager.Instance.Container.StaticContainer.Root;
            }
            else
            {
                currentFile = null;
                currentFolder = null;
                EditorGUILayout.HelpBox("You should enter \"Play Mode\" with DataManager.StartNew() called to see data.\nThere is nothing to show for you.", MessageType.Info);
            }
            DrawBasicLayout();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUI.enabled = DataManager.IsEnabled;
            if (GUILayout.Button("Refresh", GUILayout.ExpandWidth(false)))
            {
                DataManager.Instance.FlushAllData();
            }
            if (GUILayout.Button("Open Static", GUILayout.ExpandWidth(false)))
                currentFolder = DataFolder.Get(FSPath.StaticPathRoot);
            GUI.enabled = DataManager.Instance != null && DataManager.Instance.Container.HasActiveCurrentContainer;
            if (GUILayout.Button("Open Current", GUILayout.ExpandWidth(false)))
                currentFolder = DataFolder.Get(FSPath.CurrentPathRoot);
            GUI.enabled = !DataManager.IsEnabled;
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            var status = "Status: Disabled";
            if (DataManager.Booting)
                status = "Status: Booting...";
            if (DataManager.IsEnabled)
                status = "Status: Running";
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(new GUIContent(status, "Current running status of Data Manager"));
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button(
                    new GUIContent("Clean Folder", "Clean Save Data Folder.\n" + 
                         "It only valid if you have not boot DataManger.\n" + 
                         "It will only clean default data path.If you assign other path, it may not working for you"), 
                    GUILayout.ExpandWidth(false)))
            {
                if (Directory.Exists(Application.persistentDataPath + "/Save"))
                    Directory.Delete(Application.persistentDataPath + "/Save", true);
                Directory.CreateDirectory(Application.persistentDataPath + "/Save");
            }
            GUI.enabled = true;
            if (GUILayout.Button(
                    new GUIContent("Open Folder",
                        "Open Save Data Folder.\n" +
                        "It will open default data path if you not boot DataManager.\n" +
                        "If it booted, it will open data path from runtime."),
                    GUILayout.ExpandWidth(false)))
            {
                if (DataManager.Booting)
                {
                    if (!Directory.Exists(DataManager.Instance.setting.GameDataRelativeDirectoryPath))
                        Directory.CreateDirectory(DataManager.Instance.setting.GameDataRelativeDirectoryPath);
                    EditorUtility.RevealInFinder(DataManager.Instance.setting.GameDataRelativeDirectoryPath);
                }
                else
                {
                    if (!Directory.Exists(Application.persistentDataPath + "/Save"))
                        Directory.CreateDirectory(Application.persistentDataPath + "/Save");
                    EditorUtility.RevealInFinder(Application.persistentDataPath + "/Save");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private Vector2 dataContentScrollPos = Vector2.zero;
        private Vector2 dataStructScrollPos = Vector2.zero;
        private DataFile currentFile;
        private DataFolder currentFolder;
        
        private void DrawBasicLayout()
        {
            var size = position.size;
            
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(size.x));
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(size.x * 0.6f), GUILayout.ExpandHeight(true));
                {
                    EditorGUILayout.LabelField("Data Disk Structure", EditorStyles.boldLabel);
                    currentFolder = DrawDiskStruct(currentFolder, currentFile, out var f);
                    if (f != null) currentFile = f;
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                {
                    EditorGUILayout.LabelField("Data Info", EditorStyles.boldLabel);
                    DrawDataInfo(currentFile);
                    EditorGUILayout.LabelField("Data Content (JSON)", EditorStyles.boldLabel);
                    dataContentScrollPos = EditorGUILayout.BeginScrollView(dataContentScrollPos);
                    {
                        DrawDataContent(currentFile);
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }  
        }

        private DataFolder DrawDiskStruct(DataFolder folder, DataFile enable,out DataFile file)
        {
            file = null;
            DataFolder refFolder = null;
            GUI.enabled = false;
            GUILayout.TextArea(folder == null ? "" : folder.Path.FullPath);
            GUI.enabled = true;
            GUILayout.Space(3f);
            
            dataStructScrollPos = EditorGUILayout.BeginScrollView(dataStructScrollPos, EditorStyles.helpBox);
            if (folder != null)
            {
                refFolder = folder;
                if (folder.Parent != folder)
                {           
                    if (GUILayout.Button("../"))
                    {
                        refFolder = folder.Parent;
                    }
                }

                foreach (var f in folder.GetFolders())
                {
                    if (GUILayout.Button(f.FolderName))
                        refFolder = f;
                }
                
                GUILayout.Space(5f);

                foreach (var f in folder.GetFiles())
                {
                    GUI.enabled = f != enable;
                    if (GUILayout.Button(f.FileName))
                        file = f;
                    GUI.enabled = true;
                }

            }
            
            EditorGUILayout.EndScrollView();
            GUILayout.Space(3f);
            return refFolder;
        }
        
        private void DrawDataInfo(DataFile file)
        {
            var textOverflow = EditorStyles.label;
            textOverflow.wordWrap = true;

            var path = file == null ? blankDataText : file.Path.FullPath;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Name", EditorStyles.largeLabel);
            EditorGUILayout.LabelField(file == null ? blankDataText: file.FileName, textOverflow);
            EditorGUILayout.LabelField("Type", EditorStyles.largeLabel);
            EditorGUILayout.LabelField(new GUIContent(
                file == null ? blankDataText: file.ObjectType.Name,
                file == null ? blankDataText: file.ObjectType.FullName), textOverflow);
            EditorGUILayout.LabelField("Path", EditorStyles.largeLabel);
            EditorGUILayout.LabelField(path, textOverflow);
            EditorGUILayout.EndVertical();
            GUI.enabled = file != null;
            if (GUILayout.Button("Copy Path")) GUIUtility.systemCopyBuffer = path;
            GUI.enabled = true;
        }

        private void DrawDataContent(DataFile file)
        {
            if (file == null)
            {
                EditorGUILayout.HelpBox("You have to select a data to show content for you!", MessageType.Info);
                return;
            }
            
            if (file.IsRemoved)
                EditorGUILayout.HelpBox("Data has already removed! It may cause by delete or container unload.", MessageType.Error);
                
            if (file.IsDirty)
                EditorGUILayout.HelpBox("Data has marked to dirty. It may out fo date.\nUse Refresh to see current data.", MessageType.Warning);
            
            if (file.IsEmpty)
            {
                EditorGUILayout.HelpBox("This is an empty data!", MessageType.Info);
            }
            else
            {
                var textOverflow = EditorStyles.label;
                textOverflow.wordWrap = true;
                EditorGUILayout.LabelField(file.jData.Value.ToString(Formatting.Indented), textOverflow);
                
                // if (_jsonWriter?.Token == file.jData)
                //     _jsonWriter?.Draw();
                // else
                // {
                //     _jsonWriter = new JsonGUIWriter(file.jData);
                //     _jsonWriter?.Draw();
                // }
            }
        }
        //
        // private JsonGUIWriter? _jsonWriter;
        //
        // private struct JsonGUIWriter
        // {
        //     public readonly JToken Token;
        //     
        //     public JsonGUIWriter(JToken token)
        //     {
        //         Token = token;
        //     }
        //
        //     public void Draw()
        //     {
        //         
        //     }
        //
        //     private void DrawInternal(JToken token)
        //     {
        //         try
        //         {
        //             if (token.Type == JTokenType.Property)
        //             {
        //                 
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //             return;
        //         }
        //     }
        // }
        
    }
}