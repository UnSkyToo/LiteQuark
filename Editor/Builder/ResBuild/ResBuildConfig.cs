using UnityEditor;

namespace LiteQuark.Editor
{
    public sealed class ResBuildConfig
    {
        /// <summary>
        /// Enable res build step
        /// </summary>
        public bool Enable { get; set; } = true;
        
        /// <summary>
        /// Build res options
        /// </summary>
        public BuildAssetBundleOptions Options { get; set; } = BuildAssetBundleOptions.None;

        /// <summary>
        /// Clean mode will delete last build file
        /// </summary>
        public bool CleanBuildMode { get; set; } = true;
    }
}