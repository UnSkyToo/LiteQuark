using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    internal static partial class AssetLifecycleValidationRunner
    {
        private sealed class PriorityItem
        {
            internal int Priority;
            internal long Order;
        }

        private static UniTask PriorityQueueOrdering()
        {
            var queue = new LiteQuark.Runtime.PriorityQueue<PriorityItem>((a, b) => a.Priority.CompareTo(b.Priority));
            var fifo = Enumerable.Range(0, 6).Select(i => new PriorityItem { Order = i }).ToArray();
            foreach (var item in fifo) queue.Enqueue(item);
            Check(fifo.All(item => ReferenceEquals(item, queue.Dequeue())), "Six equal-priority items dequeue in original order.");
            fifo[0].Priority = -1; fifo[1].Priority = 1;
            queue.Enqueue(fifo[0]); queue.Enqueue(fifo[1]);
            fifo[0].Priority = 1; queue.UpdatePriority(fifo[0]);
            Check(ReferenceEquals(fifo[0], queue.Dequeue()), "Promotion preserves original enqueue age among equal priorities.");
            queue.Clear();
            var random = new System.Random(9421);
            var model = new List<PriorityItem>();
            long order = 0;
            for (var i = 0; i < 10000; i++)
            {
                var op = random.Next(6);
                if (model.Count == 0 || op == 0)
                {
                    var item = new PriorityItem { Priority = random.Next(-2, 3), Order = order++ };
                    queue.Enqueue(item); model.Add(item);
                }
                else if (op == 1)
                {
                    var item = model[random.Next(model.Count)];
                    item.Priority = random.Next(-2, 3);
                    if (!queue.UpdatePriority(item)) throw new Exception("Missing priority index.");
                }
                else if (op == 2)
                {
                    var item = model[random.Next(model.Count)];
                    if (!queue.Remove(item) || queue.Contains(item)) throw new Exception("Remove/index mismatch.");
                    model.Remove(item);
                }
                else if (op == 3)
                {
                    var expected = model.OrderByDescending(x => x.Priority).ThenBy(x => x.Order).First();
                    if (!ReferenceEquals(expected, queue.Dequeue())) throw new Exception("Unstable dequeue.");
                    model.Remove(expected);
                }
                else if (op == 4)
                {
                    queue.Clear(); model.Clear(); order = 0;
                }
                else
                {
                    var rejected = false;
                    try { queue.Enqueue(model[0]); } catch (ArgumentException) { rejected = true; }
                    if (!rejected) throw new Exception("Duplicate accepted.");
                }
                if (queue.Count != model.Count || model.Any(item => !queue.Contains(item))) throw new Exception("Index/count mismatch.");
                if (model.Count > 0 && !ReferenceEquals(queue.Peek(), model.OrderByDescending(x => x.Priority).ThenBy(x => x.Order).First()))
                    throw new Exception("Heap differs from stable reference model.");
            }
            Check(true, "10000 deterministic operations match stable reference ordering, including decrease, removal, clear and duplicates.");
            return UniTask.CompletedTask;
        }

        private static UniTask PriorityLoaderScheduling()
        {
            var locator = new Locator(true);
            var bundles = Enumerable.Range(0, 6).Select(i => new BundleInfo(i, "queue" + i + ".ab", Array.Empty<string>(), null)).ToArray();
            var pack = new VersionPackInfo("priority", "windows", false, false, bundles);
            pack.Initialize();
            var loader = new AssetBundleLoader(locator, pack, 2);
            var callbacks = 0;
            try
            {
                foreach (var bundle in bundles) loader.LoadBundle(bundle, AssetLoadPriority.Normal, _ => callbacks++);
                loader.Tick(0);
                Check(locator.Started.SequenceEqual(new[] { "queue0.ab", "queue1.ab" }), "Two oldest equal-priority tasks start; remaining tasks stay queued.");
                loader.RaiseBundlePriority(bundles[0], AssetLoadPriority.High);
                loader.RaiseBundlePriority(bundles[4], AssetLoadPriority.High);
                loader.LoadBundle(bundles[4], AssetLoadPriority.Low, _ => callbacks++);
                loader.Tick(0);
                Check(locator.Started.Count == 2 && locator.Manual.Values.Count(t => !t.IsDone) == 2, "Promotion neither preempts running work nor exceeds the concurrency limit.");
                locator.Manual["queue0.ab"].Finish(null); loader.Tick(0);
                Check(locator.Started.Last() == "queue4.ab", "Shared queued request stays High despite a later Low request.");
                locator.Manual["queue1.ab"].Finish(null); loader.Tick(0);
                Check(locator.Started.Last() == "queue2.ab", "Remaining Normal work resumes FIFO.");
                for (var tick = 0; tick < 6 && callbacks < 7; tick++)
                {
                    locator.FinishOutstanding(); loader.Tick(0);
                    if (locator.Manual.Values.Count(t => !t.IsDone) > 2) throw new Exception("Concurrency exceeded.");
                }
                Check(locator.Started.SequenceEqual(new[] { "queue0.ab", "queue1.ab", "queue4.ab", "queue2.ab", "queue3.ab", "queue5.ab" }) &&
                    locator.Calls.Values.All(count => count == 1) && callbacks == 7,
                    "Each physical Bundle starts once; seven logical callbacks complete exactly once.");
            }
            finally { loader.Dispose(); }
            return UniTask.CompletedTask;
        }

        private static AssetLoadPriority CachePriority(Harness h, string path) => Get<AssetLoadPriority>(h.Cache(path), "_loadPriority");
        private static AssetHandle<T> PriorityAsset<T>(Harness h, string path, AssetLoadPriority priority) where T : UnityEngine.Object
        {
            var handle = h.Assets.LoadAssetHandle<T>(path, priority);
            h.Handles.Add(handle);
            return handle;
        }

        private static async UniTask PrioritySharedDependencies()
        {
            var h = New(true, 1);
            var blocker = h.Asset<TextAsset>(TextPath); h.Provider.Tick(0);
            var preload = h.Assets.PreloadAssetHandle<GameObject>(new[] { PrefabPath, "assets/__session_validation/c.prefab" }, AssetLoadPriority.Low);
            h.Handles.Add(preload);
            var high = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.High);
            var low = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.Low);
            Check(CachePriority(h, "a.ab") == AssetLoadPriority.High && CachePriority(h, "b.ab") == AssetLoadPriority.High &&
                CachePriority(h, "c.ab") == AssetLoadPriority.Low, "Higher request promotes its main and shared dependency without promoting an unrelated parent.");
            Check(h.Refs("b.ab") == 2 && Get<List<Action<AssetLoadResult<AssetBundle>>>>(h.Cache("b.ab"), "_bundleLoaderCallbackList").Count == 2,
                "Promotion adds no dependency lease or completion subscription.");
            h.Finish("text.ab", true); h.Finish("b.ab", true); h.Finish("a.ab", true); h.Finish("c.ab", true);
            await Wait(() => blocker.IsDone && preload.IsDone && high.IsDone && low.IsDone, "Prioritized shared resources");
            Check(preload.Result && high.Result != null && high.Result == low.Result, "Merged requests deliver valid shared assets.");
            Check(h.Locater.Started.SequenceEqual(new[] { "text.ab", "b.ab", "a.ab", "c.ab" }) && h.Locater.Calls.Values.All(n => n == 1),
                "Physical starts follow High then Low, and the shared dependency loads once.");
        }

        private static async UniTask PriorityMainBeforeDependency(bool completeMain)
        {
            var h = New(true, 1);
            var first = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.Low);
            var pack = Get<VersionPackInfo>(h.Provider, "_packInfo");
            // Deliberately prioritize only the physical main to exercise the supported arbitrary Bundle ordering.
            h.Provider.RaiseBundlePriority(pack.GetBundleInfoFromBundlePath("a.ab"), AssetLoadPriority.High);
            h.Provider.Tick(0);
            Check(h.Locater.Started.SequenceEqual(new[] { "a.ab" }), "Main starts while its dependency remains queued.");
            AssetHandle<TextAsset> blocker = null;
            if (completeMain)
            {
                blocker = h.Asset<TextAsset>(TextPath);
                h.Finish("a.ab", true);
                Check(h.Locater.Started.Last() == "text.ab" && !first.IsDone && h.Cache("a.ab").Stage == AssetCacheStage.Loading,
                    "Physical main is complete, but Cache and asset remain pending on queued dependency.");
            }
            var second = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.High);
            Check(CachePriority(h, "b.ab") == AssetLoadPriority.High && h.Locater.Calls["a.ab"] == 1 && h.Refs("b.ab") == 1,
                "Promotion reaches dependency without restarting main or duplicating its dependency lease.");
            if (completeMain) h.Finish("text.ab", true); else h.Finish("a.ab", true);
            Check(!first.IsDone && !second.IsDone && h.Locater.Started.Last() == "b.ab", "Asset extraction still waits for the dependency Bundle.");
            h.Finish("b.ab", true);
            await Wait(() => first.IsDone && second.IsDone && (blocker == null || blocker.IsDone), "Main-before-dependency delivery");
            Check(first.Result != null && first.Result == second.Result && first.Result.GetComponent<Renderer>().sharedMaterial != null,
                "Arbitrary Bundle order preserves real dependency references and shared asset delivery.");
        }

        private static async UniTask PriorityCanceledRequest()
        {
            var h = New(true, 1);
            var blocker = h.Asset<TextAsset>(TextPath); h.Provider.Tick(0);
            var high = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.High);
            high.Dispose();
            var low = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.Low);
            Check(CachePriority(h, "a.ab") == AssetLoadPriority.High && CachePriority(h, "b.ab") == AssetLoadPriority.High,
                "Disposing High and adding Low retain this loading round's maximum priority.");
            h.Finish("text.ab", true); h.Finish("b.ab", true); h.Finish("a.ab", true);
            await Wait(() => blocker.IsDone && high.IsDone && low.IsDone, "Canceled prioritized request cleanup");
            Check(high.Result == null && low.Result != null && h.AssetRefs("a.ab", PrefabPath, typeof(GameObject)) == 1 && h.Refs("b.ab") == 1,
                "Late canceled result releases its own lease while the remaining consumer keeps valid ownership.");
        }

        private static async UniTask PriorityRetry()
        {
            var h = New(true, 1);
            var first = PriorityAsset<TextAsset>(h, TextPath, AssetLoadPriority.High);
            h.Provider.Tick(0); h.Finish("text.ab", false);
            Check(first.IsDone && first.Result == null && h.Cache("text.ab").Stage == AssetCacheStage.Idle, "Failed High round returns to Idle.");
            var retry = PriorityAsset<TextAsset>(h, TextPath, AssetLoadPriority.Low);
            var normal = h.Asset<Material>(MaterialPath);
            Check(CachePriority(h, "text.ab") == AssetLoadPriority.Low, "New Low round overwrites the previous High priority.");
            h.Provider.Tick(0);
            Check(h.Locater.Started.Last() == "b.ab", "New Normal request starts before Low retry.");
            h.Finish("b.ab", true); h.Finish("text.ab", true);
            await Wait(() => retry.IsDone && normal.IsDone, "Priority reset retry");
            Check(retry.Result != null && normal.Result != null && h.Locater.Calls["text.ab"] == 2, "Retry succeeds through a fresh physical Bundle operation.");
        }

        private static async UniTask PriorityPublicEntryPoints()
        {
            var h = New(true);
            var preload = h.Assets.PreloadAssetHandle<TextAsset>(new[] { TextPath, TextPath }, AssetLoadPriority.Low);
            var first = h.Assets.InstantiateHandle(PrefabPath, null);
            var second = h.Assets.InstantiateHandle("assets/__session_validation/c.prefab", null, Vector3.one, Quaternion.identity, AssetLoadPriority.High);
            var scene = h.Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive), AssetLoadPriority.High);
            h.Handles.Add(preload); h.Handles.Add(first); h.Handles.Add(second); h.Handles.Add(scene);
            Check(CachePriority(h, "text.ab") == AssetLoadPriority.Low && CachePriority(h, "a.ab") == AssetLoadPriority.Normal &&
                CachePriority(h, "c.ab") == AssetLoadPriority.High && CachePriority(h, "scene.ab") == AssetLoadPriority.High &&
                CachePriority(h, "b.ab") == AssetLoadPriority.High, "Preload, default instantiate, positioned instantiate and scene propagate their priorities.");
            h.Provider.Tick(0);
            foreach (var path in h.Locater.Started.ToArray()) h.Finish(path, true);
            await Wait(() => preload.IsDone && first.IsDone && second.IsDone && scene.IsDone, "All public priority entry points");
            Check(preload.Result && preload.TotalCount == 1 && first.Result != null && second.Result != null && scene.Result.HasValue,
                "All entry points deliver real assets/instances/scene; preload still deduplicates.");
            Check(second.Result.transform.position == Vector3.one, "Positioned instantiate retains existing placement behavior.");
        }

        private static List<ITask> TaskSnapshot()
        {
            var tasks = new List<ITask>();
            LiteRuntime.Task.CopyTasksTo(tasks);
            return tasks;
        }

        private static TaskGroup RuntimeGroup(int id) =>
            Get<List<TaskGroup>>(LiteRuntime.Task, "_groups").Single(group => group.Id == id);

        private static List<TaskRecord> RuntimeRecords() =>
            Get<List<TaskRecord>>(LiteRuntime.Task, "_records");

        private static List<GateTask> FillGroup(int groupId, int count)
        {
            var tasks = new List<GateTask>();
            for (var i = 0; i < count; i++)
            {
                var task = new GateTask();
                tasks.Add(task);
                LiteRuntime.Task.AddTask(task, groupId);
            }
            return tasks;
        }

        private static async UniTask ReleaseGates(List<GateTask> gates)
        {
            foreach (var task in gates) task.Cancel();
            await Wait(() => gates.All(task => task.Disposed), "Gate tasks retired");
        }

        private static UniTask AssetGroupConfiguration()
        {
            var groups = Get<List<TaskGroup>>(LiteRuntime.Task, "_groups");
            Check(groups.Select(group => group.Id).Distinct().Count() == groups.Count,
                "Framework group IDs are unique after real AssetSystem initialization.");
            Check(RuntimeGroup(LiteConst.TaskGroupId.Default).ConcurrencyLimit == Math.Max(1, LiteRuntime.Setting.Task.ConcurrencyLimit)
                && RuntimeGroup(LiteConst.TaskGroupId.Asset).ConcurrencyLimit == Math.Max(1, LiteRuntime.Setting.Asset.TaskConcurrencyLimit)
                && RuntimeGroup(LiteConst.TaskGroupId.Flow).ConcurrencyLimit == null,
                "Default and Asset read their separate settings; Flow remains unbounded.");
            Check(LiteConst.TaskGroupId.Default == 0 && LiteConst.TaskGroupId.Flow == -1 && LiteConst.TaskGroupId.Asset == -2,
                "The new centralized reserved group IDs are used by the running framework.");
            return UniTask.CompletedTask;
        }

        private static async UniTask AssetGroupDefaultSaturation()
        {
            var h = New(false, 2);
            var group = RuntimeGroup(LiteConst.TaskGroupId.Default);
            var gates = FillGroup(group.Id, group.ConcurrencyLimit.Value);
            GateTask queued = null;
            try
            {
                await Wait(() => gates.All(task => task.IsExecuted), "Default saturation");
                queued = new GateTask();
                LiteRuntime.Task.AddTask(queued, group.Id, TaskPriority.Urgent);
                Check(group.RunningTaskCount == group.ConcurrencyLimit && !queued.IsExecuted,
                    "Default is full and an additional Urgent business task remains queued.");

                var text = h.Asset<TextAsset>(TextPath);
                var prefab = h.Asset<GameObject>(PrefabPath);
                var scene = h.Scene();
                await Wait(() => text.IsDone && prefab.IsDone && scene.IsDone,
                    "Real Asset group pipeline while Default remains full", 20);
                Check(text.Result != null && prefab.Result != null
                    && prefab.Result.GetComponent<Renderer>().sharedMaterial != null
                    && scene.Result.HasValue && scene.Result.Value.isLoaded,
                    "Real local Bundles, shared dependency, text, prefab and scene finish despite saturated Default.");
                Check(gates.All(task => task.State == TaskState.Running) && !queued.IsExecuted
                    && group.RunningTaskCount == group.ConcurrencyLimit,
                    "Resource completion did not borrow or release any Default slot; Urgent still does not bypass its group.");

                var loadedScene = scene.Result.Value;
                scene.Dispose();
                var unloadTask = TaskSnapshot().OfType<AsyncOperationTask>().Last();
                Check(RuntimeRecords().Single(record => ReferenceEquals(record.Task, unloadTask)).Group.Id == LiteConst.TaskGroupId.Flow,
                    "The real scene unload observer is registered in Flow.");
                await Wait(() => unloadTask.IsDone && h.Refs("scene.ab") == 0
                    && (!loadedScene.IsValid() || !loadedScene.isLoaded),
                    "Flow observes scene unload with Default full", 15);
                Check(!queued.IsExecuted && h.Refs("scene.ab") == 0,
                    "Scene unload and lease return finish through Flow without Default admission.");
            }
            finally
            {
                queued?.Cancel();
                await ReleaseGates(gates);
            }
        }

        private static async UniTask AssetGroupFactoryRouting()
        {
            var h = New();
            var group = RuntimeGroup(LiteConst.TaskGroupId.Asset);
            var gates = FillGroup(group.Id, group.ConcurrencyLimit.Value);
            var tasks = new List<ITask>();
            AssetBundle bundle = null;
            try
            {
                await Wait(() => gates.All(task => task.IsExecuted), "Asset group saturation");
                bundle = AssetBundle.LoadFromFile(Path.Combine(BundleFolder, "text.ab"));
                Check(bundle != null, "Factory routing fixture is a real loaded Bundle.");
                tasks.Add(LiteRuntime.Task.AddLoadVersionPackTask(new Uri(Path.Combine(BundleFolder, "unused-version.bytes")).AbsoluteUri, null));
                tasks.Add(LiteRuntime.Task.AddLoadLocalBundleTask(Path.Combine(BundleFolder, "text.ab"), null));
                tasks.Add(LiteRuntime.Task.AddLoadRemoteBundleTask("http://127.0.0.1/validation-not-sent.ab", null, null));
                tasks.Add(LiteRuntime.Task.AddLoadAssetTask<TextAsset>(bundle, "payload.txt", null));
                tasks.Add(LiteRuntime.Task.AddLoadSceneTask("validation_scene", new LoadSceneParameters(LoadSceneMode.Additive), null));
                for (var i = 0; i < tasks.Count; i++)
                {
                    var record = RuntimeRecords().Single(value => ReferenceEquals(value.Task, tasks[i]));
                    Check(record.Group.Id == LiteConst.TaskGroupId.Asset
                        && record.Priority == (i < 3 ? TaskPriority.Urgent : TaskPriority.Normal),
                        tasks[i].GetType().Name + " uses Asset and its unchanged fixed priority.");
                }

                var defaultRan = false;
                var flowRan = false;
                var defaultTask = new MainThreadTask(_ => defaultRan = true, null);
                LiteRuntime.Task.AddTask(defaultTask);
                LiteRuntime.Task.AddMainThreadTask(_ => flowRan = true, null);
                await Wait(() => defaultRan && flowRan, "Other groups execute while Asset is full");
                Check(tasks.All(task => !task.IsExecuted && task.State == TaskState.Pending)
                    && group.RunningTaskCount == group.ConcurrencyLimit,
                    "All five factories wait for Asset capacity, including Urgent; Default and Flow remain runnable.");
            }
            finally
            {
                foreach (var task in tasks) task.Cancel();
                await ReleaseGates(gates);
                if (bundle != null) bundle.Unload(true);
            }
        }

        private static async UniTask AssetGroupLoaderBudgets(int availableSlots)
        {
            const int loaderLimit = 2;
            Current = new Harness(true, loaderLimit, scheduleManual: true);
            var h = Current;
            var group = RuntimeGroup(LiteConst.TaskGroupId.Asset);
            var gates = FillGroup(group.Id, group.ConcurrencyLimit.Value - availableSlots);
            try
            {
                await Wait(() => gates.All(task => task.IsExecuted), "Reserve Asset slots");
                var text = h.Asset<TextAsset>(TextPath);
                var material = h.Asset<Material>(MaterialPath);
                var low = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.Low);
                var normal = PriorityAsset<GameObject>(h, "assets/__session_validation/c.prefab", AssetLoadPriority.Normal);
                h.Provider.Tick(0);
                LiteRuntime.Task.Tick(0);
                var admitted = h.Locater.Manual.Values.Count(task => task.IsExecuted && !task.IsDone);
                Check(h.Locater.Started.SequenceEqual(new[] { "text.ab", "b.ab" })
                    && admitted == Math.Min(availableSlots, loaderLimit),
                    $"Loader submits only {loaderLimit} tasks; with {availableSlots} Asset slots, exactly {Math.Min(availableSlots, loaderLimit)} are admitted.");

                var high = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.High);
                h.Provider.Tick(0);
                Check(h.Locater.Started.Count == loaderLimit && CachePriority(h, "a.ab") == AssetLoadPriority.High
                    && CachePriority(h, "b.ab") == AssetLoadPriority.High,
                    "Shared dependency promotion retains Loader capacity and does not restart submitted work.");
                Check(h.Locater.Manual.Values.All(task => RuntimeRecords().Single(record => ReferenceEquals(record.Task, task)).Priority == TaskPriority.Urgent),
                    "Already submitted Bundle records retain fixed Urgent priority after the Asset request is promoted.");

                var maxLoader = 0;
                var maxAdmitted = admitted;
                var deadline = Time.realtimeSinceStartup + 20;
                while (!(text.IsDone && material.IsDone && low.IsDone && normal.IsDone && high.IsDone))
                {
                    if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Dual-budget loading");
                    foreach (var pair in h.Locater.Manual.ToArray())
                    {
                        if (pair.Value.IsExecuted && !pair.Value.IsDone) h.Finish(pair.Key, true);
                    }
                    await Frame();
                    var loader = Get<AssetBundleLoader>(h.Provider, "_bundleLoader");
                    var submitted = Get<System.Collections.IList>(loader, "_runningTasks").Count;
                    admitted = h.Locater.Manual.Values.Count(task => task.IsExecuted && !task.IsDone);
                    maxLoader = Math.Max(maxLoader, submitted);
                    maxAdmitted = Math.Max(maxAdmitted, admitted);
                    if (submitted > loaderLimit || admitted > availableSlots || group.RunningTaskCount > group.ConcurrencyLimit)
                        throw new Exception("Loader or Asset group quota exceeded.");
                }
                Check(h.Locater.Started.SequenceEqual(new[] { "text.ab", "b.ab", "a.ab", "c.ab" })
                    && h.Locater.Calls.Values.All(count => count == 1),
                    "Loader promotion orders queued High before Normal, preserves initial FIFO, and submits each shared Bundle once.");
                Check(text.Result != null && material.Result != null && low.Result != null
                    && low.Result == high.Result && normal.Result != null,
                    "Controlled Bundle completions deliver real assets through the unchanged cache/lease pipeline.");
                Check(maxLoader <= loaderLimit && maxAdmitted <= Math.Min(availableSlots, loaderLimit),
                    $"Both limits remain independent throughout the run: Loader peak {maxLoader}, admitted Bundle peak {maxAdmitted}.");
            }
            finally
            {
                await ReleaseGates(gates);
            }
        }

        private sealed class GateTask : BaseTask
        {
            internal bool Disposed;
            protected override void OnExecute() { }
            protected override void OnDispose() { Disposed = true; }
        }
    }
}
