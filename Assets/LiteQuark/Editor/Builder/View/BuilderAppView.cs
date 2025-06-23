using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class BuilderAppView : BuilderStepView
    {
        public override bool Enabled
        {
            get => _config.Enable;
            protected set => _config.Enable = value;
        }
        
        private readonly AppBuildConfig _config;
        
        public BuilderAppView(ProjectBuilderWindow window, string title, AppBuildConfig config)
            : base(window, title)
        {
            _config = config;
        }

        protected override void DrawContent()
        {
            _config.Identifier = EditorGUILayout.TextField(new GUIContent("Identifier", "App Identifier"), _config.Identifier);
            _config.ProduceName = EditorGUILayout.TextField(new GUIContent("Produce Name", "Display app name"), _config.ProduceName);

            using (new EditorGUILayout.HorizontalScope())
            {
                _config.BuildCode = EditorGUILayout.IntField(new GUIContent("Build Code", "App build code"), _config.BuildCode);
                if (GUILayout.Button("-"))
                {
                    _config.BuildCode--;
                }

                if (GUILayout.Button("+"))
                {
                    _config.BuildCode++;
                }
            }

            _config.Options = (BuildOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Options", "Res build options"), _config.Options, false);
            _config.CleanBuildMode = EditorGUILayout.Toggle(new GUIContent("Clean Mode", "Clean mode will be delete last build file"), _config.CleanBuildMode);
            _config.Backend = (ScriptingImplementation)EditorGUILayout.EnumPopup(new GUIContent("Backend", "Script backend"), _config.Backend);

            if (Window.Target == BuildTarget.Android)
            {
                _config.Architecture = (AndroidArchitecture)EditorGUILayout.EnumFlagsField(new GUIContent("Architecture", "Android CPU architecture"), _config.Architecture);
                _config.IsAAB = EditorGUILayout.Toggle(new GUIContent("IsAAB", "Build with app bundle"), _config.IsAAB);
                if (_config.IsAAB)
                {
                    _config.SplitApplicationBinary = EditorGUILayout.Toggle(new GUIContent("Split Application Binary", "Split application binary for Play Asset Delivery"), _config.SplitApplicationBinary);
                }
                _config.CreateSymbols = (AndroidCreateSymbols)EditorGUILayout.EnumPopup(new GUIContent("Create Symbols", "Create Symbol for this build"), _config.CreateSymbols);
            }
            else if (Window.Target == BuildTarget.iOS)
            {
                _config.TargetDevice = (iOSTargetDevice)EditorGUILayout.EnumPopup(new GUIContent("TargetDevice", "Target iOS device"), _config.TargetDevice);
            }

            EditorGUI.BeginChangeCheck();
            _config.IsDevelopmentBuild = EditorGUILayout.Toggle(new GUIContent("Development Build", "Is development build"), _config.IsDevelopmentBuild);
            if (EditorGUI.EndChangeCheck())
            {
                // force open clean mode when release build
                if (!_config.IsDevelopmentBuild)
                {
                    _config.CleanBuildMode = true;
                    Window.ResCfg.IncrementBuildModel = false;
                    Window.ResCfg.CleanBuildMode = true;
                    Window.ResCfg.CleanStreamingAssetsBeforeCopy = true;
                }
            }
        }
    }
}