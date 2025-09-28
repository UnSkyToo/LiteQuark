using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable]
    public sealed class VersionPackInfo
    {
        public bool IsValid { get; private set; }
        public string Version { get; private set; }
        public string Platform { get; private set; }
        public bool HashMode { get; private set; }
        public bool FlatMode { get; private set; }
        public BundleInfo[] BundleList { get; private set; }

        private Dictionary<string, string> _bundleFileLoadPathCache = new ();
        private Dictionary<string, BundleInfo> _bundlePathToBundleCache = new ();
        private Dictionary<string, BundleInfo> _assetPathToBundleCache = new ();

        public VersionPackInfo()
        {
            IsValid = false;
            BundleList = Array.Empty<BundleInfo>();
        }

        public VersionPackInfo(string version, string platform, bool hashMode, bool flatMode, BundleInfo[] bundleList)
        {
            Version = version;
            Platform = platform;
            HashMode = hashMode;
            FlatMode = flatMode;
            BundleList = bundleList;
            
            IsValid = true;
        }

        public void Initialize()
        {
            _bundleFileLoadPathCache.Clear();
            _bundlePathToBundleCache.Clear();
            _assetPathToBundleCache.Clear();
            
            foreach (var bundle in BundleList)
            {
                _bundlePathToBundleCache.Add(bundle.BundlePath, bundle);
                
                foreach (var assetPath in bundle.AssetList)
                {
                    _assetPathToBundleCache.Add(assetPath, bundle);
                }
            }
        }

        public void ApplyHash(AssetBundleManifest manifest)
        {
            if (manifest == null)
            {
                return;
            }
            
            foreach (var bundle in BundleList)
            {
                var bundlePath = GetBundleFileBuildPath(bundle);
                bundle.Hash = manifest.GetAssetBundleHash(bundlePath).ToString();
                bundle.Hash = bundle.Hash.Replace(" ", string.Empty).ToLower();
            }
        }

        private void SimplifyPath()
        {
            var bundleIDMap = new Dictionary<string, string>(BundleList.Length);
            foreach (var bundle in BundleList)
            {
                bundleIDMap.Add(bundle.BundlePath, bundle.BundleID.ToString());
            }
            
            const string bundleFileExt = LiteConst.BundleFileExt;
            
            foreach (var bundle in BundleList)
            {
                var bundlePath = $"{bundle.BundlePath.Substring(0, bundle.BundlePath.Length - bundleFileExt.Length)}/";
                
                for (var index = 0; index < bundle.AssetList.Length; index++)
                {
                    bundle.AssetList[index] = PathUtils.GetRelativePath(bundlePath, bundle.AssetList[index]);
                }

                for (var index = 0; index < bundle.DependencyList.Length; index++)
                {
                    var dependencyPath = bundle.DependencyList[index];
                    if (bundleIDMap.TryGetValue(dependencyPath, out var dependencyId))
                    {
                        bundle.DependencyList[index] = dependencyId;
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Dependency not found: {dependencyPath}, bundle: {bundle.BundlePath}");
                    }
                }
            }
        }

        private void RestorePath()
        {
            var bundleIDMap = new Dictionary<string, string>(BundleList.Length);
            foreach (var bundle in BundleList)
            {
                bundleIDMap.Add(bundle.BundleID.ToString(), bundle.BundlePath);
            }
            
            const string bundleFileExt = LiteConst.BundleFileExt;
            
            foreach (var bundle in BundleList)
            {
                var bundlePath = $"{bundle.BundlePath.Substring(0, bundle.BundlePath.Length - bundleFileExt.Length)}/";
                
                for (var index = 0; index < bundle.AssetList.Length; index++)
                {
                    var assetPath = bundle.AssetList[index];
                    if (!assetPath.StartsWith(LiteConst.AssetRootName, StringComparison.OrdinalIgnoreCase))
                    {
                        bundle.AssetList[index] = $"{bundlePath}{assetPath}";
                    }
                }

                for (var index = 0; index < bundle.DependencyList.Length; index++)
                {
                    var dependencyId = bundle.DependencyList[index];
                    if (bundleIDMap.TryGetValue(dependencyId, out var dependencyPath))
                    {
                        bundle.DependencyList[index] = dependencyPath;
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Dependency ID not found: {dependencyId}, bundle: {bundle.BundlePath}");
                    }
                }
            }
        }
        
        public BundleInfo GetBundleInfoFromBundlePath(string bundlePath)
        {
            if (_bundlePathToBundleCache.TryGetValue(bundlePath, out var info))
            {
                return info;
            }

            LLog.Error("Can't get bundle info : {0}", bundlePath);
            return null;
        }

        public BundleInfo GetBundleInfoFromAssetPath(string assetPath)
        {
            if (_assetPathToBundleCache.TryGetValue(assetPath, out var info))
            {
                return info;
            }
            
            LLog.Error("Can't get bundle info : {0}", assetPath);
            return null;
        }

        public string GetBundleFileLoadPath(BundleInfo bundleInfo)
        {
            if (_bundleFileLoadPathCache.TryGetValue(bundleInfo.BundlePath, out var loadPath))
            {
                return loadPath;
            }

            loadPath = GetBundleFileBuildPath(bundleInfo);
            if (HashMode)
            {
                loadPath = System.IO.Path.ChangeExtension(loadPath, $"_{bundleInfo.Hash}{LiteConst.BundleFileExt}");
            }

            if (LiteConst.SecurityMode && HashMode && FlatMode)
            {
                loadPath = $"{bundleInfo.Hash}{LiteConst.BundleFileExt}";
            }
            
            _bundleFileLoadPathCache.Add(bundleInfo.BundlePath, loadPath);
            return loadPath;
        }

        public string GetBundleFileBuildPath(BundleInfo bundleInfo)
        {
            return FlatMode ? PathUtils.ToFlatPath(bundleInfo.BundlePath) : bundleInfo.BundlePath;
        }

        public byte[] ToBinaryData()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("VersionPackInfo is not valid");
            }
            
            SimplifyPath();
            try
            {
                var jsonText = LitJson.JsonMapper.ToJson(this);
                var jsonData = Encoding.UTF8.GetBytes(jsonText);
                if (LiteConst.SecurityMode)
                {
                    return SecurityUtils.AesEncrypt(jsonData, LiteConst.SecurityKey);
                }

                return jsonData;
            }
            catch(Exception ex)
            {
                LLog.Exception(ex, "Failed to serialize VersionPackInfo, SecurityMode:{0}", LiteConst.SecurityMode);
                throw;
            }
        }

        public static VersionPackInfo FromBinaryData(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                var jsonData = LiteConst.SecurityMode ? SecurityUtils.AesDecrypt(data, LiteConst.SecurityKey) : data;
                var jsonText = Encoding.UTF8.GetString(jsonData);
                var packInfo = LitJson.JsonMapper.ToObject<VersionPackInfo>(jsonText);
                packInfo.RestorePath();
                return packInfo;
            }
            catch (Exception ex)
            {
                LLog.Exception(ex, "Failed to deserialize VersionPackInfo, SecurityMode:{0}", LiteConst.SecurityMode);
                throw;
            }
        }
    }
}