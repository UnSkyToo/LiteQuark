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
            var bundlePack = collector.GetBundlePackInfo(builder);

            var manifest = collector.Manifest;
            if (manifest == null)
            {
                builder.LogError("AssetBundleManifest is null");
                return;
            }

            if (builder.ResConfig.HashMode)
            {
                bundlePack.ApplyHash(manifest);

                var rootPath = builder.GetResOutputPath();
                foreach (var bundle in bundlePack.BundleList)
                {
                    var oldPath = PathUtils.ConcatPath(rootPath, bundle.BundlePath);
                    var newPath = PathUtils.ConcatPath(rootPath, bundle.GetBundlePathWithHash());
                    PathUtils.RenameFile(oldPath, newPath);
                }
            }

            var jsonText = bundlePack.ToJson();
            PathUtils.CreateDirectory(builder.GetResOutputPath());
            System.IO.File.WriteAllText(PathUtils.ConcatPath(builder.GetResOutputPath(), LiteConst.BundlePackFileName), jsonText);
        }
    }
}