using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class BuilderResView : BuilderStepView
    {
        public override bool Enabled
        {
            get => Config_.Enable;
            protected set => Config_.Enable = value;
        }

        private readonly ResBuildConfig Config_;
        
        public BuilderResView(string title, Rect rect, ResBuildConfig config)
            : base(title, rect)
        {
            Config_ = config;
        }

        protected override void DrawContent()
        {
            Config_.Options = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Options", "Res build options"), Config_.Options, false);

            Config_.CleanBuildMode = EditorGUILayout.Toggle(new GUIContent("Clean Mode", "Clean mode will be delete last build file"), Config_.CleanBuildMode);
        }
    }
}