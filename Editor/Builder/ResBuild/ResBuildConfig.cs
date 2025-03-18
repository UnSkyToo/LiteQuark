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
        public BuildAssetBundleOptions Options { get; set; } = BuildAssetBundleOptions.ChunkBasedCompression;

        /// <summary>
        /// Enable hash mode for bundle
        /// </summary>
        public bool HashMode { get; set; } = false;
        
        /// <summary>
        /// Clean mode will delete last build file
        /// </summary>
        public bool CleanBuildMode { get; set; } = true;

        /// <summary>
        /// Copy asset bundle to streaming asset folder
        /// </summary>
        public bool CopyToStreamingAssets { get; set; } = true;
        
        /// <summary>
        /// Clean folder before copy asset bundle to streaming asset
        /// </summary>
        public bool CleanStreamingAssetsBeforeCopy { get; set; } = true;
    }
}