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
        
        public BuilderAppView(string title, Rect rect, AppBuildConfig config)
            : base(title, rect)
        {
            Config_ = config;
        }

        protected override void DrawContent()
        {
            Config_.Options = (BuildOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Options", "Res build options"), Config_.Options, false);
        }
    }
}