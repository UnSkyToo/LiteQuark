using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private const string SceneA = RelativeRoot + "/Scenes/A/Battle.unity";
        private const string SceneB = RelativeRoot + "/Scenes/B/Battle.unity";
        private const string SceneC = RelativeRoot + "/Scenes/C/OtherBattle.unity";
        private const string TextBundle = RelativeRoot + "/text.ab";
        private const string SceneBundle = RelativeRoot + "/scenes/a.ab";
        private const string SceneBundleB = RelativeRoot + "/scenes/b.ab";
        private const string SceneBundleC = RelativeRoot + "/scenes/c.ab";
        private const string Pending = "LiteQuark.AssetLoadingValidation.Pending";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
        private static string Reports => ValidationEnvironment.Reports("loading");
        private static string Bundles => Path.Combine(Root, "Bundles", "loading");
        private static string ControlBundles => Path.Combine(Root, "Bundles", "loading-control");
        private static readonly ValidationReport Report = new();
        private static ValidationCase Active;
        private static EnvironmentContext Context;
        private static bool Running;
        private const string SceneCollisionId = ValidationReport.SceneCollisionId;
        private const string NativeCollisionCase = ValidationReport.NativeCollisionCase;

        private sealed class KnownSceneCollisionException : Exception
        {
            internal KnownSceneCollisionException() : base("Unity rejects concurrent Bundles containing same-name scenes; confirmed separately by the native diagnostic.") { }
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
                Debug.LogException(ex);
                Report.Finish("loading");
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
            foreach (var folder in new[] { "Text/A", "Text/B", "Scenes/A", "Scenes/B", "Scenes/C" })
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
            var textFiles = new[] { ProjectPath(TextA), ProjectPath(TextB), Outside, ProjectPath(ProbePath), ProjectPath(PackagesTextPath) }
                .Concat(SpecialTexts.Select(ProjectPath)).ToArray();
            var builds = new[]
            {
                new AssetBundleBuild { assetBundleName = TextBundle, assetNames = textFiles },
                new AssetBundleBuild { assetBundleName = SceneBundle, assetNames = new[] { ProjectPath(SceneA) } },
                new AssetBundleBuild { assetBundleName = SceneBundleB, assetNames = new[] { ProjectPath(SceneB) } },
                new AssetBundleBuild { assetBundleName = SceneBundleC, assetNames = new[] { ProjectPath(SceneC) } }
            };
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
            var pack = new VersionPackInfo("path-validation", AppUtils.GetCurrentPlatformName(), false, false,
                builds.Select((build, index) => new BundleInfo(index + 1, build.assetBundleName,
                    build.assetNames.Select(AssetPath.ResolveProjectAssetPath).ToArray(), null)).ToArray());
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

        private static async UniTask Address()
        {
            await PathRules();
            foreach (var simulate in new[] { false, true })
            {
                var env = await New(simulate);
                var first = env.Asset<TextAsset>(TextA);
                var alias = env.Asset<TextAsset>(ProjectPath(TextA).ToUpperInvariant());
                var other = env.Asset<TextAsset>(TextB);
                var packages = env.Asset<TextAsset>(PackagesTextPath);
                var outside = env.Asset<TextAsset>(Outside);
                Check(AssetDatabase.LoadAssetAtPath<Texture2D>(UpmTexturePath) != null, "UPM texture exists outside the resource index.");
                var upm = env.Asset<Texture2D>(UpmTexturePath);
                await Wait(() => first.IsDone && other.IsDone && alias.IsDone && packages.IsDone && outside.IsDone && upm.IsDone, "Public asset addresses");
                Check(first.Result.text == "text-A" && other.Result.text == "text-B" && alias.Result == first.Result,
                    "Same filenames use their full paths and aliases share an asset.");
                Check(packages.Result.text == "ordinary-packages-folder" && upm.Result == null && !env.Assets.HasAsset(UpmTexturePath),
                    "Only the collected packages directory is public.");
                Check(simulate ? outside.Result == null : outside.Result.text == "outside-root", "Root-external access follows the active manifest.");
                foreach (var relative in SpecialTexts)
                {
                    var item = env.Asset<TextAsset>(ProjectPath(relative));
                    await Wait(() => item.IsDone, "Full manifest path");
                    Check(item.Result.text == "full-path:" + relative, "Exact full key resolves " + relative);
                }
                await Cleanup();
            }
        }

        private static async UniTask RunAll()
        {
            try
            {
                new GameObject("AssetPathValidationLauncher").AddComponent<LiteLauncher>();
                await Wait(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Running, "Runtime startup");
                await Run("loading/address", Address);
                await Run("loading/scene-paths", async () =>
                {
                    await DifferentScenes(true);
                    await Cleanup();
                    await DifferentScenes(false, true);
                });
                await Run("loading/builtin", BuiltInLocation);
                await Run("loading/remote", RemoteUriComposition, "VersionPack is invalid");
                await Run("loading/remote-startup-cancel", RemoteStartupCancellation);
                await Run("loading/backend-init", async () =>
                {
                    await DefaultOptions();
                    await InitializationFailure((BundleLoadMode)byte.MaxValue, "https://example.invalid/", typeof(ArgumentException));
                    await InitializationFailure(BundleLoadMode.Remote, "http://[", typeof(UriFormatException));
                });
                await Run("loading/filename-flags", async () =>
                {
                    await LookupProbe(false);
                    await LookupProbe(true);
                });
                await Run(ValidationReport.SceneCollisionCase, () => DifferentScenes(false));
                await Run(NativeCollisionCase, NativeCollision);
            }
            catch (Exception ex) { Report.fatalError = ex.ToString(); }
            finally
            {
                var diagnostic = Report.cases.SingleOrDefault(c => c.id == NativeCollisionCase);
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
                        item.Fail("Native diagnostic did not confirm the known limitation.");
                    }
                }
                Report.Finish("loading");
            }
        }

        private static async UniTask Run(string name, Func<UniTask> test, params string[] expectedErrors)
        {
            using (var execution = new ValidationCaseExecution(Report, name))
            {
                Active = execution.Result;
                Active.diagnostic = name == NativeCollisionCase;
                var collisionObserved = false;
                await execution.RunAsync(test, Cleanup, ex =>
                {
                    if (ex is not KnownSceneCollisionException) return;
                    collisionObserved = true;
                    Active.status = "pending_known_limitation";
                });
                execution.RejectUnexpectedErrors(message => Active.diagnostic ? IsNativeSceneCollisionLog(message) :
                    collisionObserved ? IsSceneCollisionLog(message) : expectedErrors.Any(message.Contains));
                if (Active.status != "pending_known_limitation")
                    Active.status = Active.passed ? (Active.diagnostic ? "diagnostic_passed" : "passed") : "failed";
            }
            Debug.Log("ASSET_PATH_VALIDATION " + Active.status + " " + name);
            Save();
            Active = null;
        }
        private static void Check(bool condition, string evidence)
        {
            Active.Check(condition, evidence);
        }
        private static void Throws(Action action, string evidence)
        {
            try { action(); }
            catch (ArgumentException) { Check(true, evidence); return; }
            Check(false, evidence);
        }
        private static void Save() => Report.Save("loading");
        private static async UniTask Frame()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            Context?.Provider.Tick(0);
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

        private static async UniTask RemoteUriComposition()
        {
            var compose = typeof(RemoteBundleProvider).GetMethod("GetVersionPackUri", Private);
            foreach (var item in new[] {
                (Base: "https://cdn.example/res", Query: "?t=", Fragment: ""),
                (Base: "https://cdn.example/res?token=a%2Bb#part", Query: "?token=a%2Bb&t=", Fragment: "#part"),
                (Base: "https://cdn.example/res?token=x&", Query: "?token=x&t=", Fragment: ""),
                (Base: "https://cdn.example/res?", Query: "?t=", Fragment: ""),
                (Base: "https://cdn.example/res#part?note=1", Query: "?t=", Fragment: "#part?note=1"),
                (Base: "https://cdn.example/res??flag#part", Query: "??flag&t=", Fragment: "#part") })
            {
                var locator = new RemoteBundleProvider(item.Base);
                var result = (string)compose.Invoke(locator, new object[] { "version #?.txt" });
                var parsed = new Uri(result);
                Check(parsed.AbsolutePath == "/res/version%20%23%3F.txt" && parsed.Fragment == item.Fragment &&
                    parsed.Query.StartsWith(item.Query, StringComparison.Ordinal) &&
                    Guid.TryParseExact(parsed.Query.Substring(item.Query.Length), "N", out _),
                    "The nonce is appended to the escaped manifest address without losing the existing query or fragment.");
                Check(result != (string)compose.Invoke(locator, new object[] { "version #?.txt" }),
                    "The same provider generates a new cache key for a new manifest request.");
            }

            var setting = LiteRuntime.Setting.Asset;
            var previousMode = setting.BundleMode;
            var previousUri = setting.BundleRemoteUri;
            var previousCache = LiteRuntime.Setting.Asset.DisableUnityWebCache;
            var assets = new AssetSystem();
            try
            {
                setting.BundleMode = BundleLoadMode.Remote;
                setting.BundleRemoteUri = "https://cdn.example/base%20dir?token=a%2Bb#part";
                LiteRuntime.Setting.Asset.DisableUnityWebCache = true;
                var locator = (RemoteBundleProvider)typeof(AssetSystem).GetMethod("CreateBundleProvider", Private).Invoke(assets, new object[] { true });
                var platform = Uri.EscapeDataString(AppUtils.GetCurrentPlatformName().ToLowerInvariant());
                var version = Uri.EscapeDataString(AppUtils.GetMainVersion());
                var expectedBase = "https://cdn.example/base%20dir/" + platform + "/" + version;
                Check(Get<string>(locator, "_remoteUri") == expectedBase + "?token=a%2Bb#part", "Actual provider appends platform and main version as URI directory segments.");
                Check(locator.DisableUnityWebCache, "Remote cache option is preserved.");
                var versionUri = (string)compose.Invoke(locator, new object[] { "version.txt" });
                Check(versionUri.StartsWith(expectedBase + "/version.txt?token=a%2Bb&t=", StringComparison.Ordinal) &&
                    versionUri.EndsWith("#part", StringComparison.Ordinal), "The manifest nonce retains the provider's version directory and suffix.");
                Check(PathUtils.ConcatUri(Get<string>(locator, "_remoteUri"), "ui/my icon.ab") == expectedBase + "/ui/my%20icon.ab?token=a%2Bb#part", "Bundle URI uses the same configured base with an escaped resource filename.");
            }
            finally
            {
                await assets.DisposeAsync();
                setting.BundleMode = previousMode;
                setting.BundleRemoteUri = previousUri;
                LiteRuntime.Setting.Asset.DisableUnityWebCache = previousCache;
            }
            var packBytes = File.ReadAllBytes(Path.Combine(Reports, "version.bytes"));
            using var versionServer = new BundleHttpServer(packBytes);
            versionServer.ReleaseBody();
            var origin = new Uri(versionServer.Url).GetLeftPart(UriPartial.Authority);
            var versionTask = new RemoteBundleProvider(origin + "/content?token=a%2Bb#part").LoadVersionPack("version file.txt");
            await Wait(() => versionTask.IsDone && versionServer.Work.IsCompleted, "Loopback version response");
            var target = versionServer.RequestLine.Split(' ')[1];
            const string expectedTarget = "/content/version%20file.txt?token=a%2Bb&t=";
            Check(versionTask.Result is VersionPackInfo received && received.IsValid &&
                target.StartsWith(expectedTarget, StringComparison.Ordinal) &&
                Guid.TryParseExact(target.Substring(expectedTarget.Length), "N", out _),
                "Server received the escaped version request and returned a usable manifest.");

            using var bundleServer = new BundleHttpServer(File.ReadAllBytes(Path.Combine(Bundles, TextBundle)));
            bundleServer.ReleaseBody();
            var remote = new RemoteBundleProvider(new Uri(bundleServer.Url).GetLeftPart(UriPartial.Authority)) { DisableUnityWebCache = true };
            var download = remote.LoadBundle(TextBundle, string.Empty);
            await download.WaitForCompletionAsync();
            try
            {
                Check(download.State == TaskState.Succeeded && download.Result != null && ReferenceEquals(download.GetBundle(), download.Result) &&
                    bundleServer.RequestLine.Contains(TextBundle),
                    "The remote task delivers the requested Bundle from the loopback server.");
            }
            finally { download.GetBundle()?.Dispose(); }

            var invalidBytes = System.Text.Encoding.UTF8.GetBytes("{\"IsValid\":false,\"BundleList\":[]}");
            using var invalid = new BundleHttpServer(LiteConst.SecurityMode ? SecurityUtils.EncryptBytes(invalidBytes, LiteConst.SecurityKey) : invalidBytes);
            invalid.ReleaseBody();
            var failed = new RemoteBundleProvider(new Uri(invalid.Url).GetLeftPart(UriPartial.Authority)).LoadVersionPack("invalid.txt");
            await Wait(() => failed.IsDone, "Invalid version response");
            Check(failed.State == TaskState.Failed && failed.Error != null, "Invalid version content fails the returned task.");
        }

        private static async UniTask RemoteStartupCancellation()
        {
            using var server = new BundleHttpServer(File.ReadAllBytes(Path.Combine(Reports, "version.bytes")));
            var runtime = LiteRuntime.Instance;
            var launcher = runtime.Launcher;
            var previous = LiteRuntime.Setting;
            var setting = new LiteSetting();
            setting.Asset.BundleMode = BundleLoadMode.Remote;
            setting.Asset.BundleRemoteUri = new Uri(server.Url).GetLeftPart(UriPartial.Authority);

            await runtime.Shutdown();
            UniTask? shutdown = null;
            try
            {
                runtime.Startup(launcher, setting);
                await Wait(() => server.BodyStarted, "In-flight startup manifest");
                Check(runtime.State == LiteRuntime.RuntimeState.InitSystem && LiteRuntime.Asset == null && !server.Work.IsCompleted,
                    "AssetSystem initialization waits for the held HTTP manifest.");

                shutdown = runtime.Shutdown().Preserve();
                await Wait(() => shutdown.Value.Status.IsCompleted() && server.ClientClosed, "Cancelled startup HTTP request");
                await shutdown.Value;
                Check(runtime.State == LiteRuntime.RuntimeState.Idle && LiteRuntime.Asset == null && !server.Work.IsCompleted,
                    "Shutdown aborts the socket and rolls back before the server releases the manifest.");
            }
            finally
            {
                if (runtime.State != LiteRuntime.RuntimeState.Idle)
                {
                    server.ReleaseBody();
                    await (shutdown ?? runtime.Shutdown());
                }
                runtime.Startup(launcher, previous);
                await Wait(() => runtime.State == LiteRuntime.RuntimeState.Running, "Runtime startup after cancellation");
            }
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
            var locator = new BuiltInBundleProvider();
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

                var resolve = typeof(BuiltInBundleProvider).GetMethod("GetRuntimeFullPath", Private);
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

        private static async UniTask AssertBuiltInLocation(BuiltInBundleProvider locator, string versionPath, string bundlePath, string marker, bool persistent)
        {
            var versionTask = locator.LoadVersionPack(versionPath);
            await Wait(() => versionTask.IsDone, "BuiltIn version load");
            await versionTask.WaitForCompletionAsync();
            var loadedPack = versionTask.Result as VersionPackInfo;
            Check(versionTask.State == TaskState.Succeeded && loadedPack != null && loadedPack.Version == marker, "Public version load selects " + marker + ", forceStreaming=" + locator.EditorForceStreamingAssets);
            Check(loadedPack.GetBundleInfoFromAssetPath(ProbePath)?.BundlePath == bundlePath, "Selected version file builds the full-path resource index.");
            var bundleTask = locator.LoadBundle(bundlePath, string.Empty);
            AssetBundle loadedBundle = null;
            try
            {
                await Wait(() => bundleTask.IsDone, "BuiltIn Bundle load");
                loadedBundle = Get<AssetBundle>(bundleTask.GetBundle(), "_bundle");
                Check(loadedBundle != null && bundleTask.Error == null, "Public Bundle load completes from the same location policy.");
                Check(loadedBundle.GetAllAssetNames().Contains(AssetPath.Resolve(TextA)) == persistent, "Different Bundle contents confirm the chosen location is " + marker);
                var asset = loadedBundle.LoadAsset<TextAsset>(AssetPath.Resolve(ProbePath));
                Check(asset != null && asset.text == ProbeContent, "Selected BuiltIn Bundle supports complete-path asset loading.");
            }
            finally
            {
                bundleTask.GetBundle()?.Dispose();
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
            public readonly AssetManager Provider = new();
            public T Keep<T>(T value) where T : IDisposable { Handles.Add(value); return value; }
            public AssetHandle<T> Asset<T>(string path) where T : UnityEngine.Object => Keep(Assets.LoadAssetHandle<T>(path));
            public SceneHandle Scene(string path) => Keep(Assets.LoadSceneHandle(path));
        }
        private sealed class LocalLocator : IBundleProvider
        {
            public BaseTask LoadVersionPack(string name) => throw new NotSupportedException();
            public ILoadBundleTask LoadBundle(string path, string hash)
            {
                var task = new LoadLocalBundleTask(Path.Combine(Bundles, path));
                LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
                return task;
            }
        }
        private static async UniTask<EnvironmentContext> New(bool editor = false)
        {
            Context = new EnvironmentContext();
            LiteRuntime.Setting.Asset.EnableRetain = true;
            LiteRuntime.Setting.Asset.BundleRetainTime = 300;
            if (editor)
            {
                var previous = LiteRuntime.Setting.Asset.BundleMode;
                LiteRuntime.Setting.Asset.BundleMode = BundleLoadMode.Simulate;
                try { Check(await Context.Provider.Initialize((IBundleProvider)typeof(AssetSystem).GetMethod("CreateBundleProvider", Private).Invoke(Context.Assets, new object[] { true })), "Simulate initializes a fresh collected manifest."); }
                finally { LiteRuntime.Setting.Asset.BundleMode = previous; }
            }
            else
            {
                var pack = ReadPack();
                Set(Context.Provider, "_packInfo", pack);
                Set(Context.Provider, "_bundleLoader", new AssetBundleLoader(new LocalLocator(), pack, 5));
            }
            Set(Context.Assets, "_manager", Context.Provider);
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
                await Context.Assets.DisposeAsync();
                Check(FixtureScenes().Length == 0, "All fixture scenes unloaded through production ownership.");
            }
            finally
            {
                foreach (var scene in FixtureScenes())
                {
                    Active.passed = false;
                    Active.evidence.Add("Harness-only cleanup: " + scene.path);
                    await SceneManager.UnloadSceneAsync(scene).ToUniTask();
                }
                Context = null;
                await UniTask.Yield();
            }
        }

        private static UniTask PathRules()
        {
            var expected = AssetPath.Resolve(TextA);
            foreach (var path in new[] { TextA, ProjectPath(TextA), ProjectPath(TextA).ToUpperInvariant(), TextA.Replace('/', '\\') })
                Check(AssetPath.Resolve(path) == expected, "Equivalent input: " + path);
            foreach (var path in new[] { null, "", "../a.txt", "folder", "a.txt/", "a.txt/." })
            {
                Check(!AssetPath.TryResolve(path, out _), "Reject invalid path: " + (path ?? "<null>"));
                Throws(() => AssetPath.Resolve(path), "Resolve throws for invalid input.");
            }
            Check(AssetPath.Resolve("AssetsBackup/a.txt") == "assets/standaloneassets/assetsbackup/a.txt", "Assets prefix respects directory boundary.");
            Check(AssetPath.Resolve("Packages/a.txt") == "assets/standaloneassets/packages/a.txt", "Packages is an ordinary resource-root-relative directory, not a UPM entry point.");
            Check(AssetPath.Resolve(Outside) == Outside.ToLowerInvariant(), "Explicit external path is preserved.");
            Throws(() => AssetPath.ResolveScene(TextA), "Scene extension is required.");
            return UniTask.CompletedTask;
        }

        private static async UniTask DifferentScenes(bool editor, bool distinctName = false)
        {
            var env = await New(editor);
            var otherPath = distinctName ? SceneC : SceneB;
            var a = env.Scene(SceneA); var b = env.Scene(ProjectPath(otherPath).ToUpperInvariant());
            await Wait(() => a.IsDone && b.IsDone, "Different same-name scenes");
            if (!editor && !distinctName && a.Result.HasValue != b.Result.HasValue &&
                Active.errors.Any(IsNativeSceneCollisionLog) && Active.errors.All(IsSceneCollisionLog))
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
            a.Dispose();
            await Wait(() => !sa.IsValid() || !sa.isLoaded, "Independent scene unload");
            Check(b.Result.HasValue && b.Result.Value.isLoaded, "Releasing one path leaves the other scene loaded.");
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
                Check(second == null && Active.errors.Count == 1 && IsNativeSceneCollisionLog(Active.errors[0]) && Active.errors[0].Contains(Path.GetFileName(SceneBundleB)),
                    "Native API rejects B with its exact same-file collision.");
                first.Unload(true);
                first = null;
                second = AssetBundle.LoadFromFile(Path.Combine(Bundles, SceneBundleB));
                Check(second != null && second.GetAllScenePaths().Single().ToLowerInvariant() == ProjectPath(SceneB).ToLowerInvariant(), "B Bundle loads independently after unloading A; build outputs represent different scene paths.");
                first = AssetBundle.LoadFromFile(Path.Combine(Bundles, SceneBundle));
                Check(first == null && Active.errors.Count == 2 && IsNativeSceneCollisionLog(Active.errors[1]) && Active.errors[1].Contains(Path.GetFileName(SceneBundle)),
                    "Reversed order rejects A with its exact same-file collision.");
            }
            finally
            {
                if (first != null) first.Unload(true);
                if (second != null) second.Unload(true);
            }
            return UniTask.CompletedTask;
        }

        private static async UniTask InitializationFailure(BundleLoadMode mode, string remoteUri, Type expectedException)
        {
            var setting = LiteRuntime.Setting.Asset;
            var previousMode = setting.BundleMode;
            var previousUri = setting.BundleRemoteUri;
            var tasks = LiteRuntime.Task;
            var groups = Get<List<TaskGroup>>(tasks, "_groups");
            var originalGroups = groups.ToArray();
            var assetGroupIndex = groups.FindIndex(group => group.Id == LiteConst.TaskGroupId.Asset);
            var originalAssetGroup = groups[assetGroupIndex];
            var assets = new AssetSystem();
            try
            {
                setting.BundleMode = mode;
                setting.BundleRemoteUri = remoteUri;
                Exception registrationError = null;
                try { _ = assets.Initialize(); } catch (Exception ex) { registrationError = ex; }
                Check(registrationError is ArgumentException argument && argument.ParamName == "groupId",
                    "Duplicate Asset group registration still throws synchronously before backend creation.");

                var pending = tasks.PendingTaskCount;
                var running = tasks.RunningTaskCount;
                UniTask<bool> initialization = default;
                Exception synchronousError = null;
                TaskGroup registeredGroup = null;
                // Isolate this public Initialize call from the fixture Runtime's existing group.
                // No await or player-loop tick occurs until the original group has been restored.
                groups.RemoveAt(assetGroupIndex);
                try
                {
                    try { initialization = assets.Initialize(); }
                    catch (Exception ex) { synchronousError = ex; }
                    registeredGroup = groups.SingleOrDefault(group => group.Id == LiteConst.TaskGroupId.Asset);
                }
                finally
                {
                    groups.RemoveAll(group => group.Id == LiteConst.TaskGroupId.Asset);
                    groups.Insert(assetGroupIndex, originalAssetGroup);
                }
                Check(synchronousError == null, "Backend creation failure does not escape the Initialize call synchronously.");
                Check(initialization.Status == UniTaskStatus.Faulted, "Initialize returns a faulted task rather than a successful false result.");
                Exception failure = null;
                try { await initialization; } catch (Exception ex) { failure = ex; }
                Check(failure?.GetType() == expectedException && failure.StackTrace.Contains("CreateBundleProvider"),
                    "Await preserves the original backend exception type and creation stack.");
                Check(registeredGroup != null && registeredGroup.ConcurrencyLimit == Math.Max(1, LiteRuntime.Setting.Asset.TaskConcurrencyLimit) &&
                    registeredGroup.PendingTaskCount == 0 && registeredGroup.RunningTaskCount == 0,
                    "The Asset group is registered before the failure, without submitting resource work.");
                Check(groups.SequenceEqual(originalGroups) && tasks.PendingTaskCount == pending && tasks.RunningTaskCount == running,
                    "The fixture restores the original group objects and order without changing existing tasks.");
                var manager = Get<AssetManager>(assets, "_manager");
                Check(Get<VersionPackInfo>(manager, "_packInfo") == null && Get<AssetBundleLoader>(manager, "_bundleLoader") == null &&
                    setting.BundleMode == mode && setting.BundleRemoteUri == remoteUri,
                    "Failed backend creation leaves manager content empty and does not rewrite configuration.");
            }
            finally
            {
                setting.BundleMode = previousMode;
                setting.BundleRemoteUri = previousUri;
                await assets.DisposeAsync();
            }
        }

        private static UniTask DefaultOptions()
        {
            var setting = LiteRuntime.Setting.Asset;
            var previous = setting.BundleMode;
            var previousUri = setting.BundleRemoteUri;
            var assets = new AssetSystem();
            var factory = typeof(AssetSystem).GetMethod("CreateBundleProvider", Private);
            try
            {
                var manager = Get<AssetManager>(LiteRuntime.Asset, "_manager");
                var loader = Get<AssetBundleLoader>(manager, "_bundleLoader");
                Check(Get<IBundleProvider>(loader, "_bundleProvider") is SimulateBundleProvider,
                    "Normal Runtime startup wires the simulated backend through AssetSystem initialization.");
                Check(new LiteSetting(LiteBuildProfile.Development).Asset.BundleMode == BundleLoadMode.Simulate &&
                    new LiteSetting(LiteBuildProfile.Release).Asset.BundleMode == BundleLoadMode.BuiltIn &&
                    (byte)BundleLoadMode.Simulate == 0 && (byte)BundleLoadMode.BuiltIn == 1 && (byte)BundleLoadMode.Remote == 2,
                    "Asset mode defaults to Release BuiltIn and Development Simulate with explicit serialized enum values.");
                setting.BundleMode = BundleLoadMode.Simulate;
                Check(factory.Invoke(assets, new object[] { true }) is SimulateBundleProvider &&
                    factory.Invoke(assets, new object[] { false }) is BuiltInBundleProvider && setting.BundleMode == BundleLoadMode.Simulate,
                    "Editor selects simulation; the same configuration selects BuiltIn in Player without rewriting settings.");
                setting.BundleRemoteUri = "https://cdn.example/validation/";
                foreach (var mode in new[] { BundleLoadMode.BuiltIn, BundleLoadMode.Remote })
                {
                    setting.BundleMode = mode;
                    var expected = mode == BundleLoadMode.BuiltIn ? typeof(BuiltInBundleProvider) : typeof(RemoteBundleProvider);
                    Check(factory.Invoke(assets, new object[] { true }).GetType() == expected &&
                        factory.Invoke(assets, new object[] { false }).GetType() == expected,
                        "Explicit " + mode + " is preserved in both environments.");
                }
            }
            finally
            {
                setting.BundleMode = previous;
                setting.BundleRemoteUri = previousUri;
            }
            var options = new ResBuildConfig().Options;
            Check((options & BuildAssetBundleOptions.ChunkBasedCompression) != 0, "Default compression remains ChunkBasedCompression.");
            Check((options & BuildAssetBundleOptions.DisableLoadAssetByFileName) != 0, "Default disables extensionless filename lookup.");
            Check((options & BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension) != 0, "Default disables filename-with-extension lookup.");
            Check(options == (BuildAssetBundleOptions.ChunkBasedCompression | FilenameFlags), "No unrelated default build flags changed.");
            return UniTask.CompletedTask;
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
