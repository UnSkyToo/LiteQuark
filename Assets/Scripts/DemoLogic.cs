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

        public void Dispose()
        {
            LiteNexusEngine.Instance.Shutdown();
            LiteRuntime.ObjectPool.RemoveUnusedPools();
        }
        
        public void Tick(float deltaTime)
        {
            LiteNexusEngine.Instance.Tick(deltaTime);
        }
    }
}