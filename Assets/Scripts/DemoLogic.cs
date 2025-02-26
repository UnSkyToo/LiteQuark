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
            LiteBattleEngine.Instance.Startup("player");
            return true;
        }

        public void Shutdown()
        {
            LiteBattleEngine.Instance.Shutdown();
        }
        
        public void Tick(float deltaTime)
        {
            LiteBattleEngine.Instance.Tick(deltaTime);
        }
    }
}