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
        public BuildAssetBundleOptions Options { get; set; } = BuildAssetBundleOptions.ChunkBasedCompression |
                                                               BuildAssetBundleOptions.DisableLoadAssetByFileName |
                                                               BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;

        /// <summary>
        /// Use bundle hashes in published filenames.
        /// </summary>
        public bool HashMode { get; set; } = true;

        /// <summary>
        /// Write bundles to a flat output directory.
        /// </summary>
        public bool FlatMode { get; set; } = false;

        /// <summary>
        /// Keep the previous resource build output for reuse.
        /// </summary>
        public bool IncrementalBuild { get; set; } = false;

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
