using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        private sealed class EffectProbePool : ActiveGameObjectPool
        {
            internal int Borrows;
            internal int Returns;
            internal Action<GameObject> Getting;
            public EffectProbePool() { }
            protected override void OnGet(GameObject go)
            {
                base.OnGet(go);
                Borrows++;
                Getting?.Invoke(go);
            }
            protected override void OnRelease(GameObject go)
            {
                base.OnRelease(go);
                Returns++;
            }
        }

        private sealed class EffectHarness
        {
            internal readonly Harness Assets = New();
            internal readonly EffectSystem System = new();
            internal readonly GameObject Parent = new("EffectOwner");
            internal readonly EffectProbePool Pool;
            internal int Completions;
            private readonly AssetManager _original;

            internal EffectHarness()
            {
                _original = Get<AssetManager>(LiteRuntime.Asset, "_manager");
                Set(LiteRuntime.Asset, "_manager", Assets.Provider);
                Pool = LiteRuntime.ObjectPool.AddPool<EffectProbePool>(EffectPath, LiteRuntime.ObjectPool.GetPoolRoot());
            }

            internal ulong Play(bool loop = false, int order = -1)
            {
                var info = new EffectCreateInfo(Parent.transform, EffectPath, EffectSpace.Local, Vector3.zero, 1f, Quaternion.identity, 1f, loop, 0.1f, order);
                info.SetCompleteCallback(() => Completions++);
                return System.PlayEffect(info);
            }

            internal EffectObject Find(ulong id) => System.FindEffect(id);
            internal void Stop(ulong id) => System.StopEffect(id);
            internal void Tick(float deltaTime) => System.Tick(deltaTime);

            internal async UniTask Cleanup()
            {
                try
                {
                    Pool.Getting = null;
                    await System.DisposeAsync();
                    LiteRuntime.ObjectPool.RemovePool(Pool);
                    await Assets.Shutdown();
                }
                finally
                {
                    Set(LiteRuntime.Asset, "_manager", _original);
                    UnityEngine.Object.DestroyImmediate(Parent);
                }
            }
        }

        private static async UniTask EffectShutdownPendingLoads()
        {
            var h = new EffectHarness();
            try
            {
                await h.System.Initialize();
                var enables = 0;
                h.Pool.Getting = go =>
                {
                    go.SetActive(false);
                    go.GetAsyncEnableTrigger().OnEnableAsync().ContinueWith(() => { enables++; }).Forget();
                };
                var firstID = h.Play();
                var first = h.Find(firstID);
                var secondID = h.Play();
                var second = h.Find(secondID);
                var retained = h.Assets.Asset<GameObject>(EffectPath);
                h.Stop(firstID);
                h.Tick(0f);
                h.Tick(0f);
                Check(!h.Pool.IsReady && first.IsDone && !second.IsValid && h.Pool.CountActive == 0,
                    "A stopped request retires while another Effect still waits for the same real prefab template.");
                var shutdown = h.System.DisposeAsync();
                Check(shutdown.Status == UniTaskStatus.Succeeded, "Effect shutdown releases its objects synchronously without waiting for shared template loading.");
                await shutdown;
                Check(first.IsDone && second.IsDone && !second.IsValid && h.Find(secondID) == null,
                    "Stopped and shutdown requests both remain terminal and disappear from the system list.");
                await Wait(() => retained.IsDone && h.Pool.IsReady, "Shared Effect template delivery after shutdown");
                await retained;
                Check(retained.Result != null && h.Assets.AssetRefs("effect.ab", EffectPath, typeof(GameObject)) == 2,
                    "The independent Handle and shared pool still own the successful prefab; Effect shutdown does not close either owner.");
                Check(enables == 0 && h.Completions == 0,
                    $"Late template success runs no Unity OnEnable continuations (observed {enables}) or Effect completion (observed {h.Completions}) for stopped or disposed requests.");
                Check(h.Pool.Borrows == h.Pool.Returns && h.Pool.CountActive == 0 && h.Parent.transform.childCount == 0,
                    "Stopped and disposed requests leave balanced pool ownership and no active instance under the caller.");
                var control = await h.Pool.Alloc(h.Parent.transform);
                h.Pool.Recycle(control);
                Check(enables == 1 && h.Pool.Borrows == h.Pool.Returns && h.Pool.CountActive == 0,
                    "An independent accepted allocation really executes the observed OnEnable continuation and returns its instance.");
                retained.Dispose();
            }
            finally { await h.Cleanup(); }
        }

        private static async UniTask EffectTerminalReuse()
        {
            var h = new EffectHarness();
            try
            {
                await h.System.Initialize();
                var id = h.Play(loop: true, order: 5);
                var retired = h.Find(id);
                await Wait(() => retired.IsValid, "Effect playback before external Dispose");
                var go = h.Parent.GetComponentInChildren<ParticleSystem>(true).gameObject;
                Check(go.GetComponent<ParticleSystemRenderer>().sortingOrder == 12, "Requested sorting offset applied.");
                ((IDisposable)retired).Dispose();
                Check(retired.IsDone && !retired.IsValid, "External Dispose clears all instance references and publishes a terminal state.");
                h.Tick(0f);
                Check(go.GetComponent<ParticleSystemRenderer>().sortingOrder == 7, "Recycling restores sorting order.");
                Check(h.Find(id) == null && h.Pool.Returns == 1, "Tick retires an externally disposed Effect without repeating its resource return.");
                var next = h.Find(h.Play(loop: true, order: 5));
                var particle = go.GetComponent<ParticleSystem>();
                Check(go.transform.IsChildOf(h.Parent.transform) && next.IsValid && particle.isPlaying,
                    "A new Effect reuses and plays the same real particle instance.");
                retired.Stop();
                retired.Play(2f);
                retired.SetSpeed(3f);
                ((IDisposable)retired).Dispose();
                Check(retired.IsDone && !retired.IsValid && next.IsValid &&
                    particle.isPlaying && Mathf.Approximately(particle.main.simulationSpeed, 1f) && h.Pool.CountActive == 1 && h.Pool.Returns == 1,
                    "An old reference cannot resurrect, stop, retime or recycle the instance now owned by another Effect.");
            }
            finally { await h.Cleanup(); }
        }
    }
}
