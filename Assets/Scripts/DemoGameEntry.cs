using System.Threading;
using Cysharp.Threading.Tasks;
using LiteBattle.Runtime;
using LiteQuark.Runtime;

namespace LiteQuark.Demo
{
    [UnityEngine.Scripting.Preserve]
    public sealed class DemoGameEntry : IGameEntry, ITick, ILateTick
    {
        public UniTask<bool> Initialize(CancellationToken ct = default)
        {
            return LiteNexusEngine.Instance.Startup(ct);
        }

        public UniTask DisposeAsync()
        {
            LiteNexusEngine.Instance.Shutdown();
            LiteRuntime.ObjectPool.RemoveUnusedPools();

            return UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
            LiteNexusEngine.Instance.Tick(deltaTime);
        }

        public void LateTick(float deltaTime)
        {
            LiteNexusEngine.Instance.LateTick(deltaTime);
        }
    }
}
