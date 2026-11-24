using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class PoolValidation : BundleValidation
    {
        private sealed class FailingPool : EmptyGameObjectPool
        {
            internal readonly InvalidOperationException Failure = new("Pool disposal failed.");

            public override void Dispose()
            {
                base.Dispose();
                throw Failure;
            }
        }

        internal static async UniTask Exercise(ValidationCase result)
        {
            _case = result;
            var h = New();
            var assets = LiteRuntime.Asset;
            var original = Get<AssetManager>(assets, "_manager");
            Set(assets, "_manager", h.Provider);
            var pools = new ObjectPoolSystem();
            await pools.Initialize();
            try
            {
                var warmup = pools.PreloadPoolAsync<ActiveGameObjectPool>(PrefabPath, 2, pools.GetPoolRoot());
                await Wait(() => warmup.Status.IsCompleted(), "Active pool native warmup");
                Check(await warmup, "Active pool preloads a real Bundle prefab and completes generated instances.");
                var pool = pools.GetActiveGameObjectPool(PrefabPath);
                var go = await pool.Alloc();
                Check(go != null && go.activeSelf && pool.CountInactive == 1, "Async allocation preserves Active pool activation.");
                pool.Recycle(go);
                Check(!go.activeSelf, "Recycling preserves Active pool deactivation.");

                var key = "assets/__session_validation/c.prefab";
                var oldPool = pools.GetActiveGameObjectPool(key);
                var pending = oldPool.Alloc();
                ActiveGameObjectPool replacement = null;
                oldPool.Alloc(_ => replacement = pools.GetActiveGameObjectPool(key));
                pools.RemovePool(oldPool);
                await CheckPoolCancelled(pending);
                Check(replacement != null && !ReferenceEquals(oldPool, replacement) && ReferenceEquals(replacement, pools.GetActiveGameObjectPool(key)),
                    "Cancelling the old pool preserves its same-key replacement.");
                pools.RemovePool(oldPool);
                var ready = replacement.WaitReadyAsync();
                await Wait(() => ready.Status.IsCompleted(), "Replacement template delivery");
                Check(await ready, "Only the replacement receives the template.");
                var borrower = new GameObject("External pool owner");
                try
                {
                    var instance = await replacement.Alloc(borrower.transform);
                    var root = pools.GetPoolRoot();
                    var failing = pools.AddPool<FailingPool>("failing", root);
                    var sibling = pools.GetEmptyGameObjectPool("sibling");
                    Exception failure = null;
                    try { await pools.DisposeAsync(); }
                    catch (InvalidOperationException ex) { failure = ex; }
                    Check(ReferenceEquals(failure, failing.Failure) && !sibling.IsReady,
                        "A pool disposal error reaches the caller after the remaining pool is released.");
                    await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate);
                    Check(root == null && go == null && instance != null && instance.transform.parent == borrower.transform,
                        "Shutdown destroys the pool root and idle objects while an externally parented instance stays with its borrower.");
                }
                finally { UnityEngine.Object.DestroyImmediate(borrower); }
                await h.Shutdown();
                Check(!h.Has("a.ab") && !h.Has("b.ab") && !h.Has("c.ab"), "Pool templates and dependencies are released.");
            }
            finally
            {
                if (pools.GetPoolRoot() != null) await pools.DisposeAsync();
                Set(assets, "_manager", original);
                await Cleanup();
                _case = null;
            }
        }

        private static async UniTask CheckPoolCancelled<T>(UniTask<T> request)
        {
            var cancelled = false;
            try { await request; }
            catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled, "An asynchronous pool request preserves cancellation instead of returning ordinary failure.");
        }
    }
}
