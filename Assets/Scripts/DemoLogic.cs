using LiteBattle.Runtime;
using LiteQuark.Runtime;

namespace LiteQuark.Demo
{
    public class DemoLogic : ILogic
    {
        public DemoLogic()
        {
        }
        
        public bool Startup()
        {
            LiteNexusEngine.Instance.Startup();
            return true;
        }

        public void Shutdown()
        {
            LiteNexusEngine.Instance.Shutdown();
        }
        
        public void Tick(float deltaTime)
        {
            LiteNexusEngine.Instance.Tick(deltaTime);
        }
    }
}