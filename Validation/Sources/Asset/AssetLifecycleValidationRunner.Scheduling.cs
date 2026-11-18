using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed partial class AssetLifecycleValidationRunner : BundleValidation
    {
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
            Check(h.Refs("b.ab") == 2 && Get<List<Action<AssetLoadResult<IBundleResource>>>>(h.Cache("b.ab"), "_bundleLoaderCallbackList").Count == 2,
                "Promotion adds no dependency lease or completion subscription.");
            high.Dispose();
            Check(CachePriority(h, "b.ab") == AssetLoadPriority.High, "Cancelled high request leaves shared dependency promoted.");
            h.Finish("text.ab", true); h.Finish("b.ab", true); h.Finish("a.ab", true); h.Finish("c.ab", true);
            await Wait(() => blocker.IsDone && preload.IsDone && high.IsDone && low.IsDone, "Prioritized shared resources");
            Check(preload.Result && high.Result == null && low.Result != null, "Merged requests deliver valid shared assets.");
            Check(h.Locater.Started.SequenceEqual(new[] { "text.ab", "b.ab", "a.ab", "c.ab" }) && h.Locater.Calls.Values.All(n => n == 1),
                "Physical starts follow High then Low, and the shared dependency loads once.");
        }

        private static TaskGroup RuntimeGroup(int id) =>
            Get<List<TaskGroup>>(LiteRuntime.Task, "_groups").Single(group => group.Id == id);

        private static List<TaskRecord> RuntimeRecords() =>
            Get<List<TaskRecord>>(LiteRuntime.Task, "_records");

        private static async UniTask AssetGroupLoaderBudgets(int availableSlots)
        {
            const int loaderLimit = 2;
            Current = new Harness(true, loaderLimit, scheduleManual: true);
            var h = Current;
            var group = RuntimeGroup(LiteConst.TaskGroupId.Asset);
            var defaultGroup = RuntimeGroup(LiteConst.TaskGroupId.Default);
            var business = FillGroup(defaultGroup.Id, defaultGroup.ConcurrencyLimit.Value);
            var gates = FillGroup(group.Id, group.ConcurrencyLimit.Value - availableSlots);
            try
            {
                await Wait(() => business.All(task => task.IsExecuted) && gates.All(task => task.IsExecuted), "Saturate Default and reserve Asset slots");
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
                await ReleaseGates(gates.Concat(business).ToList());
            }
        }
    }
}
