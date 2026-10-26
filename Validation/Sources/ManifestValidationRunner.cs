using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class ManifestValidationRunner
    {
        private static readonly Report Results = new();
        private static Case Active;
        private static string Reports => ValidationEnvironment.Reports("manifest");
        [Serializable] private sealed class Report { public string unityVersion; public string fatalError; public List<Case> cases = new(); }
        [Serializable] private sealed class Case { public string name; public bool passed; public int checks; public string failure; public List<string> evidence = new(); }

        public static void Begin()
        {
            Directory.CreateDirectory(Reports);
            try
            {
                Run("01 Full paths retain repeated directory segments", () => RoundTrip("a/b/c.ab", "a/b/c/d.png", "a/b/c/sub/a/b/c/d.png"));
                Run("02 Root and default folder identities remain distinct", () => RoundTrip("default.ab", "config.json", "default/config.json", "default/sub/config.json"));
                Run("03 Project paths are independent of Bundle names", ExternalPaths);
                Run("04 Assets and packages directory names survive verbatim", () => RoundTrip("data.ab", "data/assets.json", "data/assets_icon.png", "data/assetsbackup/icon.png", "data/assets/icon.png", "assets/demo/icon.png", "packages/demo/icon.png"));
                Run("05 Internal complete-path contract rejects relative and UPM input", StrictPaths);
                Run("06 Sparse dependency IDs round trip under all hash and flat modes", Dependencies);
                Run("07 JSON fields unchanged and only dependencies mutate in place", JsonAndBinary);
                Run("08 Unknown dependency paths and IDs fail", InvalidDependencies);
                Run("09 Legacy relative manifests require regeneration", LegacyRelative);
                Run("10 No format field means ambiguous legacy assets paths cannot be detected", LegacyAmbiguity);
                Run("11 Actual collector and build mapping use full identities", CollectorIntegration);
                Run("12 Duplicate full canonical identities are rejected", Duplicates);
                Run("13 Bundle build and load names preserve mode rules", BundleNames);
            }
            catch (Exception ex) { Results.fatalError = ex.ToString(); }
            Results.unityVersion = Application.unityVersion;
            File.WriteAllText(Path.Combine(Reports, "results.json"), JsonUtility.ToJson(Results, true));
            EditorApplication.Exit(Results.fatalError == null && Results.cases.All(c => c.passed) ? 0 : 2);
        }
        private static void Run(string name, Action action)
        {
            Active = new Case { name = name }; Results.cases.Add(Active);
            try { action(); Active.passed = true; } catch (Exception ex) { Active.failure = ex.ToString(); }
            Debug.Log("FULL_PATH_MANIFEST " + (Active.passed ? "PASS " : "FAIL ") + name);
        }
        private static void Check(bool condition, string evidence)
        {
            Active.checks++;
            if (!condition) throw new Exception(evidence);
            Active.evidence.Add(evidence);
        }
        private static string Key(string relative) => (LiteConst.AssetRootPath + "/" + relative).ToLowerInvariant();
        private static BundleInfo Bundle(int id, string path, string[] assets, params string[] deps) => new(id, path, assets, deps) { Hash = "hash" + id, Size = 1000 + id };
        private static VersionPackInfo Pack(params BundleInfo[] bundles) => new("review", "windows", false, false, bundles);
        private static string Decode(byte[] bytes) => Encoding.UTF8.GetString(LiteConst.SecurityMode ? SecurityUtils.DecryptBytes(bytes, LiteConst.SecurityKey) : bytes);
        private static byte[] Encode(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            return LiteConst.SecurityMode ? SecurityUtils.EncryptBytes(bytes, LiteConst.SecurityKey) : bytes;
        }
        private static void RoundTrip(string bundlePath, params string[] relatives)
        {
            var originals = relatives.Select(Key).ToArray();
            var source = Pack(Bundle(11, bundlePath, (string[])originals.Clone()));
            var bytes = source.ToBinaryData();
            Check(source.BundleList[0].AssetList.SequenceEqual(originals), "Writing keeps every full path unchanged.");
            var stored = LitJson.JsonMapper.ToObject<VersionPackInfo>(Decode(bytes));
            Check(stored.BundleList[0].AssetList.SequenceEqual(originals), "Serialized bytes store full paths without removing any directory.");
            var restored = VersionPackInfo.FromBinaryData(bytes);
            Check(restored.BundleList[0].AssetList.SequenceEqual(originals), "Reading keeps every full path unchanged.");
            restored.Initialize();
            foreach (var path in originals)
                Check(restored.TryGetBundleInfoByAssetKey(path, out var info) && info.BundleID == 11, "Full identity is indexed: " + path);
        }
        private static void ExternalPaths()
        {
            foreach (var bundlePath in new[] { "unrelated.ab", "default.ab", "assets/shared.ab", "a/default.ab" })
            {
                var paths = new[] { "assets/shared/material.mat", Key("a/default/icon.png") };
                var restored = VersionPackInfo.FromBinaryData(Pack(Bundle(7, bundlePath, (string[])paths.Clone())).ToBinaryData());
                restored.Initialize();
                Check(restored.BundleList[0].AssetList.SequenceEqual(paths), "No Bundle-name prefix inference: " + bundlePath);
                Check(restored.GetBundleInfoFromAssetPath("Assets/Shared/Material.mat").BundleID == 7, "Explicit root-external path remains accessible.");
            }
        }
        private static void StrictPaths()
        {
            Check(AssetPath.ResolveProjectAssetPath("Assets\\StandaloneAssets\\Data//./A.txt") == Key("data/a.txt"), "Strict input retains slash, dot and case normalization.");
            foreach (var path in new[] { "data/a.txt", "packages/demo/a.txt", "Packages/com.example/a.txt", "Assets", "Assets/folder", "Assets/a.txt/", "Assets/../a.txt" })
            {
                var rejected = false;
                try { AssetPath.ResolveProjectAssetPath(path); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Strict internal input is rejected: " + path);
            }
            Check(AssetPath.Resolve("packages/demo/a.txt") == Key("packages/demo/a.txt"), "Public input still allows ordinary root-relative packages.");
            Check(AssetPath.Resolve("assets/demo/a.txt") != Key("assets/demo/a.txt"), "Public assets-prefix ambiguity remains explicitly deferred.");
        }
        private static VersionPackInfo DependencyPack(bool hash, bool flat) => new("release-1.2", "windows", hash, flat, new[]
        {
            Bundle(7, "ui/main.ab", new[] { Key("ui/main/window.prefab") }, "assets/shared.ab", "default.ab"),
            Bundle(42, "assets/shared.ab", new[] { "assets/shared/material.mat" }, "default.ab"),
            Bundle(105, "default.ab", new[] { Key("config.json"), Key("default/config.json") })
        });
        private static void Dependencies()
        {
            foreach (var hash in new[] { false, true })
            foreach (var flat in new[] { false, true })
            {
                var source = DependencyPack(hash, flat);
                var assets = source.BundleList.SelectMany(b => b.AssetList).ToArray();
                var physical = source.BundleList.Select(source.GetBundleFileLoadPath).ToArray();
                var bytes = source.ToBinaryData();
                Check(source.BundleList[0].DependencyList.SequenceEqual(new[] { "42", "105" }) && source.BundleList[1].DependencyList.SequenceEqual(new[] { "105" }), "Sparse IDs are encoded as existing strings.");
                var raw = LitJson.JsonMapper.ToObject<VersionPackInfo>(Decode(bytes));
                Check(raw.BundleList[0].DependencyList.SequenceEqual(new[] { "42", "105" }), "Actual stored JSON contains ID strings.");
                var restored = VersionPackInfo.FromBinaryData(bytes);
                Check(restored.BundleList[0].DependencyList.SequenceEqual(new[] { "assets/shared.ab", "default.ab" }) && restored.BundleList[1].DependencyList.Single() == "default.ab", "Dependency paths restore exactly.");
                Check(restored.BundleList.SelectMany(b => b.AssetList).SequenceEqual(assets), "Dependency encoding never alters resource paths.");
                Check(restored.BundleList.Select(restored.GetBundleFileLoadPath).SequenceEqual(physical), "Physical Bundle naming unchanged: hash=" + hash + ", flat=" + flat);
                Check(restored.Version == "release-1.2" && restored.Platform == "windows" && restored.HashMode == hash && restored.FlatMode == flat && restored.BundleList.All(b => b.Hash == "hash" + b.BundleID && b.Size == 1000 + b.BundleID), "Version and Bundle metadata survive.");
                restored.Initialize();
                Check(restored.GetBundleInfoFromAssetPath("config.json")?.BundleID == 105 && restored.GetBundleInfoFromAssetPath("default/config.json")?.BundleID == 105, "Both business-relative default resource lookups succeed.");
            }
        }
        private static void BundleNames()
        {
            foreach (var flat in new[] { false, true })
            foreach (var hash in new[] { false, true })
            {
                var bundle = Bundle(17, "ui/icons.ab", new[] { Key("ui/icon.png") });
                bundle.Hash = "abc123";
                var pack = new VersionPackInfo("names", "windows", hash, flat, new[] { bundle });
                var expectedBuild = flat ? "ui_icons.ab" : "ui/icons.ab";
                var expectedLoad = hash ? (flat ? "ui_icons_abc123.ab" : "ui/icons_abc123.ab") : expectedBuild;
                if (LiteConst.SecurityMode && hash && flat) expectedLoad = "abc123.ab";
                Check(pack.GetBundleFileBuildPath(bundle) == expectedBuild, "Exact build name: hash=" + hash + ", flat=" + flat);
                Check(pack.GetBundleFileLoadPath(bundle) == expectedLoad, "Exact load name follows current SecurityMode: " + expectedLoad);
                Check(pack.GetBundleFileLoadPath(bundle) == expectedLoad && bundle.BundlePath == "ui/icons.ab", "Cache lookup leaves logical Bundle identity unchanged.");
                var restored = VersionPackInfo.FromBinaryData(pack.ToBinaryData());
                Check(restored.GetBundleFileBuildPath(restored.BundleList[0]) == expectedBuild && restored.GetBundleFileLoadPath(restored.BundleList[0]) == expectedLoad, "Naming survives manifest round trip.");
            }
            foreach (var ch in "/ \\:*?\"'<>|~#%&@+{}[]")
            {
                var bundle = Bundle(1, "unused.ab", Array.Empty<string>());
                bundle.BundlePath = "prefix" + ch + "suffix.ab";
                var pack = new VersionPackInfo("characters", "windows", false, true, new[] { bundle });
                Check(pack.GetBundleFileBuildPath(bundle) == "prefix_suffix.ab", "Existing flatten character maps to underscore: " + ch);
            }
            var dotted = Bundle(1, "data.v2/icons.ab", Array.Empty<string>());
            Check(new VersionPackInfo("dots", "windows", false, true, new[] { dotted }).GetBundleFileBuildPath(dotted) == "data.v2_icons.ab", "Dots remain intact in flattened names.");
        }

        private static void JsonAndBinary()
        {
            var source = DependencyPack(false, false);
            var assets = source.BundleList[0].AssetList;
            var deps = source.BundleList[0].DependencyList;
            var json = source.ToJsonData();
            Check(ReferenceEquals(assets, source.BundleList[0].AssetList) && assets.Single() == Key("ui/main/window.prefab"), "Asset array and content remain unchanged.");
            Check(ReferenceEquals(deps, source.BundleList[0].DependencyList) && deps[0] == "42", "Dependency conversion remains in place.");
            Check(json == Decode(DependencyPack(false, false).ToBinaryData()), "JSON and byte entry points encode the same data from fresh objects.");
            var root = LitJson.JsonMapper.ToObject(json);
            Check(root.Keys.Cast<string>().OrderBy(k => k).SequenceEqual(new[] { "IsValid", "Version", "Platform", "HashMode", "FlatMode", "BundleList" }.OrderBy(k => k)), "No format-version or other JSON field was introduced.");
        }
        private static void InvalidDependencies()
        {
            var rejected = false;
            try { Pack(Bundle(1, "ui.ab", new[] { Key("ui/icon.png") }, "missing.ab")).ToBinaryData(); } catch (KeyNotFoundException) { rejected = true; }
            Check(rejected, "Unknown dependency path fails during writing.");
            rejected = false;
            try { VersionPackInfo.FromBinaryData(Encode(LitJson.JsonMapper.ToJson(Pack(Bundle(1, "ui.ab", new[] { Key("ui/icon.png") }, "999"))))); } catch (KeyNotFoundException) { rejected = true; }
            Check(rejected, "Unknown dependency ID fails during reading.");
        }
        // Fixed wire-format input, deliberately independent of any historical implementation.
        private static VersionPackInfo ReadRelativeFixture(string storedPath)
        {
            var json = "{\"IsValid\":true,\"Version\":\"fixture\",\"Platform\":\"windows\",\"HashMode\":false,\"FlatMode\":false,\"BundleList\":[{\"BundleID\":7,\"BundlePath\":\"data.ab\",\"Hash\":\"\",\"Size\":0,\"AssetList\":[\"" + storedPath + "\"],\"DependencyList\":[]}]}";
            return VersionPackInfo.FromBinaryData(Encode(json));
        }
        private static void LegacyRelative()
        {
            var restored = ReadRelativeFixture("probe.txt");
            Check(restored.BundleList[0].AssetList.Single() == "probe.txt", "New reader deliberately does not restore old relative paths.");
            var rejected = false;
            try { restored.Initialize(); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Ordinary legacy relative content is rejected when indexing; regenerate from sources.");
        }
        private static void LegacyAmbiguity()
        {
            var restored = ReadRelativeFixture("assets/icon.png");
            restored.Initialize();
            Check(restored.BundleList[0].AssetList.Single() == "assets/icon.png", "An ambiguous old token can look like a full project path.");
            Check(!restored.TryGetBundleInfoByAssetKey(Key("data/assets/icon.png"), out _) && restored.TryGetBundleInfoByAssetKey("assets/icon.png", out _), "Without a format marker, parse success cannot establish legacy compatibility (documented limitation).");
        }
        private static void CollectorIntegration()
        {
            var files = new[] { LiteConst.AssetRootPath + "/__FullManifestCollect.txt", LiteConst.AssetRootPath + "/default/__FullManifestCollect.txt", LiteConst.AssetRootPath + "/assets/demo/__FullManifestCollect.txt" };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                foreach (var path in files) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, path); }
                AssetDatabase.Refresh();
                var collector = new ResCollector();
                typeof(ResCollector).GetField("_setting", flags).SetValue(collector, ScriptableObject.CreateInstance<ResCollectorSetting>());
                var create = typeof(ResCollector).GetMethod("CreateBundleInfo", flags, null, new[] { typeof(string), typeof(string[]) }, null);
                foreach (var path in files) create.Invoke(collector, new object[] { Path.GetDirectoryName(path).Replace('\\', '/'), new[] { path } });
                var bundles = ((Dictionary<string, BundleInfo>)typeof(ResCollector).GetField("_bundleInfoCache", flags).GetValue(collector)).Values.ToArray();
                Check(bundles.SelectMany(b => b.AssetList).OrderBy(p => p).SequenceEqual(files.Select(p => p.ToLowerInvariant()).OrderBy(p => p)), "Actual collection stores all complete canonical identities.");
                Check(bundles.Single(b => b.BundlePath == "default.ab").AssetList.Length == 2, "Existing root/default grouping remains intact.");
                foreach (var path in files) Check(collector.GetAssetSourcePath(path.ToLowerInvariant()) == path, "Build source mapping retains casing: " + path);
                var restored = VersionPackInfo.FromBinaryData(Pack(bundles).ToBinaryData());
                restored.Initialize();
                foreach (var path in files) Check(restored.GetBundleInfoFromAssetPath(path) != null, "Collected full path remains queryable after storage: " + path);
            }
            finally { foreach (var path in files) AssetDatabase.DeleteAsset(path); }
        }
        private static void Duplicates()
        {
            var rejected = false;
            try { Pack(Bundle(1, "a.ab", new[] { Key("a.txt"), Key("a.txt").ToUpperInvariant() })).Initialize(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Full-path aliases cannot introduce duplicate resource identities.");
        }
    }
}
