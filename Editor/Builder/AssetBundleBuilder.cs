using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteQuark.Runtime;
using LiteQuark.Runtime.Internal;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public class AssetBundleBuilder
    {
        [MenuItem("Lite/Build Asset")]
        private static void Func()
        {
            new AssetBundleBuilder().Build(EditorUserBuildSettings.activeBuildTarget, BuildAssetBundleOptions.None);
        }
        
        private string DefaultBundlePath => "default.ab";
        
        public void Build(BuildTarget target, BuildAssetBundleOptions options)
        {
            CleanBundleFile(target);
            CleanBundleName();

            var bundlePack = GenerateBundlePackInfo(target);
            ApplyBundleInfo(bundlePack);
            BuildAllBundle(target, options);
            
            GenerateBuildInfoFile(target, bundlePack);
            
            CopyBundleToStreamingPath(target);

            LiteLog.Instance.Info("Build Success");
        }

        private void CleanBundleFile(BuildTarget target)
        {
            PathHelper.DeleteDirectory(GetBundleOutputPath(target));
            PathHelper.DeleteDirectory(Application.streamingAssetsPath);
            AssetDatabase.Refresh();
        }

        private void CleanBundleName()
        {
            var assetBundleNameList = AssetDatabase.GetAllAssetBundleNames();
            foreach (var assetBundleName in assetBundleNameList)
            {
                var assetPathList = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                foreach (var assetPath in assetPathList)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    importer.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
                }
            }
            
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }

        private void ApplyBundleInfo(BundlePackInfo packInfo)
        {
            foreach (var buildInfo in packInfo.BundleList)
            {
                foreach (var assetPath in buildInfo.AssetList)
                {
                    var fullPath = PathHelper.GetFullPathInAssetRoot(assetPath);
                    var importer = AssetImporter.GetAtPath(fullPath);
                    importer.SetAssetBundleNameAndVariant(buildInfo.BundlePath, string.Empty);
                }
            }
        }

        private void BuildAllBundle(BuildTarget target, BuildAssetBundleOptions options)
        {
            var outputPath = GetBundleOutputPath(target);
            PathHelper.CreateDirectory(outputPath);
            BuildPipeline.BuildAssetBundles(outputPath, options, target);
            AssetDatabase.Refresh();
        }

        private void CopyBundleToStreamingPath(BuildTarget target)
        {
            bool Filter(string path)
            {
                if (path.EndsWith(".manifest") || path.EndsWith(target.ToString()))
                {
                    return false;
                }

                return true;
            }
            
            PathHelper.CopyDirectory(GetBundleOutputPath(target), Application.streamingAssetsPath, Filter);
            AssetDatabase.Refresh();
        }

        private BundlePackInfo GenerateBundlePackInfo(BuildTarget target)
        {
            var bundleInfoList = CollectBundleInfo(LiteConst.AssetRootPath);
            return new BundlePackInfo(target.ToString(), bundleInfoList);
        }

        private BundleInfo[] CollectBundleInfo(string rootPath)
        {
            rootPath = PathHelper.UnifyPath(rootPath);

            if (!PathHelper.DirectoryExists(rootPath))
            {
                return Array.Empty<BundleInfo>();
            }
            
            var infoList = new List<BundleInfo>();
            var subBundlePathList = PathHelper.GetDirectoryList(rootPath);
            
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
            var filePathList = PathHelper.GetFileList(bundlePath);
            var assetPathList = filePathList.Where(AssetFilter).ToArray();
            
            if (assetPathList.Length > 0)
            {
                var assetList = new List<string>();
                var dependencyList = new List<string>();
                
                foreach (var assetPath in assetPathList)
                {
                    assetList.Add(PathHelper.GetRelativeAssetRootPath(assetPath).ToLower());
                    
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
            var bundlePath = PathHelper.GetRelativeAssetRootPath(fullPath);

            if (string.IsNullOrWhiteSpace(bundlePath))
            {
                return DefaultBundlePath;
            }

            bundlePath = PathHelper.GetPathFromFullPath(bundlePath);
            bundlePath = bundlePath.ToLower();

            return $"{bundlePath}.ab";
        }

        private bool AssetFilter(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            
            if (ext is ".meta" or ".dll" or ".cs" or ".js" or ".boo")
            {
                return false;
            }

            return true;
        }

        private string GetBundleOutputPath(BuildTarget target)
        {
            return PathHelper.GetLiteRootPath($"BuildBundle/{target}");
        }

        private void GenerateBuildInfoFile(BuildTarget target, BundlePackInfo packInfo)
        {
            var jsonText = packInfo.ToJson();
            File.WriteAllText(PathHelper.ConcatPath(GetBundleOutputPath(target), LiteConst.BundlePackFileName), jsonText);
        }
    }
}