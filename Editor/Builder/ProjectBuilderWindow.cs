using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class ProjectBuilderWindow : EditorWindow
    {
        public BuildTarget Target => _target;
        public ResBuildConfig ResCfg => _resCfg;
        public AppBuildConfig AppCfg => _appCfg;
        public CustomBuildConfig CustomCfg => _customCfg;
        
        private BuildTarget _target;
        private string _version;
        private ResBuildConfig _resCfg;
        private AppBuildConfig _appCfg;
        private CustomBuildConfig _customCfg;
        
        private List<BuilderStepView> _stepViewList;
        private ICustomBuildView _customView;

        [MenuItem("Lite/Builder &B")]
        private static void ShowWin()
        {
            var win = GetWindow<ProjectBuilderWindow>();
            win.titleContent = new GUIContent("Project Builder");
            win.minSize = new Vector2(615, 500);
            win.Show();
        }

        private void OnEnable()
        {
            _target = EditorUserBuildSettings.activeBuildTarget;
            _version = PlayerSettings.bundleVersion;
            _resCfg = new ResBuildConfig();
            _appCfg = new AppBuildConfig()
            {
                Identifier = PlayerSettings.applicationIdentifier,
                ProduceName = PlayerSettings.productName,
            };
            
#if UNITY_ANDROID
            _appCfg.BuildCode = PlayerSettings.Android.bundleVersionCode;
#elif UNITY_IOS
            if (int.TryParse(PlayerSettings.iOS.buildNumber, out var buildCode))
            {
                AppCfg_.BuildCode = buildCode;
            }
#endif
            _customCfg = new CustomBuildConfig();

            _stepViewList = new List<BuilderStepView>
            {
                new BuilderResView(this, "Res - Compile Resource Step", _resCfg),
                new BuilderAppView(this, "App - Build Application Step", _appCfg),
            };
            
            var customView = ProjectBuilderUtils.CreateCustomBuildView();
            if (customView != null)
            {
                _stepViewList.Add(new BuilderCustomView(this, "Custom - Misc Config", _customCfg, customView));
            }
        }

        private void OnDisable()
        {
        }
        
        private void OnGUI()
        {
            const int space = 5;
            
            var viewCount = _stepViewList.Count;
            var viewWidth = Mathf.Max(300, (position.width - (viewCount + 1) * space) / viewCount);
            var viewHeight = Mathf.Max(300, (position.height - space * 2) * 0.8f);
            var viewRect = new Rect(space, space, viewWidth, viewHeight);
            
            foreach (var view in _stepViewList)
            {
                view.Draw(viewRect);
                viewRect.x += (viewWidth + space);
            }

            var commonRect = new Rect(space, viewRect.yMax + space, position.width - space * 2, position.height - viewRect.yMax - space * 2);
            GUILayout.BeginArea(commonRect);
            _target = (BuildTarget) EditorGUILayout.EnumPopup("Target", _target);
            using (new EditorGUILayout.HorizontalScope())
            {
                _version = EditorGUILayout.TextField(new GUIContent("Version", "Version"), _version);
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    _version = AppUtils.GetNextVersion(_version);
                }
            }

            if (GUILayout.Button("Build"))
            {
                EditorApplication.delayCall += BuildProject;
                // BuildProject();
            }

            using (new ColorScope(Color.yellow))
            {
                EditorGUILayout.LabelField($"Customize the build pipeline using {nameof(IBuildCallback)},{nameof(ICustomBuildView)}.");
            }
            
            GUILayout.EndArea();
        }

        private void BuildProject()
        {
            if (_resCfg == null || _appCfg == null)
            {
                LEditorLog.Error("error build config");
                return;
            }

            var buildCfg = new ProjectBuildConfig(_target, _version, _resCfg, _appCfg, _customCfg);
            var buildReport = new ProjectBuilder().Build(buildCfg);
            
            var resultMsg = buildReport.IsSuccess ? "Build Success" : "Build Failed";
            var timeMsg = $"Time : {buildReport.ElapsedSeconds:0.00}s";

            if (EditorUtility.DisplayDialog("Project Builder", $"{resultMsg}\n{timeMsg}", "Confirm"))
            {
                if (buildReport.IsSuccess)
                {
                    LiteEditorUtils.OpenFolder(buildReport.OutputRootPath);
                }
            }
        }
    }
}