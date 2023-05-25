using LiteQuark.Runtime;

namespace InfiniteGame
{
    public class InfiniteLogic : ILogic
    {
        public bool Startup()
        {
            BulletFactory.Instance.Initialize();
            BattleManager.Instance.CreatePlayer();
            // LiteRuntime.Get<UISystem>().OpenUI<UIChooseSkill>(1, 3);
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