using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class ProjectBuilderWindow : EditorWindow
    {
        public BuildTarget Target => Target_;
        public ResBuildConfig ResCfg => ResCfg_;
        public AppBuildConfig AppCfg => AppCfg_;
        public CustomBuildConfig CustomCfg => CustomCfg_;
        
        private BuildTarget Target_;
        private string Version_;
        private ResBuildConfig ResCfg_;
        private AppBuildConfig AppCfg_;
        private CustomBuildConfig CustomCfg_;
        
        private List<BuilderStepView> StepViewList_;
        private ICustomBuildView CustomView_;

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
            Target_ = EditorUserBuildSettings.activeBuildTarget;
            Version_ = PlayerSettings.bundleVersion;
            ResCfg_ = new ResBuildConfig();
            AppCfg_ = new AppBuildConfig()
            {
                Identifier = PlayerSettings.applicationIdentifier,
                ProduceName = PlayerSettings.productName,
            };
            
#if UNITY_ANDROID
            AppCfg_.BuildCode = PlayerSettings.Android.bundleVersionCode;
#elif UNITY_IOS
            if (int.TryParse(PlayerSettings.iOS.buildNumber, out var buildCode))
            {
                AppCfg_.BuildCode = buildCode;
            }
#endif
            CustomCfg_ = new CustomBuildConfig();

            StepViewList_ = new List<BuilderStepView>
            {
                new BuilderResView(this, "Res - Compile Resource Step", ResCfg_),
                new BuilderAppView(this, "App - Build Application Step", AppCfg_),
            };
            
            var customView = ProjectBuilderUtils.CreateCustomBuildView();
            if (customView != null)
            {
                StepViewList_.Add(new BuilderCustomView(this, "Custom - Misc Config", CustomCfg_, customView));
            }
        }

        private void OnDisable()
        {
        }
        
        private void OnGUI()
        {
            const int space = 5;
            
            var viewCount = StepViewList_.Count;
            var viewWidth = Mathf.Max(300, (position.width - (viewCount + 1) * space) / viewCount);
            var viewHeight = Mathf.Max(300, (position.height - space * 2) * 0.8f);
            var viewRect = new Rect(space, space, viewWidth, viewHeight);
            
            foreach (var view in StepViewList_)
            {
                view.Draw(viewRect);
                viewRect.x += (viewWidth + space);
            }

            var commonRect = new Rect(space, viewRect.yMax + space, position.width - space * 2, position.height - viewRect.yMax - space * 2);
            GUILayout.BeginArea(commonRect);
            Target_ = (BuildTarget) EditorGUILayout.EnumPopup("Target", Target_);
            using (new EditorGUILayout.HorizontalScope())
            {
                Version_ = EditorGUILayout.TextField(new GUIContent("Version", "Version"), Version_);
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    Version_ = AppUtils.GetNextVersion(Version_);
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
            if (ResCfg_ == null || AppCfg_ == null)
            {
                LEditorLog.Error("error build config");
                return;
            }

            var buildCfg = new ProjectBuildConfig(Target_, Version_, ResCfg_, AppCfg_, CustomCfg_);
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