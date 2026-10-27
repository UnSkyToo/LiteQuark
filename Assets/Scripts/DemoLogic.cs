using Cysharp.Threading.Tasks;
using LiteBattle.Runtime;
using LiteQuark.Runtime;

namespace LiteQuark.Demo
{
    public class DemoLogic : ILogic
    {
        public DemoLogic()
        {
        }

        public UniTask<bool> Initialize()
        {
            return LiteNexusEngine.Instance.Startup();
            return UniTask.FromResult(true);
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
