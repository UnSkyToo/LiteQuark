using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class BuilderAppView : BuilderStepView
    {
        public override bool Enabled
        {
            get => Config_.Enable;
            protected set => Config_.Enable = value;
        }
        
        private readonly AppBuildConfig Config_;
        
        public BuilderAppView(ProjectBuilderWindow window, string title, Rect rect, AppBuildConfig config)
            : base(window, title, rect)
        {
            Config_ = config;
        }

        protected override void DrawContent()
        {
            Config_.Identifier = EditorGUILayout.TextField(new GUIContent("Identifier", "App Identifier"), Config_.Identifier);
            Config_.ProduceName = EditorGUILayout.TextField(new GUIContent("Produce Name", "Display app name"), Config_.ProduceName);
            Config_.Version = EditorGUILayout.TextField(new GUIContent("Version", "App version"), Config_.Version);

            using (new EditorGUILayout.HorizontalScope())
            {
                Config_.BuildCode = EditorGUILayout.IntField(new GUIContent("Build Code", "App build code"), Config_.BuildCode);
                if (GUILayout.Button("-"))
                {
                    Config_.BuildCode--;
                }

                if (GUILayout.Button("+"))
                {
                    Config_.BuildCode++;
                }
            }

            Config_.Options = (BuildOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Options", "Res build options"), Config_.Options, false);
            Config_.Backend = (ScriptingImplementation)EditorGUILayout.EnumPopup(new GUIContent("Backend", "Script backend"), Config_.Backend);

            if (Window.Target == BuildTarget.Android)
            {
                Config_.Architecture = (AndroidArchitecture)EditorGUILayout.EnumFlagsField(new GUIContent("Architecture", "Android CPU architecture"), Config_.Architecture);
            }
            else if (Window.Target == BuildTarget.iOS)
            {
                Config_.TargetDevice = (iOSTargetDevice)EditorGUILayout.EnumPopup(new GUIContent("TargetDevice", "Target iOS device"), Config_.TargetDevice);
            }

            Config_.IsDevelopmentBuild = EditorGUILayout.Toggle(new GUIContent("Development Build", "Is development build"), Config_.IsDevelopmentBuild);
        }
    }
}