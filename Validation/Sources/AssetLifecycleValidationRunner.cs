using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    // Local-only harness. Production runtime sources are copied without modification.
    internal static partial class AssetLifecycleValidationRunner
    {
        private const string PendingKey = "LiteQuark.SessionValidation.Pending";
        private const string Fixture = "Assets/__session_validation";
        private const string TextPath = "assets/__session_validation/payload.txt";
        private const string SecondTextPath = "assets/__session_validation/second.txt";
        private const string PrefabPath = "assets/__session_validation/a.prefab";
        private const string MaterialPath = "assets/__session_validation/shared.mat";
        private const string ScenePath = "assets/__session_validation/validation_scene.unity";
        private const string EditorTextPath = "Assets/StandaloneAssets/_session_validation/lease_payload.txt";
        private const string EditorScenePath = "Assets/StandaloneAssets/_session_validation/editor_validation_scene.unity";
        private const string EditorSecondScenePath = "Assets/StandaloneAssets/_session_validation/editor_validation_second.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
        private static string BundleFolder => Path.Combine(Root, "Bundles", "lifecycle");
        private static readonly Report Results = new();
        private static readonly List<string> Errors = new();
        private static Harness Current;
        private static bool _running;
        private static CaseResult _case;

        [Serializable] private sealed class Report
        {
            public string unityVersion;
            public string sourceCommit;
            public string sourceSnapshot;
            public string finishedUtc;
            public string fatalError;
            public List<CaseResult> cases = new();
        }

        [Serializable] private sealed class CaseResult
        {
            public string name;
            public string mode;
            public bool passed;
            public int checks;
            public float seconds;
            public string failure;
            public List<string> evidence = new();
            public List<string> errorLogs = new();
        }

        public static void Begin()
        {
            try
            {
                Directory.CreateDirectory(ValidationEnvironment.Reports("lifecycle"));
                BuildFixtures();
                SessionState.SetBool(PendingKey, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
                SaveReport();
                Debug.LogException(ex);
                EditorApplication.Exit(2);
            }
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode) StartIfPending();
            };
            EditorApplication.delayCall += StartIfPending;
        }

        private static void StartIfPending()
        {
            if (_running || !EditorApplication.isPlaying || !SessionState.GetBool(PendingKey, false)) return;
            _running = true;
            SessionState.SetBool(PendingKey, false);
            RunAll().Forget();
        }

        private static async UniTask RunAll()
        {
            Application.logMessageReceived += CaptureLog;
            try
            {
                new GameObject("SessionValidationLauncher").AddComponent<LiteLauncher>();
                var deadline = Time.realtimeSinceStartup + 30;
                while (LiteRuntime.Instance.State != LiteRuntime.RuntimeState.Running)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new Exception("Runtime startup timed out.");
                    await UniTask.Yield();
                }
                await Run("01 Shared asset handles and one bundle lease", "real async Bundle + asset", SharedAsset);
                await Run("02 Retention expiry", "real async + controlled Tick time", Retention);
                await Run("03 Cleanup in first same-asset continuation", "real async + synchronous Handle continuation", ReentrantAsset);
                await Run("04 Dispose while asset request is submitted", "real AssetBundleRequest", DisposeDuringAsset);
                await Run("05 Token cancellation while loading", "real async + CancellationToken", CancelDuringAsset);
                await Run("06 Cached type mismatch", "real async", TypeMismatch, "Load asset failed");
                await Run("07 Missing asset in valid bundle", "real async", MissingAsset, "Load asset failed");
                await Run("08 Failed asset request followed by retry", "real async", RetryAsset, "Load asset failed");
                await Run("09 Real shared dependency and instantiate", "real async prefab/material", SharedDependency);
                await Run("10 Preload handle composition", "real async public API", Preload);
                await Run("11 Fast dependency survives slow parent", "controlled completion + real bundles", SlowParent);
                await Run("12 Main failure rolls back after dependencies", "controlled failure + real dependency", MainFailure, "Load bundle failed");
                await Run("13 Dependency failure holds until group settles", "controlled failure + real main", DependencyFailure, "Load bundle failed");
                await Run("14 Shared dependency survives one parent failure", "controlled failure + real bundles", SharedFailure, "Load bundle failed");
                await Run("15 Bundle callback cleanup handoff", "controlled ordering + real dependency asset request", BundleHandoff, "Load bundle failed");
                await Run("16 Failure callback immediate retry", "controlled failure + real retry", RetryBundle, "Load bundle failed");
                await Run("17 Loader creation throws then retries", "isolated throwing locator", LoaderFailure, "Injected locator failure", "Create bundle load task failed", "Load bundle failed");
                await Run("19 Asset task startup exception callback once", "fault injection through real TaskSystem", AssetTaskFailure,
                    "NullReferenceException: Object reference not set to an instance of an object", "Load asset failed");
                await Run("20 Asset task cancel before execution", "real task, pre-execution cancel", AssetTaskCancel);
                await Run("21 Disposed asset entry cannot revive", "real cache + queued task cancel", EntryDispose);
                await Run("22 Scene load and unload protection", "real async additive scene", SceneLifetime);
                await Run("23 Scene Handle cancelled before late success", "real async additive scene", SceneCancellation);
                await Run("24 Scene startup exception and name retry", "real invalid scene request", SceneFailure, "couldn't be loaded", "cannot be loaded", "Scene couldn't be loaded", "Task LoadSceneTask execute failed", "Load scene failed");
                await Run("25 Scene task cancel before execution", "real task, pre-execution cancel", SceneTaskCancel);
                await Run("26 Lease idempotency and disposed dependency", "real cache accounting", LeaseAccounting);
                await Run("27 Cleanup with retention disabled", "real async + zero retention", NoRetention);
                await Run("28 Asset startup failure reaches cache and Handle", "real TaskSystem + invalidated bundle injection", AssetFailurePipeline,
                    "Load asset failed", "MissingReferenceException: The object of type 'AssetBundle' has been destroyed");
                await Run("29 Pre-cancelled Handle starts no loading", "public Handle API", PreCancelledHandle);
                await Run("30 Missing dependency metadata rolls back", "isolated metadata fault + real bundle results", MissingDependency, "Can't get bundle info", "Missing dependency bundle", "Load bundle failed");
                await Run("31 Compatible requests own independent Asset leases", "real async + cached compatible types", AssetLeaseIdentity);
                await Run("32 Destroyed Unity asset still returns its reference", "real bundle asset + deliberate native destruction", DestroyedAssetLease);
                await Run("33 Asset lease releases exact entry after mapping replacement", "controlled dictionary replacement + real asset", ExactAssetEntry);
                await Run("34 Old Asset lease ignores unloaded entry and replacement", "controlled entry unload + real asset", UnloadedAssetLease);
                await Run("35 Editor Provider lease and delayed cancellation", "real AssetDatabase + ordinary and simulated delay", EditorAssetLease, "Can't load asset");
                await Run("36 Pre-cancelled scene starts no request", "public SceneHandle API", ScenePreCancelled);
                await Run("37 Dispose during submitted scene load", "real Unity scene operation", SceneDisposeDuringLoad);
                await Run("38 Same-name loading guard spans async operation", "controlled bundle delivery + real scene", SceneConcurrentGuard, "Scene is already loading");
                await Run("39 Sequential Additive loads own separate instances", "two real scene instances and leases", SceneIndependentInstances);
                await Run("40 Scene bundle failure rolls back and permits retry", "controlled bundle failure", SceneBundleFailure, "Load bundle failed");
                await Run("41 Missing scene metadata does not poison name", "missing metadata then real same-name scene", SceneMissingMetadata, "Can't get bundle info : missing/validation_scene.unity");
                await Run("42 Scene entry Dispose ends queued request once", "real queued LoadSceneTask", SceneEntryDispose);
                await Run("43 AssetSystem Dispose ends pending bundle request", "controlled pending bundle operation", SceneSystemDisposeDuringBundle, "Bundle load failed");
                await Run("44 AssetSystem Dispose ends queued scene request", "real queued LoadSceneTask", SceneSystemDisposeQueued);
                await Run("45 Pre-execution task cancel reaches SceneHandle", "real cache/task/Handle callback chain", SceneQueuedTaskCancel, "Load scene failed");
                await Run("46 Editor scene loading and independent release", "real Editor scenes; ordinary next-frame delivery", () => EditorSceneLifetime(false));
                await Run("47 Editor scene cancellation and repeated Dispose", "real Editor scenes; ordinary next-frame delivery", () => EditorSceneCancellation(false));
                await Run("48 Editor scene pre-cancel and startup failure retry", "public API + real invalid Editor path", EditorSceneFailure, "editor_validation_missing");
                await Run("49 Editor invalid scene rejected at delayed delivery", "manual completed unload before delayed callback", EditorSceneInvalidated,
                    "Can't load scene : Assets/StandaloneAssets/_session_validation/editor_validation_scene.unity");
                await Run("50 Editor scene loading with simulated delay", "real Editor scenes; simulated delay", () => EditorSceneLifetime(true));
                await Run("51 Editor scene cancellation with simulated delay", "real Editor scenes; simulated delay", () => EditorSceneCancellation(true));
                await Run("52 Editor native scene load timing", "direct Unity API timing observation", EditorNativeTiming);
                await Run("53 Bundle external scene unload", "two real same-path instances; native external unload", () => SceneAutomaticUnload(false));
                await Run("54 Bundle Single scene replacement", "real Single load; shared bundle ownership", () => SceneAutomaticUnload(true));
                await Run("55 Editor external scene unload", "real Editor scenes; ordinary delivery", () => EditorSceneAutomaticUnload(false, false));
                await Run("56 Editor Single scene replacement", "real Editor scenes; ordinary delivery", () => EditorSceneAutomaticUnload(true, false));
                await Run("57 Editor external unload with simulated delay", "real Editor scenes; simulated delay", () => EditorSceneAutomaticUnload(false, true));
                await Run("58 Editor Single replacement with simulated delay", "real Editor scenes; simulated delay", () => EditorSceneAutomaticUnload(true, true));
                await Run("59 Idle asset index survives another asset in the same bundle", "real assets + controlled Tick time", SharedBundleRetention);
                await Run("60 Dependency retention starts after parent unload", "real prefab/material + controlled Tick time", DependencyRetention);
                await Run("61 AssetSystem Dispose ends submitted asset request", "real submitted AssetBundleRequest", AssetSystemDisposeSubmitted);
                await Run("62 Pending requests own leases before Bundle delivery", "controlled Bundle + real asset task and cancellation", PendingRequestLeases);
                await Run("63 Final continuation cleans and reloads the same path", "real Bundle and synchronous Handle continuation", FinalContinuationReload);
                await Run("64 Asset failure cleans and retries the same path", "real wrong-type asset load then correct-type reload", AssetFailureCleanupRetry, "Load asset failed");
                await Run("65 Bundle failure cleans and retries the same path", "controlled Bundle failure + real asset retry", BundleFailureCleanupRetry, "Load bundle failed");
                await Run("66 Shared failure callbacks preserve a new retry", "controlled Bundle failure + interleaved Handle continuations", SharedFailureRetry, "Load bundle failed");
                await Run("67 Profiler exceptions do not interrupt lease transfer", "real Bundle and asset load + throwing event observer", ProfilerExceptionIsolation, "Injected validation profiler failure");
                await Run("68 Shutdown rejects retry of another asset in the same Bundle", "real submitted asset request + synchronous failure continuation", () => ShutdownFailureRetry(false));
                await Run("69 Shutdown rejects retry into a different Bundle", "real submitted asset request + synchronous failure continuation", () => ShutdownFailureRetry(true));
                await Run("70 Cache rejects Asset and Scene requests during and after unload", "direct cache boundary during real shutdown continuation", CacheShutdownRequestGuards);
                await Run("71 Provider rejects Scene retry and repeated disposal", "public SceneHandle during shutdown + closed Provider boundary", ProviderShutdownSceneGuard);
                await Run("72 Stable indexed priority queue", "production queue + deterministic randomized reference model", PriorityQueueOrdering);
                await Run("73 Loader stable order and concurrency", "production Loader + controlled tasks", PriorityLoaderScheduling);
                await Run("74 Shared dependency priority propagation", "public preload/load + real shared bundles", PrioritySharedDependencies);
                await Run("75 Running main still raises queued dependency", "controlled main-first start + real bundles", () => PriorityMainBeforeDependency(false));
                await Run("76 Completed main still raises queued dependency", "controlled main-first completion + real bundles", () => PriorityMainBeforeDependency(true));
                await Run("77 Canceled high request does not downgrade", "public Handle disposal + real late result", PriorityCanceledRequest);
                await Run("78 Failed loading round resets priority", "controlled failure then real retry", PriorityRetry, "Load bundle failed");
                await Run("79 Public priority entry points", "preload, both instantiate overloads and real scene", PriorityPublicEntryPoints);
                await Run("80 Asset group registration and configuration", "real framework startup and registered groups", AssetGroupConfiguration);
                await Run("81 Saturated Default still loads real assets and scene", "real local Bundles, dependencies, Asset and Scene tasks", AssetGroupDefaultSaturation);
                await Run("82 Five loading factories route to blocked Asset group", "actual factories; remote request is canceled before execution", AssetGroupFactoryRouting);
                await Run("83 Asset quota constrains Loader submissions", "one available Asset slot, Loader limit two, controlled completion and real assets", () => AssetGroupLoaderBudgets(1));
                await Run("84 Loader limit constrains available Asset quota", "three available Asset slots, Loader limit two, controlled completion and real assets", () => AssetGroupLoaderBudgets(3));
                await Run("85 Retention expires while timeScale is zero", "real asset and public AssetSystem Tick with Unity clock", RetentionDuringPause);
                await Run("86 Shutdown releases a late local Bundle", "controlled native request submission and real completion", ShutdownDuringLocalBundle);
                await Run("87 Shutdown releases a completed undelivered Bundle", "real Bundle success continuation closes before result callback", ShutdownBeforeBundleDelivery);
                await Run("88 Scene unload can recover after last-scene refusal", "real Single scene, Handle disposal and retry after host restoration", SceneUnloadRetry,
                    "Start scene unload failed", "Unloading the last loaded scene");
                await Run("89 Task result views and one-shot delivery", "real local/remote-file Bundles and asset extraction", TaskResultViews,
                    "Injected validation result observer");
                await Run("90 Failed local task releases a late Bundle", "real native request and controlled failure", FailedLocalBundleCleanup);
                await Run("91 Loader shutdown during delivery preserves ownership", "shared callbacks, real Bundles and reentrant close", LoaderShutdownHandoff);
                await Run("92 Shutdown rolls back main waiting for dependency", "controlled main delivery and pending dependency", ShutdownWithPendingDependency, "Bundle load failed");
                await Run("93 Shutdown releases undelivered remote-file Bundle", "file URI immediate handoff and close before result callback", RemoteBundleShutdown);
                await Run("94 Scene unload does not require a TaskSystem observer", "real native unload with Flow registration temporarily absent", SceneObserverFailure);
                await Run("95 External unload recovers a refused scene release", "real Single scene, refused release and external native unload", SceneExternalUnloadAfterRefusal,
                    "Start scene unload failed", "Unloading the last loaded scene");
                await Run("96 Remote shutdown during HTTP transfer", "loopback HTTP with body held mid-transfer", () => RemoteHttpCancellation(false));
                await Run("97 Remote shutdown after download before publication", "loopback HTTP with main-thread completion deferred", () => RemoteHttpCancellation(true));
                await Run("98 Scene failure preserves Profiler record identity", "controlled Task failures followed by real same-path scene loading", SceneFailureProfiler, "Load scene failed");
                await Run("99 Handle states and failure information", "public requests, real asset failure and provider shutdown", HandleStatusProtocol, "Load asset failed");
                await Run("100 Cancelled shared request rejects late success", "real asset with first continuation releasing its Handle", SharedCancellationSuccess);
                await Run("101 Cancelled shared request and immediate failure retry", "Task error snapshot and reentrant same-cache retry", SharedCancellationFailureRetry, "Load bundle failed");
                await Run("102 GameObject failure keeps await semantics", "real prefab ownership and null/throwing instantiate", InstantiateFailureProtocol);
                await Run("103 Preload preserves individual errors", "real mixed batch, cancellation and successful ownership", PreloadErrorProtocol, "Load asset failed");
                await Run("104 Pending resource Tasks expose cleanup without execution", "four Task types, repeated cleanup waits and original terminal callbacks", PendingResourceTaskCleanup, "Bundle download failed");
                await Run("105 Local Bundle cleanup survives wrapper retirement", "real native cancellation/failure and success await-Dispose handoff", LocalBundleTaskCleanup);
                await Run("106 Asset cleanup waits without releasing its Bundle", "real AssetBundleRequest cancellation and success await-Dispose handoff", AssetTaskCleanup);
                await Run("107 Scene cleanup unloads only cancelled late results", "real Additive scene, early wrapper disposal and success handoff", SceneTaskCleanup);
                await Run("108 Refused late Single scene cleanup faults independently", "real last-scene refusal, repeated cleanup faults and null logical await", SceneTaskCleanupRefusal,
                    "Unloading the last loaded scene");
                await Run("109 Remote cleanup follows request and timer retirement", "real file URI handoff, loopback cancellation and timeout retry timer", RemoteTaskCleanup,
                    "LoadRemoteBundleTask failed:", "Bundle download failed");
                await Run("110 Download release failure terminates retry and preserves terminal results", "throwing DownloadHandler, real HTTP timeout and controlled logical terminals", DownloadReleaseFailures,
                    "RequestReleaseProbe failed:");
                await Run("111 System close waits for retired Bundle cleanup", "native Bundle outlives logical failure and Loader retirement", ShutdownWaitsForRetiredBundle, "Load bundle failed");
                await Run("112 System close releases retained asset and scene Handles", "real scene unload and cached Bundle dependencies", ShutdownReleasesHeldResources);
                await Run("113 System close waits for cancelled native scene", "native loading, early task disposal and retained dependencies", ShutdownWaitsForNativeScene);
                await Run("114 System close reports refused scene unload", "real last-scene refusal and retained Bundle", ShutdownReportsSceneRefusal,
                    "Start scene unload failed", "Unloading the last loaded scene");
                await Run("115 System close drains other cleanup after a failure", "controlled cleanup outcomes with a real cached Bundle", ShutdownWaitsAfterCleanupFailure);
                await Run("116 Editor close drains ordinary and simulated delivery", "real Editor assets and Timer delivery", EditorShutdownDeliveries);
                await Run("117 Editor close unloads a delayed scene", "real Editor Scene and pending Handle delivery", EditorShutdownScene);
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
                SaveReport();
                EditorApplication.Exit(Results.fatalError == null && Results.cases.All(c => c.passed) ? 0 : 2);
            }
        }

        private static async UniTask Run(string name, string mode, Func<UniTask> action, params string[] expected)
        {
            _case = new CaseResult { name = name, mode = mode };
            Results.cases.Add(_case);
            Errors.Clear();
            var start = Time.realtimeSinceStartup;
            LiteRuntime.Setting.Asset.EnableRetain = true;
            LiteRuntime.Setting.Asset.BundleRetainTime = 300;
            try
            {
                await action();
                _case.passed = true;
            }
            catch (Exception ex)
            {
                _case.failure = ex.ToString();
            }
            finally
            {
                try { await Cleanup(); }
                catch (Exception ex) { _case.passed = false; _case.failure += "\nCleanup: " + ex; }
                _case.errorLogs.AddRange(Errors);
                var unexpected = Errors.Where(e => !expected.Any(e.Contains)).ToArray();
                if (unexpected.Length > 0)
                {
                    _case.passed = false;
                    _case.failure += "\nUnexpected errors: " + string.Join("\n", unexpected);
                }
                _case.seconds = Time.realtimeSinceStartup - start;
                Debug.Log("SESSION_VALIDATION " + (_case.passed ? "PASS " : "FAIL ") + name);
                SaveReport();
                _case = null;
            }
        }

        private static void CaptureLog(string message, string stack, LogType type)
        {
            if (_case != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Errors.Add(message);
        }

        private static void SaveReport()
        {
            Results.unityVersion = Application.unityVersion;
            var commitFile = Path.Combine(Root, "Reports", "source-commit.txt");
            Results.sourceCommit = File.Exists(commitFile) ? File.ReadAllText(commitFile).Trim() : "unknown";
            Results.sourceSnapshot = "Current workspace snapshot; see Reports/source-hashes.csv and source-state.json";
            Results.finishedUtc = DateTime.UtcNow.ToString("O");
            Directory.CreateDirectory(ValidationEnvironment.Reports("lifecycle"));
            File.WriteAllText(Path.Combine(ValidationEnvironment.Reports("lifecycle"), "results.json"), JsonUtility.ToJson(Results, true));
        }

        private static void Check(bool condition, string evidence)
        {
            _case.checks++;
            if (!condition) throw new Exception(evidence);
            _case.evidence.Add(evidence);
        }

        private static async UniTask Wait(Func<bool> predicate, string operation, float seconds = 12)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > end) throw new TimeoutException(operation);
                await Frame();
            }
        }

        private static async UniTask Frame()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (Current != null && !Current.Disposed) Current.Provider.Tick(0);
        }

        private static UniTask ObserveResourceCleanup(BaseTask task)
        {
            var before = TaskSnapshot();
            var cleanup = ((IResourceTaskCleanup)task).WaitForCleanupAsync();
            Check(TaskSnapshot().SequenceEqual(before), task.DebugName + " cleanup observation adds no TaskSystem observer.");
            return cleanup;
        }

        private static async UniTask WaitForResourceCleanup(UniTask first, UniTask second, string operation)
        {
            async UniTask AwaitCleanup(UniTask cleanup) { await cleanup; }
            var firstWait = AwaitCleanup(first);
            var secondWait = AwaitCleanup(second);
            await WaitWithoutLoaderTick(() => firstWait.Status.IsCompleted() && secondWait.Status.IsCompleted(), operation);
            try { await firstWait; }
            finally { await secondWait; }
        }

        private static async UniTask<object> DisposeInResourceTaskAwait(BaseTask task, List<string> events)
        {
            var result = await task.Task;
            events.Add("await");
            Check(Get<Delegate>(task, "_callback") == null, task.DebugName + " detaches its success callback before the await continuation.");
            task.Dispose();
            return result;
        }

        private static async UniTask Cleanup()
        {
            if (Current == null) { await UniTask.Yield(); return; }
            foreach (var handle in Current.Handles) handle.Dispose();
            Current.Locater.FinishOutstanding();
            // Let already-submitted native operations settle before enumerating fixture scenes.
            for (var i = 0; i < 4; i++) await Frame();
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(s => s.name == "validation_scene" || s.name.StartsWith("editor_validation_", StringComparison.Ordinal)).ToArray();
            foreach (var scene in scenes)
            {
                if (!string.IsNullOrEmpty(_case.failure) && scene.IsValid() && scene.isLoaded)
                {
                    // Test isolation only: do not let a failed production ownership path contaminate later cases.
                    _case.evidence.Add("Harness-only cleanup of leaked fixture Scene " + scene.name + "#" + scene.handle +
                        "; this is not counted as successful production cleanup.");
                    SceneManager.UnloadSceneAsync(scene);
                }
                try
                {
                    await Wait(() => !scene.IsValid() || !scene.isLoaded, "Scene cleanup", 15);
                }
                catch (TimeoutException ex)
                {
                    _case.passed = false;
                    _case.failure += "\nProduction scene cleanup did not finish: " + scene.name + "#" + scene.handle + "\n" + ex;
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        _case.evidence.Add("Harness-only cleanup after timeout for Scene " + scene.name + "#" + scene.handle +
                            "; the production cleanup failure remains a failed case.");
                        SceneManager.UnloadSceneAsync(scene);
                        await Wait(() => !scene.IsValid() || !scene.isLoaded, "Harness scene isolation cleanup", 15);
                    }
                }
            }
            for (var i = 0; i < 4; i++) await Frame();
            if (!Current.Disposed)
            {
                if (Current.ShutdownFailed)
                {
                    _case.evidence.Add("Fixture teardown after recorded close failure; this does not define a Runtime retry API.");
                    var caches = Get<Dictionary<string, AssetBundleCache>>(Current.Provider, "_bundleCacheMap");
                    // All close waits have settled; failure fixtures explicitly release their remaining native Bundles.
                    foreach (var cache in caches.Values) if (cache.Bundle != null) cache.Bundle.Unload(true);
                    caches.Clear();
                    Current.Disposed = true;
                }
                else
                {
                    Current.Assets.UnloadUnusedAssets(20);
                    await Current.Shutdown();
                }
            }
            foreach (var bundle in Current.Locater.Opened) if (bundle != null) bundle.Unload(true);
            Current = null;
            await UniTask.Yield();
        }

        private static Harness New(bool manual = false, int concurrency = 10)
        {
            Current = new Harness(manual, concurrency);
            return Current;
        }

        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    }
}
