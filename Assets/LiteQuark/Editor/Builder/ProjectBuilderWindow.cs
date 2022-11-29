using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public sealed class ProjectBuilderWindow : EditorWindow
    {
        private BuildTarget Target_;
        private ResBuildConfig ResCfg_;
        private AppBuildConfig AppCfg_;
        
        private BuilderStepView[] StepViewList_;

        [MenuItem("Lite/Builder &B")]
        private static void ShowWin()
        {
            var win = GetWindow<ProjectBuilderWindow>();
            win.titleContent = new GUIContent("Project Builder");
            win.Show();
        }

        private void OnEnable()
        {
            Target_ = EditorUserBuildSettings.activeBuildTarget;
            ResCfg_ = new ResBuildConfig();
            AppCfg_ = new AppBuildConfig();

            StepViewList_ = new BuilderStepView[]
            {
                new BuilderResView("Res - Compile Resource Step", new Rect(5, 5, 300, 400), ResCfg_),
                new BuilderAppView("App - Build Application Step", new Rect(350, 5, 300, 400), AppCfg_),
            };
        }

        private void OnDisable()
        {
        }
        
        private void OnGUI()
        {
            foreach (var view in StepViewList_)
            {
                view.Draw();
            }
            
            GUILayout.BeginArea(new Rect(0, 410, 400, position.height - 410));
            Target_ = (BuildTarget) EditorGUILayout.EnumPopup("Target", Target_);

            if (GUILayout.Button("Build"))
            {
                BuildProject();
            }
            
            GUILayout.EndArea();
        }

        private void BuildProject()
        {
            if (ResCfg_ == null || AppCfg_ == null)
            {
                Debug.LogError("error build config");
                return;
            }

            var buildCfg = new ProjectBuildConfig(Target_, ResCfg_, AppCfg_);
            var result = new ProjectBuilder().Build(buildCfg);

            if (result.IsSuccess)
            {
                EditorUtility.DisplayDialog("Project Builder", $"Build Success\nTime : {result.ElapsedSeconds:0.00}s", "Confirm");
            }
            else
            {
                EditorUtility.DisplayDialog("Project Builder", $"Build Failed\nTime : {result.ElapsedSeconds:0.00}s", "Confirm");
            }
        }
    }
}