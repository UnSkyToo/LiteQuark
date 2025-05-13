using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public class NavigationEditorWindow : EditorWindow
    {
        private const string SubPath = "Navigation/PathList.json";
        private NavigationData _data;
        private Vector2 _scrollPos;
        
        [MenuItem("Lite/Common/Navigation")]
        private static void ShowWin()
        {
            var win = GetWindow<NavigationEditorWindow>("Navigation");
            win.Show();
        }

        [MenuItem("Lite/Common/Clear PersistentDataPath")]
        private static void ClearPersistentDataPath()
        {
            try
            {
                System.IO.Directory.Delete(Application.persistentDataPath, true);
                System.IO.Directory.CreateDirectory(Application.persistentDataPath);
                Debug.Log("Clear PersistentDataPath success");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Clear PersistentDataPath error: {ex}");
            }
        }

        private void OnEnable()
        {
            var jsonPath = PathUtils.GetLiteQuarkRootPath(SubPath);
            _data = NavigationData.FromJson(jsonPath);
            _scrollPos = Vector2.zero;
        }

        private void OnDisable()
        {
            var jsonPath = PathUtils.GetLiteQuarkRootPath(SubPath);
            NavigationData.ToJson(_data, jsonPath);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button("PersistentData Path"))
                {
                    LiteEditorUtils.OpenFolder(Application.persistentDataPath);
                }

                if (GUILayout.Button("TemporaryCache Path"))
                {
                    LiteEditorUtils.OpenFolder(Application.temporaryCachePath);
                }

                foreach (var path in _data.GetPathList())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(path))
                        {
                            LiteEditorUtils.Ping(path);
                            
                            if (Event.current.alt)
                            {
                                LiteEditorUtils.OpenAsset(path);
                                Event.current.Use();
                            }
                        }

                        if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
                        {
                            _data.RemovePath(path);
                            break;
                        }
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Click the path to select it, and press and hold alt to open it directly. Drag path to add it.");

            if (mouseOverWindow == this)
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                }
                else if (Event.current.type == EventType.DragExited)
                {
                    _data.AddPath(DragAndDrop.paths);
                    
                    Focus();
                    Event.current.Use();
                }
            }
        }
    }
}