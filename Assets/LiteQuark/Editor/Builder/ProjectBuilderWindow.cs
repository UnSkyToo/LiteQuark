﻿using UnityEditor;
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
            AppCfg_ = new AppBuildConfig()
            {
                Identifier = PlayerSettings.applicationIdentifier,
                ProduceName = PlayerSettings.productName,
                Version = PlayerSettings.bundleVersion,
#if UNITY_ANDROID
                BuildCode = PlayerSettings.Android.bundleVersionCode,
#endif
            };

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
                EditorApplication.delayCall += BuildProject;
                // BuildProject();
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

            var buildCfg = new ProjectBuildConfig(Target_, ResCfg_, AppCfg_);
            var result = new ProjectBuilder().Build(buildCfg);
            
            var resultMsg = result.IsSuccess ? "Build Success" : "Build Failed";
            var timeMsg = $"Time : {result.ElapsedSeconds:0.00}s";

            if (EditorUtility.DisplayDialog("Project Builder", $"{resultMsg}\n{timeMsg}", "Confirm"))
            {
                LiteEditorUtils.OpenFolder(result.OutputPath);
            }
        }
    }
}