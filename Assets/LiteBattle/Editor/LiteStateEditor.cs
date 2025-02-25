using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    public sealed class LiteStateEditor : EditorWindow
    {
        private static LiteStateEditor Instance_;
        public static LiteStateEditor Instance
        {
            get
            {
                if (Instance_ == null)
                {
                    ShowEditor();
                }

                return Instance_;
            }
        }

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
            Instance_ = this;
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

            Instance_ = null;
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

        public string GetCurrentAgentTimelineRootPath()
        {
            var subPath = LiteEditorBinder.Instance.GetCurrentStateGroup();
            return PathUtils.ConcatPath(LiteStateUtils.GetTimelineRootPath(), subPath);
        }
        
        public List<string> GetCurrentAgentTimelinePathList()
        {
            if (string.IsNullOrWhiteSpace(LiteEditorBinder.Instance.GetCurrentStateGroup()))
            {
                return new List<string>();
            }

            return LiteAssetHelper.GetAssetPathList("TimelineAsset", GetCurrentAgentTimelineRootPath());
        }
    }
}