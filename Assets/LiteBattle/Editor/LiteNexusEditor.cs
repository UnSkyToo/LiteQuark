using System.Collections.Generic;
using System.IO;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    public sealed class LiteNexusEditor : EditorWindow
    {
        private bool IsInit_ = false;
        private readonly List<ILiteEditorView> ViewList_ = new List<ILiteEditorView>();
        private LiteNexusSceneView SceneView_;
        
        [MenuItem("Lite/Nexus Engine/Editor Window")]
        private static void ShowEditor()
        {
            var win = GetWindow<LiteNexusEditor>();
            win.titleContent = new GUIContent("Nexus Engine");
            win.Show();
        }

        private void OnEnable()
        {
            IsInit_ = false;
            
            LayoutUtils.OpenScene("LiteNexusEditorScene");
            Startup();

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;

            Shutdown();
        }

        private void OnPlayModeStateChange(PlayModeStateChange state)
        {
            foreach (var view in ViewList_)
            {
                view.OnPlayModeStateChange(state);
            }
        }

        private void OnGUI()
        {
            if (!IsInit_)
            {
                LiteEditorStyle.Generate();
                IsInit_ = true;
            }

            foreach (var view in ViewList_)
            {
                if (view.IsVisible())
                {
                    view.Draw();
                    EditorGUILayout.Space();
                }
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!LiteEditorBinder.Instance.IsBindUnit())
            {
                return;
            }
            
            SceneView_?.Draw(sceneView);
        }

        private void Startup()
        {
            LiteEditorBinder.Instance.Startup();
            LiteEditorPreviewer.Instance.Startup();
            
            ViewList_.Clear();
            ViewList_.Add(new LiteRuntimeView(this));
            ViewList_.Add(new LiteUnitView(this));
            ViewList_.Add(new LiteTimelineView(this));
            ViewList_.Add(new LiteTimelineCtrlView(this));

            SceneView_ = new LiteNexusSceneView(this);
        }

        private void Shutdown()
        {
            SceneView_?.Dispose();

            foreach (var view in ViewList_)
            {
                view.Dispose();
            }
            ViewList_.Clear();

            // LiteEditorLayoutDrawer.Clear();
            LiteEditorPreviewer.Instance.Shutdown();
            LiteEditorBinder.Instance.Shutdown();
            
            SaveDatabase();
        }

        public T GetView<T>() where T : ILiteEditorView
        {
            foreach (var view in ViewList_)
            {
                if (view is T inst)
                {
                    return inst;
                }
            }

            return default;
        }

        public LiteRuntimeView GetRuntimeView() => GetView<LiteRuntimeView>();
        public LiteUnitView GetUnitView() => GetView<LiteUnitView>();
        public LiteTimelineView GetTimelineView() => GetView<LiteTimelineView>();
        public LiteTimelineCtrlView GetTimelineCtrlView() => GetView<LiteTimelineCtrlView>();

        public bool IsRuntimeMode()
        {
            return EditorApplication.isPlaying;
        }

        public bool IsEditorMode()
        {
            return !EditorApplication.isPlaying;
        }

        private void SaveDatabase()
        {
            var unitList = new List<string>();
            var stateMap = new Dictionary<string, List<string>>();
            
            var unitConfigFullPathList = AssetUtils.GetAssetPathList("LiteUnitConfig", LiteNexusConfig.Instance.GetUnitDatabasePath());
            foreach (var unitConfigFullPath in unitConfigFullPathList)
            {
                var unitConfig = AssetDatabase.LoadAssetAtPath<LiteUnitConfig>(unitConfigFullPath);
                if (unitConfig == null)
                {
                    Debug.LogError($"{unitConfigFullPath} is not LiteUnitConfig");
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(unitConfig.StateGroup))
                {
                    Debug.LogError($"{unitConfigFullPath} StateGroup is empty");
                    continue;
                }

                var timelineRootPath = PathUtils.ConcatPath(LiteNexusConfig.Instance.GetTimelineDatabasePath(), unitConfig.StateGroup);
                var timelineFullPathList = AssetUtils.GetAssetPathList("TimelineAsset", timelineRootPath);
                var timelineList = new List<string>(timelineFullPathList.Count);
                
                foreach (var timelineFullPath in timelineFullPathList)
                {
                    var timelinePath = PathUtils.GetRelativeAssetRootPath(timelineFullPath);
                    timelineList.Add(timelinePath);
                }

                var unitConfigPath = PathUtils.GetRelativeAssetRootPath(unitConfigFullPath);
                unitList.Add(unitConfigPath);
                stateMap.Add(unitConfig.name, timelineList);
            }

            var database = new LiteNexusDatabase(unitList, stateMap);
            // save to json
            var json = LitJson.JsonMapper.ToJson(database);
            if (string.IsNullOrWhiteSpace(json) || json == "{}")
            {
                Debug.LogError("Database json is empty");
                return;
            }

            var jsonPath = LiteNexusConfig.Instance.GetDatabaseJsonPath();
            File.WriteAllText(jsonPath, json);
            AssetDatabase.Refresh();
        }
    }
}