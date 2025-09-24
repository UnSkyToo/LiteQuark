using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class BuilderResView : BuilderStepView
    {
        public override bool Enabled
        {
            get => _config.Enable;
            protected set => _config.Enable = value;
        }

        private readonly ResBuildConfig _config;
        
        public BuilderResView(ProjectBuilderWindow window, string title, ResBuildConfig config)
            : base(window, title)
        {
            _config = config;
        }

        protected override void DrawContent()
        {
            _config.Options = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Options", "Res build options"), _config.Options, false);

            _config.HashMode = EditorGUILayout.Toggle(new GUIContent("Hash Mode", "Hash mode will be generate hash for asset bundle"), _config.HashMode);
            
            _config.FlatMode = EditorGUILayout.Toggle(new GUIContent("Flat Mode", "Flat mode will remove bundle folder"), _config.FlatMode);
            
            _config.IncrementBuildModel = EditorGUILayout.Toggle(new GUIContent("Increment Build", "Increment build mode will be use last build info"), _config.IncrementBuildModel);
            
            _config.CleanBuildMode = EditorGUILayout.Toggle(new GUIContent("Clean Mode", "Clean mode will remove all bundle info"), _config.CleanBuildMode);

            _config.CopyToStreamingAssets = EditorGUILayout.Toggle(new GUIContent("CopyTo StreamingAssets", "Copy asset bundle to streaming assets path"), _config.CopyToStreamingAssets);

            if (_config.CopyToStreamingAssets)
            {
                using (new IndentLevelScope())
                {
                    _config.CleanStreamingAssetsBeforeCopy = EditorGUILayout.Toggle(new GUIContent("Clean Before Copy", "Clean streaming assets before copy to streaming assets path"), _config.CleanStreamingAssetsBeforeCopy);
                }
            }
        }
    }
}