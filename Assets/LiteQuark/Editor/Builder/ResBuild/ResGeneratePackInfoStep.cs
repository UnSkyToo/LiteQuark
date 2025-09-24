using LiteQuark.Runtime;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Write bundle pack info to json
    /// </summary>
    internal sealed class ResGeneratePackInfoStep : IBuildStep
    {
        public string Name => "Res Generate PackInfo Step";
        
        public void Execute(ProjectBuilder builder)
        {
            var collector = builder.Collector;
            var versionPack = collector.GetVersionPackInfo(builder);

            var manifest = collector.Manifest;
            if (manifest == null)
            {
                builder.LogError("AssetBundleManifest is null");
                return;
            }
            
            var rootPath = builder.GetResOutputPath();
            foreach (var bundle in versionPack.BundleList)
            {
                var bundlePath = versionPack.GetBundleFileBuildPath(bundle);
                var fullPath = PathUtils.ConcatPath(rootPath, bundlePath);
                var fileInfo = new System.IO.FileInfo(fullPath);
                if (!fileInfo.Exists)
                {
                    LEditorLog.Warning($"Bundle file {fullPath} not exists");
                    continue;
                }
                bundle.Size = fileInfo.Length;
            }

            if (builder.ResConfig.HashMode)
            {
                versionPack.ApplyHash(manifest);
                
                foreach (var bundleInfo in versionPack.BundleList)
                {
                    var oldPath = PathUtils.ConcatPath(rootPath, versionPack.GetBundleFileBuildPath(bundleInfo));
                    var newPath = PathUtils.ConcatPath(rootPath, versionPack.GetBundleFileLoadPath(bundleInfo));
                    PathUtils.RenameFile(oldPath, newPath);
                }
            }
            
            var jsonData = versionPack.ToBinaryData();
            PathUtils.CreateDirectory(builder.GetResOutputPath());
            System.IO.File.WriteAllBytes(PathUtils.ConcatPath(builder.GetResOutputPath(), AppUtils.GetVersionFileName()), jsonData);
        }
    }
}