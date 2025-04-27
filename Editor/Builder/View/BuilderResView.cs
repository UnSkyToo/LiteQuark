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
        
        public BuilderResView(ProjectBuilderWindow window, string title, ResBuildConfig config)
            : base(window, title)
        {
            Config_ = config;
        }

        protected override void DrawContent()
        {
            Config_.Options = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Options", "Res build options"), Config_.Options, false);

            Config_.HashMode = EditorGUILayout.Toggle(new GUIContent("Hash Mode", "Hash mode will be generate hash for asset bundle"), Config_.HashMode);
            
            Config_.CleanBuildMode = EditorGUILayout.Toggle(new GUIContent("Clean Mode", "Clean mode will be delete last build file"), Config_.CleanBuildMode);

            Config_.CopyToStreamingAssets = EditorGUILayout.Toggle(new GUIContent("CopyTo StreamingAssets", "Copy asset bundle to streaming assets path"), Config_.CopyToStreamingAssets);

            if (Config_.CopyToStreamingAssets)
            {
                using (new IndentLevelScope())
                {
                    Config_.CleanStreamingAssetsBeforeCopy = EditorGUILayout.Toggle(new GUIContent("Clean Before Copy", "Clean streaming assets before copy to streaming assets path"), Config_.CleanStreamingAssetsBeforeCopy);
                }
            }
        }
    }
}