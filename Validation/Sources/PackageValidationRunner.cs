using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class PackageValidationRunner
    {
        private const string PackageAsset = "Packages/com.unity.ai.navigation/Gizmos/NavMeshSurface Icon.png";
        private const string Fixture = "Assets/StandaloneAssets/__PackageValidationRunner20261022";
        private const string MaterialPath = Fixture + "/Probe.mat";
        private static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
        private static readonly Result Report = new();
        [Serializable] private sealed class Result
        {
            public string name = "UPM dependencies and path boundary";
            public string unityVersion;
            public int checks;
            public bool passed;
            public string failure;
            public string packageAssetPath;
            public List<string> evidence = new();
        }
        private static void Check(bool condition, string evidence)
        {
            Report.checks++;
            if (!condition) throw new Exception(evidence);
            Report.evidence.Add(evidence);
        }
        public static void Begin()
        {
            var reports = ValidationEnvironment.Reports("packages");
            Directory.CreateDirectory(reports);
            try
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PackageAsset);
                Check(texture != null, "AssetDatabase loads an actual package Texture2D using its Packages/... path.");
                Report.packageAssetPath = AssetDatabase.GetAssetPath(texture);
                Check(Report.packageAssetPath == PackageAsset, "GetAssetPath returns the logical Packages/... path, not PackageCache's disk path.");
                Directory.CreateDirectory(Fixture);
                AssetDatabase.Refresh();
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (material == null)
                {
                    var shader = Shader.Find("Unlit/Texture");
                    if (shader == null) throw new Exception("Fixture shader unavailable.");
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }
                material.mainTexture = texture;
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
                Check(AssetDatabase.GetDependencies(MaterialPath).Contains(PackageAsset), "GetDependencies reports the referenced package texture as Packages/... .");
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var collector = new ResCollector();
                typeof(ResCollector).GetField("_setting", flags).SetValue(collector, ScriptableObject.CreateInstance<ResCollectorSetting>());
                typeof(ResCollector).GetMethod("CollectBundleInfo", flags).Invoke(collector, new object[] { Fixture });
                var collected = ((Dictionary<string, BundleInfo>)typeof(ResCollector).GetField("_bundleInfoCache", flags).GetValue(collector)).Values.ToArray();
                Check(collected.Length == 1 && collected[0].AssetList.Length == 1 &&
                    !collected.SelectMany(b => b.AssetList).Any(p => p.StartsWith("packages/", StringComparison.OrdinalIgnoreCase)),
                    "Current collector explicitly records the material but filters the package dependency out of its own AssetList.");
                ProbeBundles(texture.width, texture.height);
                ProbeManifest();
                var rootRelativeKey = AssetPath.Resolve(PackageAsset);
                Check(rootRelativeKey == (LiteConst.AssetRootPath + "/" + PackageAsset).ToLowerInvariant(), "Package-looking public input is interpreted under the Assets resource root, never as the real UPM asset.");
                Check(AssetDatabase.LoadAssetAtPath<Texture2D>(rootRelativeKey) == null, "The mapped resource-root counterpart is absent even though the real UPM texture exists.");
                Report.passed = true;
            }
            catch (Exception ex) { Report.failure = ex.ToString(); Debug.LogException(ex); }
            finally
            {
                Report.unityVersion = Application.unityVersion;
                File.WriteAllText(Path.Combine(reports, "results.json"), JsonUtility.ToJson(Report, true));
                EditorApplication.Exit(Report.passed ? 0 : 2);
            }
        }
        private static void ProbeBundles(int width, int height)
        {
            var implicitRoot = Path.Combine(Root, "Bundles", "package-probe-implicit");
            var explicitRoot = Path.Combine(Root, "Bundles", "package-probe-explicit");
            Directory.CreateDirectory(implicitRoot); Directory.CreateDirectory(explicitRoot);
            var options = new ResBuildConfig().Options;
            var materialBuild = new AssetBundleBuild { assetBundleName = "material.ab", assetNames = new[] { MaterialPath } };
            var implicitManifest = BuildPipeline.BuildAssetBundles(implicitRoot, new[] { materialBuild }, options, ValidationEnvironment.BundleTarget);
            Check(implicitManifest != null, "Native implicit-dependency Bundle build succeeds with new default flags.");
            var bundle = AssetBundle.LoadFromFile(Path.Combine(implicitRoot, "material.ab"));
            try
            {
                Check(bundle != null, "Implicit-dependency Bundle loads.");
                var material = bundle.LoadAsset<Material>(MaterialPath.ToLowerInvariant());
                Check(material != null && material.mainTexture != null && material.mainTexture.width == width && material.mainTexture.height == height,
                    "Package texture is embedded and restored as the material's dependency even though absent from the explicit assetNames list.");
            }
            finally { if (bundle != null) bundle.Unload(true); }
            var explicitManifest = BuildPipeline.BuildAssetBundles(explicitRoot, new[]
            {
                materialBuild,
                new AssetBundleBuild { assetBundleName = "package-shared.ab", assetNames = new[] { PackageAsset } }
            }, options, ValidationEnvironment.BundleTarget);
            Check(explicitManifest != null && explicitManifest.GetAllDependencies("material.ab").Contains("package-shared.ab"),
                "Unity can explicitly build a Packages/... asset and report its shared Bundle dependency.");
            AssetBundle shared = null;
            bundle = null;
            try
            {
                shared = AssetBundle.LoadFromFile(Path.Combine(explicitRoot, "package-shared.ab"));
                bundle = AssetBundle.LoadFromFile(Path.Combine(explicitRoot, "material.ab"));
                Check(shared != null && bundle != null, "Both explicitly split native Bundles load.");
                var texture = shared.LoadAsset<Texture2D>(PackageAsset.ToLowerInvariant());
                var material = bundle.LoadAsset<Material>(MaterialPath.ToLowerInvariant());
                Check(texture != null && material != null && material.mainTexture == texture,
                    "Native full Packages/... lookup succeeds and the material refers to the exact shared texture.");
            }
            finally
            {
                if (bundle != null) bundle.Unload(true);
                if (shared != null) shared.Unload(true);
            }
        }
        private static void ProbeManifest()
        {
            var key = PackageAsset.ToLowerInvariant();
            foreach (var bundlePath in new[] { "shared.ab", "packages/com.unity.ai.navigation/gizmos.ab" })
            {
                var source = new VersionPackInfo("test", "windows", false, false,
                    new[] { new BundleInfo(1, bundlePath, new[] { key }, null) });
                var restored = VersionPackInfo.FromBinaryData(source.ToBinaryData());
                Check(restored.BundleList[0].AssetList.Single() == key, "Codec does not infer prefixes from Bundle names: " + bundlePath);
                var rejected = false;
                try { restored.Initialize(); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Current manifest index rejects a real UPM path: " + bundlePath);
            }
        }
    }
}
