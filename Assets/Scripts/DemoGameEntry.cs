using Cysharp.Threading.Tasks;
using LiteBattle.Runtime;
using LiteQuark.Runtime;

namespace LiteQuark.Demo
{
    [UnityEngine.Scripting.Preserve]
    public sealed class DemoGameEntry : IGameEntry, ITick
    {
        public UniTask<bool> Initialize()
        {
            return LiteNexusEngine.Instance.Startup();
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
    }
}
