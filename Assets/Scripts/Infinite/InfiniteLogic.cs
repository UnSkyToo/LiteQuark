using LiteQuark.Runtime;

namespace InfiniteGame
{
    public class InfiniteLogic : ILogic
    {
        public bool Startup()
        {
            BattleManager.Instance.CreatePlayer();
            return true;
        }

        public void Shutdown()
        {
        }
        
        public void Tick(float deltaTime)
        {
            BattleManager.Instance.Tick(deltaTime);
        }
    }
}