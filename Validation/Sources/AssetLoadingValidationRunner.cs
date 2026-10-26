using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    // Local validation only. Runtime sources are the unmodified working-tree snapshot.
    internal static class AssetLoadingValidationRunner
    {
        private const string RelativeRoot = "__pathvalidation20261022";
        private const string Fixture = "Assets/StandaloneAssets/" + RelativeRoot;
        private const string Outside = "Assets/__pathvalidation20261022outside/Shared.txt";
        private const string TextA = RelativeRoot + "/Text/A/Same.txt";
        private const string TextB = RelativeRoot + "/Text/B/Same.txt";
        private const string PackagesTextPath = "packages/demo/__PackagesContractProbe20261023.txt";
        private static readonly string[] SpecialTexts = {
            "data/assets/__FullPathManifestProbe.txt", "assets/demo/__FullPathManifestProbe.txt",
            "__FullPathManifestRoot.txt", "default/__FullPathManifestRoot.txt",
            "__PathUtilsDotted.v2/__FullPathManifestProbe.txt"
        };
        private const string UpmTexturePath = "Packages/com.unity.ai.navigation/Gizmos/NavMeshSurface Icon.png";
        private const string ProbePath = RelativeRoot + "/Text/UniqueLookupProbe.txt";
        private const string ProbeContent = "unique-lookup-probe";
        private const string ControlBundle = "probe-control.ab";
        private const BuildAssetBundleOptions FilenameFlags =
            BuildAssetBundleOptions.DisableLoadAssetByFileName |
            BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
        private const string PrefabA = RelativeRoot + "/Prefabs/A/Same.prefab";
        private const string PrefabB = RelativeRoot + "/Prefabs/B/Same.prefab";
        private const string SceneA = RelativeRoot + "/Scenes/A/Battle.unity";
        private const string SceneB = RelativeRoot + "/Scenes/B/Battle.unity";
        private const string SceneC = RelativeRoot + "/Scenes/C/OtherBattle.unity";
        private const string TextBundle = RelativeRoot + "/text.ab";
        private const string PrefabBundle = RelativeRoot + "/prefabs.ab";
        private const string SceneBundle = RelativeRoot + "/scenes/a.ab";
        private const string SceneBundleB = RelativeRoot + "/scenes/b.ab";
        private const string SceneBundleC = RelativeRoot + "/scenes/c.ab";
        private const string Pending = "LiteQuark.AssetLoadingValidation.Pending";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
        private static string Reports => ValidationEnvironment.Reports("loading");
        private static string Bundles => Path.Combine(Root, "Bundles", "loading");
        private static string ControlBundles => Path.Combine(Root, "Bundles", "loading-control");
        private static readonly Result Report = new();
        private static readonly List<string> Errors = new();
        private static Case Active;
        private static EnvironmentContext Context;
        private static bool Running;
        private const string SceneCollisionId = "UNITY-SAME-NAME-SCENE-BUNDLES";
        private const string NativeCollisionCase = "17 Native same-name Bundle collision diagnostic";

        private sealed class KnownSceneCollisionException : Exception
        {
            internal KnownSceneCollisionException() : base("Unity rejects concurrent Bundles containing same-name scenes; confirmed separately by the native diagnostic.") { }
        }

        [Serializable] private sealed class Result
        {
            public string unityVersion;
            public string finishedUtc;
            public string fatalError;
            public List<Case> cases = new();
        }
        [Serializable] private sealed class Case
        {
            public string name;
            public bool passed;
            public string status;
            public string limitationId;
            public bool diagnostic;
            public int assertions;
            public string failure;
            public List<string> evidence = new();
            public List<string> errors = new();
        }

        public static void Begin()
        {
            try
            {
                Directory.CreateDirectory(Reports);
                BuildFixtures();
                SessionState.SetBool(Pending, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception ex)
            {
                Report.fatalError = ex.ToString();
                Save();
                Debug.LogException(ex);
                EditorApplication.Exit(2);
            }
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode) Start();
            };
            EditorApplication.delayCall += Start;
        }

        private static void Start()
        {
            if (Running || !EditorApplication.isPlaying || !SessionState.GetBool(Pending, false)) return;
            Running = true;
            SessionState.SetBool(Pending, false);
            RunAll().Forget();
        }

        private static string ProjectPath(string input) => input.StartsWith("Assets/", StringComparison.Ordinal) ? input : "Assets/StandaloneAssets/" + input;
        private static void BuildFixtures()
        {
            foreach (var folder in new[] { "Text/A", "Text/B", "Prefabs/A", "Prefabs/B", "Scenes/A", "Scenes/B", "Scenes/C" })
                Directory.CreateDirectory(Fixture + "/" + folder);
            Directory.CreateDirectory(Path.GetDirectoryName(Outside));
            Directory.CreateDirectory(Bundles);
            Directory.CreateDirectory(ControlBundles);
            File.WriteAllText(ProjectPath(TextA), "text-A");
            File.WriteAllText(ProjectPath(TextB), "text-B");
            Directory.CreateDirectory(Path.GetDirectoryName(ProjectPath(PackagesTextPath)));
            File.WriteAllText(ProjectPath(PackagesTextPath), "ordinary-packages-folder");
            foreach (var relative in SpecialTexts)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ProjectPath(relative)));
                File.WriteAllText(ProjectPath(relative), "full-path:" + relative);
            }
            File.WriteAllText(ProjectPath(ProbePath), ProbeContent);
            File.WriteAllText(Outside, "outside-root");
            AssetDatabase.Refresh();
            foreach (var pair in new[] { (PrefabA, "prefab-A"), (PrefabB, "prefab-B") })
            {
                var go = new GameObject(pair.Item2);
                new GameObject(pair.Item2).transform.SetParent(go.transform);
                PrefabUtility.SaveAsPrefabAsset(go, ProjectPath(pair.Item1));
                UnityEngine.Object.DestroyImmediate(go);
            }
            foreach (var pair in new[] { (SceneA, "marker-A"), (SceneB, "marker-B"), (SceneC, "marker-C") })
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.skybox = null;
                new GameObject(pair.Item2);
                EditorSceneManager.SaveScene(scene, ProjectPath(pair.Item1));
            }
            var host = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(host, Fixture + "/Host.unity");
            AssetDatabase.SaveAssets();
            var builds = new[]
            {
                new AssetBundleBuild { assetBundleName = TextBundle, assetNames = new[] { ProjectPath(TextA), ProjectPath(TextB), Outside, ProjectPath(ProbePath) } },
                new AssetBundleBuild { assetBundleName = PrefabBundle, assetNames = new[] { ProjectPath(PrefabA), ProjectPath(PrefabB) } },
                new AssetBundleBuild { assetBundleName = SceneBundle, assetNames = new[] { ProjectPath(SceneA) } },
                new AssetBundleBuild { assetBundleName = SceneBundleB, assetNames = new[] { ProjectPath(SceneB) } },
                new AssetBundleBuild { assetBundleName = SceneBundleC, assetNames = new[] { ProjectPath(SceneC) } }
            };
            // Use the actual collector and build mapping for the ordinary packages folder.
            var packageCollector = new ResCollector();
            Set(packageCollector, "_setting", ScriptableObject.CreateInstance<ResCollectorSetting>());
            typeof(ResCollector).GetMethod("CreateBundleInfo", Private, null, new[] { typeof(string), typeof(string[]) }, null)
                .Invoke(packageCollector, new object[] { Path.GetDirectoryName(ProjectPath(PackagesTextPath)).Replace('\\', '/'), new[] { ProjectPath(PackagesTextPath) } });
            foreach (var relative in SpecialTexts)
                typeof(ResCollector).GetMethod("CreateBundleInfo", Private, null, new[] { typeof(string), typeof(string[]) }, null)
                    .Invoke(packageCollector, new object[] { Path.GetDirectoryName(ProjectPath(relative)).Replace('\\', '/'), new[] { ProjectPath(relative) } });
            var packageBundles = Get<Dictionary<string, BundleInfo>>(packageCollector, "_bundleInfoCache").Values.ToArray();
            for (var i = 0; i < packageBundles.Length; i++) packageBundles[i].BundleID = 6 + i;
            var packagePack = new VersionPackInfo("package-probe", "windows", false, false, packageBundles);
            var packageBuilder = new ProjectBuilder();
            typeof(ProjectBuilder).GetProperty("Collector").SetValue(packageBuilder, packageCollector);
            typeof(ResCollectInfoStep).GetMethod("CollectAssetBundleBuilds", Private)
                .Invoke(new ResCollectInfoStep(), new object[] { packageBuilder, packagePack });
            if (!packageBundles.Any(b => b.AssetList.Contains(ProjectPath(PackagesTextPath).ToLowerInvariant())) ||
                !packageCollector.Builds.SelectMany(b => b.assetNames).Contains(ProjectPath(PackagesTextPath)))
                throw new Exception("Unexpected packages collection or source mapping.");
            builds = builds.Concat(packageCollector.Builds).ToArray();
            var buildOptions = new ResBuildConfig().Options;
            var manifest = BuildPipeline.BuildAssetBundles(Bundles, builds, buildOptions, ValidationEnvironment.BundleTarget);
            if (manifest == null) throw new Exception("Fixture Bundle build failed.");
            foreach (var build in builds)
                if (!File.Exists(Path.Combine(Bundles, build.assetBundleName)))
                    throw new Exception("Fixture Bundle was not produced: " + build.assetBundleName);
            var controlOptions = buildOptions & ~FilenameFlags;
            var controlManifest = BuildPipeline.BuildAssetBundles(ControlBundles,
                new[] { new AssetBundleBuild { assetBundleName = ControlBundle, assetNames = new[] { ProjectPath(ProbePath) } } },
                controlOptions, ValidationEnvironment.BundleTarget);
            if (controlManifest == null || !File.Exists(Path.Combine(ControlBundles, ControlBundle)))
                throw new Exception("Control Bundle build failed.");
            File.WriteAllText(Path.Combine(Reports, "effective-build-options.json"), JsonUtility.ToJson(new BuildOptionEvidence
            {
                mainOptions = buildOptions.ToString(), mainValue = (int)buildOptions,
                controlOptions = controlOptions.ToString(), controlValue = (int)controlOptions
            }, true));
            var pack = new VersionPackInfo("path-validation", "windows", false, false, new[]
            {
                new BundleInfo(1, TextBundle, new[] { ProjectPath(TextA).ToLowerInvariant(), ProjectPath(TextB).ToLowerInvariant(), Outside.ToLowerInvariant(), ProjectPath(ProbePath).ToLowerInvariant() }, null),
                new BundleInfo(2, PrefabBundle, new[] { ProjectPath(PrefabA).ToLowerInvariant(), ProjectPath(PrefabB).ToLowerInvariant() }, null),
                new BundleInfo(3, SceneBundle, new[] { ProjectPath(SceneA).ToLowerInvariant() }, null),
                new BundleInfo(4, SceneBundleB, new[] { ProjectPath(SceneB).ToLowerInvariant() }, null),
                new BundleInfo(5, SceneBundleC, new[] { ProjectPath(SceneC).ToLowerInvariant() }, null)
            }.Concat(packageBundles).ToArray());
            File.WriteAllBytes(Path.Combine(Reports, "version.bytes"), pack.ToBinaryData());
            File.WriteAllText(Path.Combine(Reports, "fixture-assets.json"), JsonUtility.ToJson(new FixturePaths { assets = builds.SelectMany(b => b.assetNames).ToArray() }, true));
        }
        [Serializable] private sealed class FixturePaths { public string[] assets; }
        [Serializable] private sealed class BuildOptionEvidence
        {
            public string mainOptions;
            public int mainValue;
            public string controlOptions;
            public int controlValue;
        }

        private static async UniTask RunAll()
        {
            Application.logMessageReceived += Capture;
            try
            {
                new GameObject("AssetPathValidationLauncher").AddComponent<LiteLauncher>();
                await Wait(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Running, "Runtime startup");
                await Run("01 Path normalization and invalid input", PathRules);
                await Run("02 Full-path version file and public lookup", Manifest);
                await Run("03 Bundle same-name assets and independent instances", BundleNames);
                await Run("04 Bundle aliases, typed cache and retention", BundleAliases);
                await Run("05 Preload deduplication and failed canonical paths", Preload);
                await Run("06 Invalid requests start no Bundle work", InvalidRequests);
                await Run("07 Root-external resource explicit path", OutsideRoot);
                await Run("08 Bundle same-name scenes in different folders", () => DifferentScenes(false));
                await Run("09 Bundle alias guard and sequential instances", () => SceneInstances(false), "Scene is already loading");
                await Run("10 Bundle scene cancellation and release", SceneCancellation);
                await Run("11 Editor assets and path aliases", EditorAssets);
                await Run("12 Editor same-name scenes in different folders", () => DifferentScenes(true));
                await Run("13 Editor alias guard and sequential instances", () => SceneInstances(true), "Scene is already loading");
                await Run("14 Editor delayed scene delivery", () => DifferentScenes(true, true));
                await Run("15 Scene lookup failure does not poison retry", MissingScene);
                await Run("16 Bundle distinct-name scenes load concurrently", () => DifferentScenes(false, false, true));
                await Run(NativeCollisionCase, NativeCollision);
                await Run("18 New configuration enables both filename flags", DefaultOptions);
                await Run("19 Synchronous full-path versus filename lookup with control", () => LookupProbe(false));
                await Run("20 Asynchronous full-path versus filename lookup with control", () => LookupProbe(true));
                await Run("21 Collected packages path loads through Bundle public API", () => PackagesFolderLoad(false));
                await Run("22 Packages aliases load through Editor public API", () => PackagesFolderLoad(true));
                await Run("23 Existing UPM texture is not a Bundle fallback", () => UpmNotSupported(false));
                await Run("24 Existing UPM texture is not an Editor fallback", () => UpmNotSupported(true), "Can't load asset");
                await Run("25 Full manifest paths load assets directories and default resources", FullManifestResources);
                await Run("26 BuiltIn locator preserves override and persistent priority", BuiltInLocation);
                await Run("27 Remote URI configuration and version query composition", RemoteUriComposition);
            }
            catch (Exception ex) { Report.fatalError = ex.ToString(); }
            finally
            {
                Application.logMessageReceived -= Capture;
                var diagnostic = Report.cases.SingleOrDefault(c => c.name == NativeCollisionCase);
                foreach (var item in Report.cases.Where(c => c.status == "pending_known_limitation"))
                {
                    if (diagnostic != null && diagnostic.passed)
                    {
                        item.status = "known_limitation";
                        item.limitationId = SceneCollisionId;
                        item.evidence.Add("Known limitation confirmed by native loads in both orders; excluded from regression pass/fail counts.");
                    }
                    else
                    {
                        item.status = "failed";
                        item.failure += "\nNative diagnostic did not confirm the known limitation.";
                    }
                }
                Save();
                EditorApplication.Exit(Report.fatalError == null && Report.cases.All(c => c.passed || c.status == "known_limitation") ? 0 : 2);
            }
        }

        private static async UniTask Run(string name, Func<UniTask> test, params string[] expectedErrors)
        {
            Active = new Case { name = name, diagnostic = name == NativeCollisionCase };
            Report.cases.Add(Active);
            Errors.Clear();
            try { await test(); Active.passed = true; }
            catch (KnownSceneCollisionException ex) { Active.status = "pending_known_limitation"; Active.failure = ex.ToString(); }
            catch (Exception ex) { Active.failure = ex.ToString(); }
            finally
            {
                var collisionObserved = Active.status == "pending_known_limitation";
                try { await Cleanup(); }
                catch (Exception ex) { Active.passed = false; Active.status = "failed"; Active.failure += "\nCleanup: " + ex; }
                Active.errors.AddRange(Errors);
                var unexpected = Errors.Where(e => Active.diagnostic ? !IsNativeSceneCollisionLog(e) :
                    collisionObserved ? !IsSceneCollisionLog(e) : !expectedErrors.Any(e.Contains)).ToArray();
                if (unexpected.Length > 0) { Active.passed = false; Active.status = "failed"; Active.failure += "\nUnexpected: " + string.Join("\n", unexpected); }
                if (Active.status != "pending_known_limitation")
                    Active.status = Active.passed ? (Active.diagnostic ? "diagnostic_passed" : "passed") : "failed";
                Debug.Log("ASSET_PATH_VALIDATION " + Active.status + " " + name);
                Save();
                Active = null;
            }
        }
        private static void Capture(string message, string stack, LogType type)
        {
            if (Active != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Errors.Add(message);
        }
        private static void Check(bool condition, string evidence)
        {
            Active.assertions++;
            if (!condition) throw new Exception(evidence);
            Active.evidence.Add(evidence);
        }
        private static void Throws(Action action, string evidence)
        {
            try { action(); }
            catch (ArgumentException) { Check(true, evidence); return; }
            Check(false, evidence);
        }
        private static void Save()
        {
            Directory.CreateDirectory(Reports);
            Report.unityVersion = Application.unityVersion;
            Report.finishedUtc = DateTime.UtcNow.ToString("O");
            File.WriteAllText(Path.Combine(Reports, "results.json"), JsonUtility.ToJson(Report, true));
        }
        private static async UniTask Frame()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            Context?.Provider?.Tick(0);
        }
        private static async UniTask Wait(Func<bool> condition, string label)
        {
            var deadline = Time.realtimeSinceStartup + 20;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(label);
                await Frame();
            }
        }
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static VersionPackInfo ReadPack()
        {
            var pack = VersionPackInfo.FromBinaryData(File.ReadAllBytes(Path.Combine(Reports, "version.bytes")));
            pack.Initialize();
            return pack;
        }

        private static UniTask RemoteUriComposition()
        {
            var versionQuery = "t=" + Uri.EscapeDataString(AppUtils.GetVersion());
            var compose = typeof(BundleRemoteLocater).GetMethod("GetVersionPackUri", Private);
            foreach (var item in new[] {
                (Base: "https://cdn.example/res", Query: versionQuery, Fragment: ""),
                (Base: "https://cdn.example/res?token=a%2Bb#part", Query: "token=a%2Bb&" + versionQuery, Fragment: "#part"),
                (Base: "https://cdn.example/res?token=x&", Query: "token=x&" + versionQuery, Fragment: ""),
                (Base: "https://cdn.example/res?", Query: versionQuery, Fragment: ""),
                (Base: "https://cdn.example/res#part?note=1", Query: versionQuery, Fragment: "#part?note=1"),
                (Base: "https://cdn.example/res??flag#part", Query: "?flag&" + versionQuery, Fragment: "#part") })
            {
                var locator = new BundleRemoteLocater(item.Base);
                var result = (string)compose.Invoke(locator, new object[] { "version #?.txt" });
                var expected = "https://cdn.example/res/version%20%23%3F.txt?" + item.Query + item.Fragment;
                Check(result == expected, "Version URI expected=[" + expected + "]; actual=[" + result + "]");
                var parsed = new Uri(result);
                Check(parsed.Query == "?" + item.Query && parsed.Fragment == item.Fragment, "Cache query is inserted before the fragment without losing original query data.");
            }

            var setting = LiteRuntime.Setting.Asset;
            var previousMode = setting.BundleLocater;
            var previousUri = setting.BundleRemoteUri;
            var previousCache = setting.DisableUnityWebCache;
            var provider = new AssetBundleProvider();
            try
            {
                setting.BundleLocater = BundleLocaterMode.Remote;
                setting.BundleRemoteUri = "https://cdn.example/base%20dir?token=a%2Bb#part";
                setting.DisableUnityWebCache = true;
                var locator = (BundleRemoteLocater)typeof(AssetBundleProvider).GetMethod("CreateLocater", Private).Invoke(provider, null);
                var platform = Uri.EscapeDataString(AppUtils.GetCurrentPlatformName().ToLowerInvariant());
                var version = Uri.EscapeDataString(AppUtils.GetMainVersion());
                var expectedBase = "https://cdn.example/base%20dir/" + platform + "/" + version;
                Check(Get<string>(locator, "_remoteUri") == expectedBase + "?token=a%2Bb#part", "Actual provider appends platform and main version as URI directory segments.");
                Check(locator.DisableUnityWebCache, "Remote cache option is preserved.");
                Check((string)compose.Invoke(locator, new object[] { "version.txt" }) == expectedBase + "/version.txt?token=a%2Bb&" + versionQuery + "#part", "Provider-to-Locator version address composition retains all suffix data.");
                Check(PathUtils.ConcatUri(Get<string>(locator, "_remoteUri"), "ui/my icon.ab") == expectedBase + "/ui/my%20icon.ab?token=a%2Bb#part", "Bundle URI uses the same configured base with an escaped resource filename.");
            }
            finally
            {
                provider.Dispose();
                setting.BundleLocater = previousMode;
                setting.BundleRemoteUri = previousUri;
                setting.DisableUnityWebCache = previousCache;
            }
            Active.evidence.Add("No version or remote Bundle task was started; all remote URI assertions are offline.");
            return UniTask.CompletedTask;
        }

        private static async UniTask BuiltInLocation()
        {
            var leaf = "__sessionvalidationlocator_" + Guid.NewGuid().ToString("N");
            var persistentBase = Path.GetFullPath(Path.Combine(Application.persistentDataPath, LiteConst.Tag));
            var streamingBase = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, LiteConst.Tag));
            var persistent = Path.Combine(persistentBase, leaf);
            var streaming = Path.Combine(streamingBase, leaf);
            foreach (var folder in new[] { persistent, streaming })
                if (Directory.Exists(folder) || File.Exists(folder) || File.Exists(folder + ".meta"))
                    throw new InvalidOperationException("Locator fixture already exists: " + folder);
            var relativeBundle = leaf + "/probe.ab";
            var relativeVersion = leaf + "/version.txt";
            var locator = new BundleBuiltInLocater();
            try
            {
                Directory.CreateDirectory(persistent);
                Directory.CreateDirectory(streaming);
                File.Copy(Path.Combine(Bundles, TextBundle), Path.Combine(persistent, "probe.ab"));
                File.Copy(Path.Combine(ControlBundles, ControlBundle), Path.Combine(streaming, "probe.ab"));
                foreach (var item in new[] { (Folder: persistent, Marker: "persistent-marker"), (Folder: streaming, Marker: "streaming-marker") })
                {
                    var pack = new VersionPackInfo(item.Marker, "windows", false, false, new[] {
                        new BundleInfo(1, relativeBundle, new[] { AssetPath.ResolveProjectAssetPath(ProjectPath(ProbePath)) }, null)
                    });
                    File.WriteAllBytes(Path.Combine(item.Folder, "version.txt"), pack.ToBinaryData());
                }
                locator.EditorForceStreamingAssets = true;
                await AssertBuiltInLocation(locator, relativeVersion, relativeBundle, "streaming-marker", false);
                locator.EditorForceStreamingAssets = false;
                await AssertBuiltInLocation(locator, relativeVersion, relativeBundle, "persistent-marker", true);
                File.Delete(Path.Combine(persistent, "version.txt"));
                File.Delete(Path.Combine(persistent, "probe.ab"));
                await AssertBuiltInLocation(locator, relativeVersion, relativeBundle, "streaming-marker", false);

                var resolve = typeof(BundleBuiltInLocater).GetMethod("GetRuntimeFullPath", Private);
                File.WriteAllText(Path.Combine(persistent, "probe.ab"), "intentionally-not-a-bundle");
                Check((string)resolve.Invoke(locator, new object[] { relativeBundle }) == PathUtils.GetPersistentDataPath(LiteConst.Tag, relativeBundle), "Selection remains existence-based; it does not inspect or silently bypass corrupt persistent content.");
                File.Delete(Path.Combine(persistent, "probe.ab"));
                File.Delete(Path.Combine(streaming, "probe.ab"));
                Check((string)resolve.Invoke(locator, new object[] { relativeBundle }) == PathUtils.GetStreamingAssetsPath(LiteConst.Tag, relativeBundle), "A missing persistent file selects StreamingAssets without requiring that fallback file to exist.");
            }
            finally
            {
                DeleteLocatorFixture(persistent, persistentBase, leaf);
                DeleteLocatorFixture(streaming, streamingBase, leaf);
            }
            Check(!Directory.Exists(persistent) && !Directory.Exists(streaming), "Only this run's unique locator fixtures are removed.");
        }

        private static async UniTask AssertBuiltInLocation(BundleBuiltInLocater locator, string versionPath, string bundlePath, string marker, bool persistent)
        {
            VersionPackInfo callbackPack = null;
            var versionTask = locator.LoadVersionPack(versionPath, pack => callbackPack = pack);
            await Wait(() => versionTask.IsDone, "BuiltIn version load");
            var loadedPack = versionTask.Result as VersionPackInfo;
            Check(loadedPack != null && loadedPack.Version == marker && ReferenceEquals(loadedPack, callbackPack), "Public version load selects " + marker + ", forceStreaming=" + locator.EditorForceStreamingAssets);
            Check(loadedPack.GetBundleInfoFromAssetPath(ProbePath)?.BundlePath == bundlePath, "Selected version file builds the full-path resource index.");
            AssetBundle callbackBundle = null;
            var bundleTask = locator.LoadBundle(bundlePath, string.Empty, bundle => callbackBundle = bundle);
            AssetBundle loadedBundle = null;
            try
            {
                await Wait(() => bundleTask.IsDone, "BuiltIn Bundle load");
                loadedBundle = bundleTask.GetBundle();
                Check(loadedBundle != null && loadedBundle == callbackBundle, "Public Bundle load completes from the same location policy.");
                Check(loadedBundle.GetAllAssetNames().Contains(AssetPath.Resolve(TextA)) == persistent, "Different Bundle contents confirm the chosen location is " + marker);
                var asset = loadedBundle.LoadAsset<TextAsset>(AssetPath.Resolve(ProbePath));
                Check(asset != null && asset.text == ProbeContent, "Selected BuiltIn Bundle supports complete-path asset loading.");
            }
            finally
            {
                if (loadedBundle != null) loadedBundle.Unload(true);
            }
        }

        private static void DeleteLocatorFixture(string folder, string basePath, string leaf)
        {
            var full = Path.GetFullPath(folder);
            var prefix = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(full) != leaf || !leaf.StartsWith("__sessionvalidationlocator_", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected locator fixture cleanup target.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
            if (File.Exists(full + ".meta")) File.Delete(full + ".meta");
        }

        private sealed class EnvironmentContext
        {
            public readonly AssetSystem Assets = new();
            public readonly List<IDisposable> Handles = new();
            public AssetBundleProvider Provider;
            public LocalLocator Locator;
            public IAssetProvider ActualProvider;
            public T Keep<T>(T value) where T : IDisposable { Handles.Add(value); return value; }
            public AssetHandle<T> Asset<T>(string path) where T : UnityEngine.Object => Keep(Assets.LoadAssetHandle<T>(path));
            public SceneHandle Scene(string path) => Keep(Assets.LoadSceneHandle(path, new LoadSceneParameters(LoadSceneMode.Additive)));
            public int Loading => Get<HashSet<string>>(ActualProvider, "_loadingSceneSet").Count;
        }
        private sealed class LocalLocator : IBundleLocater
        {
            public int Calls;
            public LoadVersionPackTask LoadVersionPack(string name, Action<VersionPackInfo> callback) => throw new NotSupportedException();
            public ILoadBundleTask LoadBundle(string path, string hash, Action<AssetBundle> callback)
            {
                Calls++;
                return LiteRuntime.Task.AddLoadLocalBundleTask(Path.Combine(Bundles, path), callback);
            }
        }
        private static async UniTask<EnvironmentContext> New(bool editor = false, bool delayed = false)
        {
            Context = new EnvironmentContext();
            LiteRuntime.Setting.Asset.EnableRetain = true;
            LiteRuntime.Setting.Asset.BundleRetainTime = 300;
            if (editor)
            {
                LiteRuntime.Setting.Asset.SimulateAsyncDelayInEditor = delayed;
                LiteRuntime.Setting.Asset.AsyncDelayMinFrame = 5;
                LiteRuntime.Setting.Asset.AsyncDelayMaxFrame = 6;
                var provider = new AssetEditorProvider();
                await provider.Initialize();
                Context.ActualProvider = provider;
            }
            else
            {
                Context.Provider = new AssetBundleProvider();
                Context.Locator = new LocalLocator();
                var pack = ReadPack();
                Set(Context.Provider, "_packInfo", pack);
                Set(Context.Provider, "_bundleLoader", new AssetBundleLoader(Context.Locator, pack, 5));
                Context.ActualProvider = Context.Provider;
            }
            Set(Context.Assets, "_provider", Context.ActualProvider);
            return Context;
        }
        private static Scene[] FixtureScenes() => Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
            .Where(s => s.path.IndexOf(RelativeRoot + "/Scenes/", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        private static async UniTask Cleanup()
        {
            if (Context == null) return;
            foreach (var handle in Context.Handles) handle.Dispose();
            try
            {
                await Wait(() => Context.Loading == 0, "Pending scene requests settle");
                for (var i = 0; i < 8; i++) await Frame();
                await Wait(() => FixtureScenes().Length == 0, "Handle-owned scene cleanup");
                Check(true, "All fixture scenes unloaded through production ownership.");
            }
            finally
            {
                foreach (var scene in FixtureScenes())
                {
                    Active.passed = false;
                    Active.evidence.Add("Harness-only cleanup: " + scene.path);
                    await SceneManager.UnloadSceneAsync(scene).ToUniTask();
                }
                Context.Assets.Dispose();
                Context = null;
                LiteRuntime.Setting.Asset.SimulateAsyncDelayInEditor = false;
                await UniTask.Yield();
            }
        }

        private static UniTask PathRules()
        {
            var expected = AssetPath.Resolve(TextA);
            foreach (var path in new[] { TextA, ProjectPath(TextA), ProjectPath(TextA).ToUpperInvariant(), TextA.Replace('/', '\\'), "./" + TextA.Replace("/", "//") })
                Check(AssetPath.Resolve(path) == expected, "Equivalent input: " + path);
            foreach (var path in new[] { null, "", " ", "../a.txt", "a/../b.txt", "C:/a.txt", "/Assets/a.txt", "https://host/a.txt", "Packages", "a", "a.txt/", "a.txt/.", "a?.txt", "a\nb.txt", "\\\\server\\a.txt" })
            {
                Check(!AssetPath.TryResolve(path, out _), "Reject invalid path: " + (path ?? "<null>"));
                Throws(() => AssetPath.Resolve(path), "Resolve throws for invalid input.");
            }
            Check(AssetPath.Resolve("AssetsBackup/a.txt") == "assets/standaloneassets/assetsbackup/a.txt", "Assets prefix respects directory boundary.");
            Check(AssetPath.Resolve("Packages/a.txt") == "assets/standaloneassets/packages/a.txt", "Packages is an ordinary resource-root-relative directory, not a UPM entry point.");
            Check(AssetPath.Resolve(Outside) == Outside.ToLowerInvariant(), "Explicit external path is preserved.");
            Check(AssetPath.TryGetRelativeToRoot(ProjectPath(TextA), "Assets/StandaloneAssets", out var relative) && relative == TextA, "Relative extraction preserves case.");
            Check(!AssetPath.TryGetRelativeToRoot("Assets/StandaloneAssetsBackup/a.txt", "Assets/StandaloneAssets", out _), "Root prefix respects directory boundary.");
            Check(!AssetPath.TryGetRelativeToRoot("Assets/StandaloneAssets", "Assets/StandaloneAssets", out _), "Root itself is not a child.");
            Throws(() => AssetPath.ResolveScene(TextA), "Scene extension is required.");
            return UniTask.CompletedTask;
        }
        private static UniTask Manifest()
        {
            var pack = VersionPackInfo.FromBinaryData(File.ReadAllBytes(Path.Combine(Reports, "version.bytes")));
            var before = pack.BundleList.SelectMany(b => b.AssetList).ToArray();
            pack.Initialize();
            Check(before.SequenceEqual(pack.BundleList.SelectMany(b => b.AssetList)), "Initialize does not rewrite stored AssetList.");
            var expected = pack.GetBundleInfoFromAssetPath(TextA);
            Check(expected != null && expected.BundlePath == TextBundle, "Business relative lookup from full-path manifest succeeds.");
            Check(ReferenceEquals(expected, pack.GetBundleInfoFromAssetPath(ProjectPath(TextA))), "Mixed-case full path maps to same BundleInfo.");
            Check(ReferenceEquals(expected, pack.GetBundleInfoFromAssetPath(AssetPath.Resolve(TextA))), "Canonical key maps to same BundleInfo.");
            Check(pack.GetBundleInfoFromAssetPath(Outside) == expected, "External project path survives serialized round trip.");
            return UniTask.CompletedTask;
        }
        private static async UniTask BundleNames()
        {
            var env = await New();
            var a = env.Asset<TextAsset>(TextA); var b = env.Asset<TextAsset>(TextB);
            var pa = env.Asset<GameObject>(PrefabA); var pb = env.Asset<GameObject>(PrefabB);
            await Wait(() => a.IsDone && b.IsDone && pa.IsDone && pb.IsDone, "Same-name assets");
            Check(a.Result != null && a.Result.text == "text-A" && b.Result != null && b.Result.text == "text-B", "Two Same.txt files in one Bundle resolve independently.");
            Check(pa.Result != null && pa.Result.transform.Find("prefab-A") != null && pb.Result != null && pb.Result.transform.Find("prefab-B") != null, "Two Same.prefab files in one Bundle resolve independently by their distinct child contents.");
            var ia = env.Keep(env.Assets.InstantiateHandle(PrefabA, null));
            var ib = env.Keep(env.Assets.InstantiateHandle(ProjectPath(PrefabB), null, Vector3.one, Quaternion.identity));
            await Wait(() => ia.IsDone && ib.IsDone, "Instantiation");
            Check(ia.Result != null && ib.Result != null && ia.Result != ib.Result && ib.Result.transform.position == Vector3.one, "Both instantiate overloads resolve paths and own independent objects.");
        }
        private static async UniTask BundleAliases()
        {
            var env = await New();
            var a = env.Asset<TextAsset>(TextA); var b = env.Asset<TextAsset>(ProjectPath(TextA).ToUpperInvariant());
            await Wait(() => a.IsDone && b.IsDone, "Aliases");
            Check(a.Result != null && a.Result == b.Result, "Aliases return the same loaded object.");
            var cache = env.Provider.GetOrCreateBundleCache(TextBundle);
            var entries = Get<Dictionary<(string, Type), AssetInfoCache>>(cache, "_assetCacheMap");
            Check(entries.Count == 1 && entries.ContainsKey((AssetPath.Resolve(TextA), typeof(TextAsset))), "Aliases share one typed cache key.");
            var objectHandle = env.Asset<UnityEngine.Object>(TextA);
            await Wait(() => objectHandle.IsDone, "Different requested type");
            Check(objectHandle.Result == a.Result && entries.Count == 2, "Requested types retain independent cache entries.");
            var asset = a.Result;
            a.Dispose(); b.Dispose(); objectHandle.Dispose();
            Check(cache.IsRetained, "Final Handle release retains Bundle.");
            var retry = env.Asset<TextAsset>(ProjectPath(TextA));
            Check(retry.IsDone && retry.Result == asset && env.Locator.Calls == 1, "Retained alias load is a synchronous cache hit without reloading Bundle.");
        }
        private static async UniTask Preload()
        {
            var env = await New();
            var preload = env.Keep(env.Assets.PreloadAssetHandle<TextAsset>(new[] { TextA, ProjectPath(TextA), TextA.Replace('/', '\\'), TextB }));
            await Wait(() => preload.IsDone, "Preload");
            Check(preload.Result && preload.TotalCount == 2 && preload.LoadedCount == 2, "Preload deduplicates all aliases before loading.");
            var missing = RelativeRoot + "/Text/Missing.txt";
            var failed = env.Keep(env.Assets.PreloadAssetHandle<TextAsset>(new[] { TextA, missing, ProjectPath(missing) }));
            await Wait(() => failed.IsDone, "Failed preload");
            Check(!failed.Result && failed.TotalCount == 2 && failed.FailedCount == 1 && failed.FailedPaths.Single() == AssetPath.Resolve(missing), "FailedPaths contains the canonical full key exactly once.");
        }
        private static async UniTask InvalidRequests()
        {
            var env = await New();
            Throws(() => env.Assets.PreloadAssetHandle<TextAsset>(new[] { TextA, "../invalid.txt" }), "Batch validation rejects before any request.");
            Throws(() => env.Assets.LoadAssetHandle<TextAsset>("C:/invalid.txt"), "LoadAsset rejects disk path synchronously.");
            Throws(() => env.Assets.InstantiateHandle("../invalid.prefab", null), "Instantiate rejects traversal synchronously.");
            Throws(() => env.Assets.LoadSceneHandle(TextA, new LoadSceneParameters(LoadSceneMode.Additive)), "LoadScene rejects wrong extension synchronously.");
            Check(!env.Assets.HasAsset("../invalid.txt") && !env.Assets.HasAsset("missing.txt"), "HasAsset returns false for invalid and absent paths.");
            await Frame();
            Check(env.Locator.Calls == 0 && Get<Dictionary<string, AssetBundleCache>>(env.Provider, "_bundleCacheMap").Count == 0, "Invalid batch starts no Bundle or cache work.");
        }
        private static async UniTask OutsideRoot()
        {
            var env = await New();
            var asset = env.Asset<TextAsset>(Outside);
            await Wait(() => asset.IsDone, "External root asset");
            Check(env.Assets.HasAsset(Outside) && asset.Result != null && asset.Result.text == "outside-root", "Explicit root-external resource loads from Bundle.");
        }
        private static async UniTask DifferentScenes(bool editor, bool delayed = false, bool distinctName = false)
        {
            var env = await New(editor, delayed);
            var otherPath = distinctName ? SceneC : SceneB;
            var a = env.Scene(SceneA); var b = env.Scene(ProjectPath(otherPath).ToUpperInvariant());
            await Wait(() => a.IsDone && b.IsDone, "Different same-name scenes");
            if (!editor && !distinctName && a.Result.HasValue != b.Result.HasValue &&
                Errors.Any(IsNativeSceneCollisionLog) && Errors.All(IsSceneCollisionLog))
            {
                var survivor = a.Result.HasValue ? a.Result.Value : b.Result.Value;
                var expectedPath = a.Result.HasValue ? SceneA : SceneB;
                var expectedMarker = a.Result.HasValue ? "marker-A" : "marker-B";
                Check(survivor.isLoaded && survivor.path.ToLowerInvariant() == ProjectPath(expectedPath).ToLowerInvariant() &&
                    survivor.GetRootGameObjects().Any(o => o.name == expectedMarker), "The surviving scene has the exact expected path and content.");
                throw new KnownSceneCollisionException();
            }
            Check(a.Result.HasValue && b.Result.HasValue, "Both requested scenes load successfully.");
            var sa = a.Result.Value; var sb = b.Result.Value;
            Check(sa.handle != sb.handle && (distinctName ? sa.name != sb.name : sa.name == sb.name), "Requested scenes are distinct instances with expected names.");
            Check(AssetPath.ResolveScene(sa.path) == AssetPath.ResolveScene(SceneA) && AssetPath.ResolveScene(sb.path) == AssetPath.ResolveScene(otherPath), "Each completion belongs to the exact requested full path.");
            Check(sa.GetRootGameObjects().Any(o => o.name == "marker-A") && sb.GetRootGameObjects().Any(o => o.name == (distinctName ? "marker-C" : "marker-B")), "Scene contents confirm correct selection.");
            if (!editor)
            {
                foreach (var bundlePath in new[] { SceneBundle, distinctName ? SceneBundleC : SceneBundleB })
                {
                    var cache = env.Provider.GetOrCreateBundleCache(bundlePath);
                    var actualPaths = cache.Bundle.GetAllScenePaths();
                    var entries = Get<List<SceneInfoCache>>(cache, "_sceneCacheMap");
                    Check(entries.Count == 1 && entries.All(e => actualPaths.Contains(Get<string>(e, "_loadPath"))), "Scene entry preserves exact path returned by " + bundlePath);
                }
            }
            a.Dispose();
            await Wait(() => !sa.IsValid() || !sa.isLoaded, "Independent scene unload");
            Check(b.Result.HasValue && b.Result.Value.isLoaded, "Disposing one same-name scene leaves the other alive.");
        }
        private static async UniTask SceneInstances(bool editor)
        {
            var env = await New(editor);
            var first = env.Scene(SceneA);
            var rejected = env.Scene(ProjectPath(SceneA).ToUpperInvariant());
            Check(rejected.IsDone && rejected.Result == null, "Concurrent alias request rejected before another scene operation.");
            await Wait(() => first.IsDone, "First scene");
            Check(first.Result.HasValue && env.Loading == 0, "First scene succeeds and clears loading guard.");
            var second = env.Scene(ProjectPath(SceneA));
            await Wait(() => second.IsDone, "Second instance");
            Check(second.Result.HasValue && second.Result.Value.handle != first.Result.Value.handle, "Sequential same-path Additive load creates independent instance.");
            var firstScene = first.Result.Value;
            await SceneManager.UnloadSceneAsync(firstScene).ToUniTask();
            await Frame();
            Check(first.Result == null && second.Result.HasValue, "External unload clears only the matching Handle.");
            first.Dispose(); first.Dispose();
            Check(second.Result.Value.isLoaded, "Repeated disposal preserves the other instance.");
        }
        private static async UniTask SceneCancellation()
        {
            var env = await New();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var canceled = env.Keep(env.Assets.LoadSceneHandle(SceneA, new LoadSceneParameters(LoadSceneMode.Additive), ct: cts.Token));
            Check(canceled.IsDone && env.Loading == 0 && env.Locator.Calls == 0, "Pre-cancelled scene registers no request.");
            var pending = env.Scene(SceneA);
            pending.Dispose();
            await Wait(() => env.Loading == 0, "Released pending scene settles");
            for (var i = 0; i < 8; i++) await Frame();
            await Wait(() => FixtureScenes().Length == 0, "Late scene result is released");
            Check(pending.Result == null, "Disposed pending Handle never exposes a late scene.");
        }
        private static async UniTask EditorAssets()
        {
            var env = await New(true);
            var a = env.Asset<TextAsset>(TextA); var b = env.Asset<TextAsset>(ProjectPath(TextA).ToUpperInvariant());
            var outside = env.Asset<TextAsset>(Outside);
            await Wait(() => a.IsDone && b.IsDone && outside.IsDone, "Editor assets");
            Check(a.Result != null && a.Result.text == "text-A" && a.Result == b.Result, "Editor aliases select the same asset.");
            Check(outside.Result != null && outside.Result.text == "outside-root", "Editor resolves root-external project path.");
            Check(env.Assets.HasAsset(TextA) && env.Assets.HasAsset(ProjectPath(TextA)) && !env.Assets.HasAsset("../invalid.txt"), "Editor HasAsset shares input rules.");
        }
        private static async UniTask MissingScene()
        {
            var env = await New();
            var missing = env.Scene(RelativeRoot + "/Scenes/Missing/Battle.unity");
            Check(missing.IsDone && missing.Result == null && env.Loading == 0, "Missing scene metadata clears its loading guard.");
            var actual = env.Scene(SceneA);
            await Wait(() => actual.IsDone, "Valid same-name scene after missing path");
            Check(actual.Result.HasValue, "Valid same-name scene loads after failed lookup.");
        }

        private static bool IsNativeSceneCollisionLog(string message)
        {
            return new[] { SceneBundle, SceneBundleB }.Any(path => message ==
                "The AssetBundle '" + Path.GetFileName(path) + "' can't be loaded because another AssetBundle with the same files is already loaded.");
        }

        private static bool IsSceneCollisionLog(string message)
        {
            return IsNativeSceneCollisionLog(message) || message == "Load bundle failed : " + SceneBundle ||
                message == "Load bundle failed : " + SceneBundleB;
        }

        private static UniTask NativeCollision()
        {
            AssetBundle first = null;
            AssetBundle second = null;
            try
            {
                first = AssetBundle.LoadFromFile(Path.Combine(Bundles, SceneBundle));
                Check(first != null && first.GetAllScenePaths().Single().ToLowerInvariant() == ProjectPath(SceneA).ToLowerInvariant(), "Native API loads A Bundle and confirms its exact scene path.");
                second = AssetBundle.LoadFromFile(Path.Combine(Bundles, SceneBundleB));
                Check(second == null, "Native API rejects simultaneous B Bundle before any framework or scene loading call.");
                first.Unload(true);
                first = null;
                second = AssetBundle.LoadFromFile(Path.Combine(Bundles, SceneBundleB));
                Check(second != null && second.GetAllScenePaths().Single().ToLowerInvariant() == ProjectPath(SceneB).ToLowerInvariant(), "B Bundle loads independently after unloading A; build outputs represent different scene paths.");
                first = AssetBundle.LoadFromFile(Path.Combine(Bundles, SceneBundle));
                Check(first == null, "Native collision also reproduces with the loading order reversed.");
            }
            finally
            {
                if (first != null) first.Unload(true);
                if (second != null) second.Unload(true);
            }
            return UniTask.CompletedTask;
        }

        private static UniTask DefaultOptions()
        {
            var options = new ResBuildConfig().Options;
            Check((options & BuildAssetBundleOptions.ChunkBasedCompression) != 0, "Default compression remains ChunkBasedCompression.");
            Check((options & BuildAssetBundleOptions.DisableLoadAssetByFileName) != 0, "Default disables extensionless filename lookup.");
            Check((options & BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension) != 0, "Default disables filename-with-extension lookup.");
            Check(options == (BuildAssetBundleOptions.ChunkBasedCompression | FilenameFlags), "No unrelated default build flags changed.");
            return UniTask.CompletedTask;
        }

        private static async UniTask PackagesFolderLoad(bool editor)
        {
            var env = await New(editor);
            var first = env.Asset<TextAsset>(PackagesTextPath);
            var second = env.Asset<TextAsset>(PackagesTextPath.ToUpperInvariant());
            var full = env.Asset<TextAsset>(ProjectPath(PackagesTextPath));
            await Wait(() => first.IsDone && second.IsDone && full.IsDone, "Ordinary packages folder aliases");
            Check(first.Result != null && first.Result.text == "ordinary-packages-folder", "Public loading returns the actual root-relative packages resource.");
            Check(first.Result == second.Result && first.Result == full.Result, "Short, case-variant and complete project paths resolve the same object.");
            Check(env.Assets.HasAsset(PackagesTextPath) && env.Assets.HasAsset(ProjectPath(PackagesTextPath)), "HasAsset recognizes both public forms.");
            var preload = env.Keep(env.Assets.PreloadAssetHandle<TextAsset>(new[] { PackagesTextPath, ProjectPath(PackagesTextPath) }));
            await Wait(() => preload.IsDone, "Packages preload");
            Check(preload.Result && preload.TotalCount == 1, "Preload deduplicates root-relative packages aliases.");
            if (!editor)
            {
                var pack = ReadPack();
                var info = pack.GetBundleInfoFromAssetPath(PackagesTextPath);
                Check(info != null && info.AssetList.Contains(AssetPath.Resolve(PackagesTextPath)), "The actual collector's full path survives serialization and indexing.");
                var entries = Get<Dictionary<(string, Type), AssetInfoCache>>(env.Provider.GetOrCreateBundleCache(info.BundlePath), "_assetCacheMap");
                Check(entries.Count == 1 && env.Locator.Calls == 1, "Aliases share a single typed cache entry and one Bundle load.");
            }
        }

        private static async UniTask FullManifestResources()
        {
            var env = await New();
            var pack = ReadPack();
            Check(pack.BundleList.SelectMany(b => b.AssetList).All(p => p == AssetPath.ResolveProjectAssetPath(p)), "All loaded manifest entries are full canonical project paths.");
            foreach (var relative in SpecialTexts)
            {
                var fullPath = ProjectPath(relative);
                var info = pack.GetBundleInfoFromAssetPath(fullPath);
                Check(info != null && info.AssetList.Contains(fullPath.ToLowerInvariant()), "Full identity survives collection and storage: " + fullPath);
                var handle = env.Asset<TextAsset>(fullPath);
                await Wait(() => handle.IsDone, "Full-path manifest asset");
                Check(handle.Result != null && handle.Result.text == "full-path:" + relative, "Actual Bundle loading returns the distinct content: " + relative);
                if (!relative.StartsWith("assets/", StringComparison.Ordinal))
                {
                    var alias = env.Asset<TextAsset>(relative);
                    await Wait(() => alias.IsDone, "Business relative alias");
                    Check(alias.Result == handle.Result, "Business relative path shares the full-path resource.");
                }
            }
            Check(pack.GetBundleInfoFromAssetPath(ProjectPath(SpecialTexts[4])).BundlePath == "__pathutilsdotted.v2.ab", "Dotted source folder builds and loads from its own Bundle.");
            Check(pack.GetBundleInfoFromAssetPath(ProjectPath(SpecialTexts[2])).BundlePath == "default.ab" &&
                pack.GetBundleInfoFromAssetPath(ProjectPath(SpecialTexts[3])).BundlePath == "default.ab", "Root and default-folder assets retain existing default.ab grouping.");
        }

        private static async UniTask UpmNotSupported(bool editor)
        {
            Check(AssetDatabase.LoadAssetAtPath<Texture2D>(UpmTexturePath) != null, "The genuine UPM texture exists in this project.");
            var mappedKey = AssetPath.Resolve(UpmTexturePath);
            Check(mappedKey == (LiteConst.AssetRootPath + "/" + UpmTexturePath).ToLowerInvariant(), "Public input maps under the resource root, not under project Packages.");
            Check(AssetDatabase.LoadAssetAtPath<Texture2D>(mappedKey) == null, "No same-named resource-root counterpart exists.");
            var env = await New(editor);
            Check(!env.Assets.HasAsset(UpmTexturePath), "HasAsset does not fall back to the existing UPM resource.");
            var handle = env.Asset<Texture2D>(UpmTexturePath);
            await Wait(() => handle.IsDone, "Missing root-relative counterpart");
            Check(handle.Result == null, "LoadAssetHandle does not load the real UPM texture by fallback.");
            if (!editor) Check(env.Locator.Calls == 0, "Missing package-looking key starts no Bundle load.");
        }

        private static async UniTask LookupProbe(bool useAsync)
        {
            foreach (var disabled in new[] { false, true })
            {
                var bundleFile = disabled ? Path.Combine(Bundles, TextBundle) : Path.Combine(ControlBundles, ControlBundle);
                var bundle = AssetBundle.LoadFromFile(bundleFile);
                try
                {
                    Check(bundle != null, "Load " + (disabled ? "new-default" : "control") + " probe Bundle.");
                    var fullPath = ProjectPath(ProbePath).ToLowerInvariant();
                    Check(bundle.GetAllAssetNames().Contains(fullPath), "Probe exists under the exact full project path.");
                    var inputs = new[] { fullPath, "uniquelookupprobe", "uniquelookupprobe.txt" };
                    for (var i = 0; i < inputs.Length; i++)
                    {
                        TextAsset asset;
                        if (useAsync)
                        {
                            var request = bundle.LoadAssetAsync<TextAsset>(inputs[i]);
                            await Wait(() => request.isDone, "Native asynchronous probe lookup");
                            asset = request.asset as TextAsset;
                        }
                        else
                        {
                            asset = bundle.LoadAsset<TextAsset>(inputs[i]);
                        }
                        var shouldLoad = i == 0 || !disabled;
                        Check(shouldLoad ? asset != null && asset.text == ProbeContent : asset == null,
                            (useAsync ? "Async " : "Sync ") + (disabled ? "new defaults: " : "control: ") + inputs[i] +
                            (shouldLoad ? " loads correct content." : " is disabled (null)."));
                    }
                }
                finally
                {
                    if (bundle != null) bundle.Unload(true);
                }
            }
        }
    }
}
