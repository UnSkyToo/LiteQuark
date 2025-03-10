using System.Threading.Tasks;
using LiteBattle.Runtime;
using LiteQuark.Runtime;

namespace LiteQuark.Demo
{
    public class DemoLogic : ILogic
    {
        public DemoLogic()
        {
        }
        
        public Task<bool> Initialize()
        {
            return LiteNexusEngine.Instance.Startup();
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            LiteNexusEngine.Instance.Shutdown();
        }
        
        public void Tick(float deltaTime)
        {
            LiteNexusEngine.Instance.Tick(deltaTime);
        }
    }
}