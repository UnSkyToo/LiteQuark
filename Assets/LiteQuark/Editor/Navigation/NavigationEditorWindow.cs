using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public class NavigationEditorWindow : EditorWindow
    {
        private const string SubPath = "Navigation/PathList.json";
        private NavigationData Data_;
        private Vector2 ScrollPos_;
        
        [MenuItem("Lite/Navigation")]
        private static void ShowWin()
        {
            var win = GetWindow<NavigationEditorWindow>("Navigation");
            win.Show();
        }

        private void OnEnable()
        {
            var jsonPath = PathHelper.GetLiteQuarkRootPath(SubPath);
            Data_ = NavigationData.FromJson(jsonPath);
            ScrollPos_ = Vector2.zero;
        }

        private void OnDisable()
        {
            var jsonPath = PathHelper.GetLiteQuarkRootPath(SubPath);
            NavigationData.ToJson(Data_, jsonPath);
        }

        private void OnGUI()
        {
            ScrollPos_ = EditorGUILayout.BeginScrollView(ScrollPos_);
            
            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button("PersistentData Path"))
                {
                    LiteEditorHelper.OpenFolder(Application.persistentDataPath);
                }

                if (GUILayout.Button("TemporaryCache Path"))
                {
                    LiteEditorHelper.OpenFolder(Application.temporaryCachePath);
                }

                foreach (var path in Data_.GetPathList())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(path))
                        {
                            LiteEditorHelper.Ping(path);
                            
                            if (Event.current.alt)
                            {
                                LiteEditorHelper.OpenAsset(path);
                                Event.current.Use();
                            }
                        }

                        if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
                        {
                            Data_.RemovePath(path);
                            break;
                        }
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Click the path to select it, and press and hold alt to open it directly");

            if (mouseOverWindow == this)
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                }
                else if (Event.current.type == EventType.DragExited)
                {
                    Data_.AddPath(DragAndDrop.paths);
                    
                    Focus();
                    Event.current.Use();
                }
            }
        }
    }
}