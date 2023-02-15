using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Collect asset bundle info and apply, write pack info to json
    /// </summary>
    internal sealed class ResCollectInfoStep : IBuildStep
    {
        public string Name => "Res Collect Info Step";
        
        private string DefaultBundlePath => "default.ab";

        private Dictionary<string, BundleInfo> BundleInfoCache_ = new Dictionary<string, BundleInfo>();

        public void Execute(ProjectBuilder builder)
        {
            var bundlePack = GenerateBundlePackInfo(builder.Target);
            ApplyBundleInfo(bundlePack);
            
            var jsonText = bundlePack.ToJson();
            PathUtils.CreateDirectory(builder.GetResOutputPath());
            System.IO.File.WriteAllText(PathUtils.ConcatPath(builder.GetResOutputPath(), LiteConst.BundlePackFileName), jsonText);
        }

        private BundlePackInfo GenerateBundlePackInfo(BuildTarget target)
        {
            BundleInfoCache_.Clear();
            CollectBundleInfo(LiteConst.AssetRootPath);
            return new BundlePackInfo(target.ToString(), BundleInfoCache_.Values.ToArray());
        }

        private void AddToBundleInfoCache(string bundlePath, string[] assetList, string[] dependencyList)
        {
            if (BundleInfoCache_.TryGetValue(bundlePath, out var cache))
            {
                cache.BundlePath = bundlePath;
                cache.AssetList = ArrayUtils.AppendArray(cache.AssetList, assetList, false);
                cache.DependencyList = ArrayUtils.AppendArray(cache.DependencyList, dependencyList, false);
            }
            else
            {
                cache = new BundleInfo(bundlePath, assetList, dependencyList);
                BundleInfoCache_.Add(bundlePath, cache);
            }
        }
        
        private void CollectBundleInfo(string rootPath)
        {
            rootPath = PathUtils.UnifyPath(rootPath);

            if (rootPath.Contains("#"))
            {
                return;
            }

            if (!PathUtils.DirectoryExists(rootPath))
            {
                return;
            }
            
            var subBundlePathList = PathUtils.GetDirectoryList(rootPath);
            
            foreach (var subBundlePath in subBundlePathList)
            {
                CollectBundleInfo(subBundlePath);
            }

            CreateBundleInfo(rootPath);
        }
        
        private void CreateBundleInfo(string bundleFullPath)
        {
            var filePathList = PathUtils.GetFileList(bundleFullPath);
            CreateBundleInfo(bundleFullPath, filePathList);
        }
        
        private void CreateBundleInfo(string bundleFullPath, string[] filePathList)
        {
            var assetPathList = filePathList.Where(AssetFilter).ToArray();
            
            if (assetPathList.Length > 0)
            {
                var assetList = new List<string>();
                var dependencyList = new List<string>();
                
                foreach (var assetPath in assetPathList)
                {
                    assetList.Add(PathUtils.GetRelativeAssetRootPath(assetPath).ToLower());
                    
                    foreach (var dep in GetDependencyList(assetPath))
                    {
                        if (!dependencyList.Contains(dep))
                        {
                            dependencyList.Add(dep);
                        }
                    }
                }

                var bundlePath = $"{GetBundlePathFromFullPath(bundleFullPath)}";
                dependencyList.Remove(bundlePath);
                AddToBundleInfoCache(bundlePath, assetList.ToArray(), dependencyList.ToArray());
            }
        }
        
        private string[] GetDependencyList(string assetPath)
        {
            var dependencyPathList = AssetDatabase.GetDependencies(assetPath);
            var result = new List<string>();

            foreach (var dependencyPath in dependencyPathList)
            {
                if (string.Compare(dependencyPath, assetPath, StringComparison.OrdinalIgnoreCase) == 0 || !AssetFilter(dependencyPath))
                {
                    continue;
                }
                
                var path = GetBundlePathFromFullPath(dependencyPath);
                if (!result.Contains(path))
                {
                    CreateBundleInfo(dependencyPath, new[] { PathUtils.GetRelativeAssetRootPath(dependencyPath).ToLower() });
                    result.Add(path);
                }
            }

            return result.ToArray();
        }
        
        private string GetBundlePathFromFullPath(string fullPath)
        {
            var bundlePath = PathUtils.GetRelativeAssetRootPath(fullPath);

            if (string.IsNullOrWhiteSpace(bundlePath))
            {
                return DefaultBundlePath;
            }

            bundlePath = PathUtils.GetPathFromFullPath(bundlePath);
            bundlePath = bundlePath.ToLower();

            return $"{bundlePath}.ab";
        }
        
        private bool AssetFilter(string filePath)
        {
            if (filePath.Contains("#"))
            {
                return false;
            }

            var ext = System.IO.Path.GetExtension(filePath);
            if (ext is ".meta" or ".dll" or ".cs" or ".js" or ".boo" or ".psd")
            {
                return false;
            }

            if (ext is ".ttf" or ".ttc" or ".otf")
            {
                return false;
            }

            return true;
        }
        
        private void ApplyBundleInfo(BundlePackInfo packInfo)
        {
            foreach (var buildInfo in packInfo.BundleList)
            {
                if (buildInfo.DependencyList.Contains(buildInfo.BundlePath))
                {
                    LLogEditor.Error($"loop reference : {buildInfo.BundlePath}");
                }
                
                foreach (var assetPath in buildInfo.AssetList)
                {
                    var fullPath = assetPath.StartsWith("assets") ? assetPath : PathUtils.GetFullPathInAssetRoot(assetPath);
                    var importer = AssetImporter.GetAtPath(fullPath);
                    importer.SetAssetBundleNameAndVariant(buildInfo.BundlePath, string.Empty);
                }
            }
            
            AssetDatabase.Refresh();
        }
    }
}