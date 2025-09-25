using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
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
            var bundleIDMap = new Dictionary<string, string>();
            foreach (var bundle in BundleList)
            {
                bundleIDMap.Add(bundle.BundlePath, bundle.BundleID.ToString());
            }
            
            foreach (var bundle in BundleList)
            {
                var bundlePath = $"{bundle.BundlePath.Replace(LiteConst.BundleFileExt, string.Empty)}/";
                for (var index = 0; index < bundle.AssetList.Length; index++)
                {
                    bundle.AssetList[index] = PathUtils.GetRelativePath(bundlePath, bundle.AssetList[index]);
                }

                for (var index = 0; index < bundle.DependencyList.Length; index++)
                {
                    bundle.DependencyList[index] = bundleIDMap[bundle.DependencyList[index]];
                }
            }
        }

        private void RestorePath()
        {
            var bundleIDMap = new Dictionary<string, string>();
            foreach (var bundle in BundleList)
            {
                bundleIDMap.Add(bundle.BundleID.ToString(), bundle.BundlePath);
            }
            
            foreach (var bundle in BundleList)
            {
                var bundlePath = $"{bundle.BundlePath.Replace(LiteConst.BundleFileExt, string.Empty)}/";
                for (var index = 0; index < bundle.AssetList.Length; index++)
                {
                    if (!bundle.AssetList[index].StartsWith("assets"))
                    {
                        bundle.AssetList[index] = $"{bundlePath}{bundle.AssetList[index]}";
                    }
                }

                for (var index = 0; index < bundle.DependencyList.Length; index++)
                {
                    bundle.DependencyList[index] = bundleIDMap[bundle.DependencyList[index]];
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
                loadPath = loadPath.Replace(LiteConst.BundleFileExt, $"_{bundleInfo.Hash}{LiteConst.BundleFileExt}");
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
            SimplifyPath();
            var jsonText = LitJson.JsonMapper.ToJson(this);
            var jsonData = Encoding.UTF8.GetBytes(jsonText);
            if (LiteConst.SecurityMode)
            {
                return SecurityUtils.AesEncrypt(jsonData, LiteConst.SecurityKey);
            }
            return jsonData;
        }

        public static VersionPackInfo FromBinaryData(byte[] data)
        {
            var jsonData = LiteConst.SecurityMode ? SecurityUtils.AesDecrypt(data, LiteConst.SecurityKey) : data;
            var jsonText = Encoding.UTF8.GetString(jsonData);
            var packInfo = LitJson.JsonMapper.ToObject<VersionPackInfo>(jsonText);
            packInfo.RestorePath();
            return packInfo;
        }
        
        public static void LoadPackAsync(string bundleUri, Action<VersionPackInfo> callback)
        {
            try
            {
                LiteRuntime.Task.UnityWebGetRequestTask(bundleUri, 0, true, (downloadHandler) =>
                {
                    if (downloadHandler?.isDone ?? false)
                    {
                        var info = FromBinaryData(downloadHandler.data);

                        if (info is not { IsValid: true })
                        {
                            LLog.Error("Bundle package parse error\n{0}", downloadHandler.error);
                            callback?.Invoke(null);
                            return;
                        }

                        info.Initialize();
                        callback?.Invoke(info);
                    }
                    else
                    {
                        LLog.Error("Bundle package download error : {0}\n{1}", bundleUri, downloadHandler?.error);
                        callback?.Invoke(null);
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static UniTask<VersionPackInfo> LoadPackAsync(string bundleUri)
        {
            try
            {
                var tcs = new UniTaskCompletionSource<VersionPackInfo>();
                LoadPackAsync(bundleUri, (info) =>
                {
                    tcs.TrySetResult(info);
                });
                return tcs.Task;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}