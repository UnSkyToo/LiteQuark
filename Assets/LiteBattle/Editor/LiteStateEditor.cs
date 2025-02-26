using System.Collections.Generic;
using System.IO;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    public sealed class LiteStateEditor : EditorWindow
    {
        private bool IsInit_ = false;
        private readonly List<ILiteEditorView> ViewList_ = new List<ILiteEditorView>();
        private LiteStateSceneView SceneView_;
        
        [MenuItem("Lite/State/Editor Window")]
        private static void ShowEditor()
        {
            var win = GetWindow<LiteStateEditor>();
            win.titleContent = new GUIContent("State Editor");
            win.Show();
        }

        private void OnEnable()
        {
            IsInit_ = false;
            
            LiteStateUtils.OpenStateEditorScene();
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
            if (!LiteEditorBinder.Instance.IsBindAgent())
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
            ViewList_.Add(new LiteAgentView(this));
            ViewList_.Add(new LiteTimelineView(this));
            ViewList_.Add(new LiteTimelineCtrlView(this));

            SceneView_ = new LiteStateSceneView(this);
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
            
            SaveGroupData();
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
        public LiteAgentView GetAgentView() => GetView<LiteAgentView>();
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

        private void SaveGroupData()
        {
            var groupData = new List<LiteGroupData>();
            var agentFullPathList = AssetUtils.GetAssetPathList("LiteAgentConfig", LiteStateUtils.GetAgentRootPath());
            foreach (var agentFullPath in agentFullPathList)
            {
                var agentConfig = AssetDatabase.LoadAssetAtPath<LiteAgentConfig>(agentFullPath);
                if (agentConfig == null)
                {
                    Debug.LogError($"{agentFullPath} is not LiteAgentConfig");
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(agentConfig.StateGroup))
                {
                    Debug.LogError($"{agentFullPath} StateGroup is empty");
                    continue;
                }

                var timelineRootPath = PathUtils.ConcatPath(LiteStateUtils.GetTimelineRootPath(), agentConfig.StateGroup);
                var timelineFullPathList = AssetUtils.GetAssetPathList("TimelineAsset", timelineRootPath);
                var timelineList = new List<string>(timelineFullPathList.Count);
                
                foreach (var timelineFullPath in timelineFullPathList)
                {
                    var timelinePath = PathUtils.GetRelativeAssetRootPath(timelineFullPath);
                    timelineList.Add(timelinePath);
                }

                var agentPath = PathUtils.GetRelativeAssetRootPath(agentFullPath);
                groupData.Add(new LiteGroupData(agentConfig.StateGroup, agentPath, timelineList));
            }
            
            // save to json
            var json = LitJson.JsonMapper.ToJson(groupData);
            if (string.IsNullOrWhiteSpace(json) || json == "{}")
            {
                Debug.LogError("SaveGroupData json is empty");
                return;
            }

            var jsonPath = PathUtils.ConcatPath(LiteStateConfig.Instance.DataPath, "Database.json");
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
                AssetDatabase.Refresh();
            }
            File.WriteAllText(jsonPath, json);
            AssetDatabase.Refresh();
        }
    }
}