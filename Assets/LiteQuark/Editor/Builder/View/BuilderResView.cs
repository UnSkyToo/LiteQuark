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
            _config.Options = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Options", "AssetBundle build options."), _config.Options, false);

            _config.HashMode = EditorGUILayout.Toggle(new GUIContent("Hash Mode", "Use bundle hashes in published filenames."), _config.HashMode);

            _config.FlatMode = EditorGUILayout.Toggle(new GUIContent("Flat Mode", "Write bundles to a flat output directory."), _config.FlatMode);

            _config.IncrementalBuild = EditorGUILayout.Toggle(new GUIContent("Incremental Build", "Keep the previous resource build output for reuse."), _config.IncrementalBuild);

            _config.CopyToStreamingAssets = EditorGUILayout.Toggle(new GUIContent("Copy to StreamingAssets", "Copy the resource build output to StreamingAssets."), _config.CopyToStreamingAssets);

            if (_config.CopyToStreamingAssets)
            {
                using (new IndentLevelScope())
                {
                    _config.CleanStreamingAssetsBeforeCopy = EditorGUILayout.Toggle(new GUIContent("Clean Before Copy", "Delete the LiteQuark subdirectory in StreamingAssets before copying."), _config.CleanStreamingAssetsBeforeCopy);
                }
            }
        }
    }
}
