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
            var bundleInfoList = CollectBundleInfo(LiteConst.AssetRootPath);
            return new BundlePackInfo(target.ToString(), bundleInfoList);
        }
        
        private BundleInfo[] CollectBundleInfo(string rootPath)
        {
            rootPath = PathUtils.UnifyPath(rootPath);

            if (!PathUtils.DirectoryExists(rootPath))
            {
                return Array.Empty<BundleInfo>();
            }
            
            var infoList = new List<BundleInfo>();
            var subBundlePathList = PathUtils.GetDirectoryList(rootPath);
            
            foreach (var subBundlePath in subBundlePathList)
            {
                var subInfoList = CollectBundleInfo(subBundlePath);
                infoList.AddRange(subInfoList);
            }

            var bundleInfo = CreateBundleInfo(rootPath);
            if (bundleInfo != null)
            {
                infoList.Add(bundleInfo);
            }

            return infoList.ToArray();
        }
        
        private BundleInfo CreateBundleInfo(string bundlePath)
        {
            var filePathList = PathUtils.GetFileList(bundlePath);
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

                return new BundleInfo($"{GetBundlePathFromFullPath(bundlePath)}", assetList.ToArray(), dependencyList.ToArray());
            }

            return null;
        }
        
        private string[] GetDependencyList(string assetPath)
        {
            var dependencyPathList = AssetDatabase.GetDependencies(assetPath);
            var result = new List<string>();

            foreach (var dependencyPath in dependencyPathList)
            {
                if (dependencyPath == assetPath || !AssetFilter(assetPath))
                {
                    continue;
                }
                
                var path = GetBundlePathFromFullPath(dependencyPath);
                if (!result.Contains(path))
                {
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
            var ext = System.IO.Path.GetExtension(filePath);
            
            if (ext is ".meta" or ".dll" or ".cs" or ".js" or ".boo")
            {
                return false;
            }

            return true;
        }
        
        private void ApplyBundleInfo(BundlePackInfo packInfo)
        {
            foreach (var buildInfo in packInfo.BundleList)
            {
                foreach (var assetPath in buildInfo.AssetList)
                {
                    var fullPath = PathUtils.GetFullPathInAssetRoot(assetPath);
                    var importer = AssetImporter.GetAtPath(fullPath);
                    importer.SetAssetBundleNameAndVariant(buildInfo.BundlePath, string.Empty);
                }
            }
            
            AssetDatabase.Refresh();
        }
    }
}