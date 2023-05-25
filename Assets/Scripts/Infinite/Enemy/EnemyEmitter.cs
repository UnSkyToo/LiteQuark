using LiteQuark.Runtime;

namespace InfiniteGame
{
    public interface IEnemyEmitter : ITick
    {
        void Generate();
    }

    public sealed class NormalEnemyEmitter : IEnemyEmitter
    {
        private float EnemySpawnerTime_;
        
        public NormalEnemyEmitter()
        {
            EnemySpawnerTime_ = float.MaxValue;
        }

        public void Tick(float deltaTime)
        {
            EnemySpawnerTime_ += deltaTime;
            if (EnemySpawnerTime_ > 10f)
            {
                EnemySpawnerTime_ = 0;

                for (var i = 0; i < 8; ++i)
                {
                    Generate();
                }
            }
        }
        
        public void Generate()
        {
            BattleManager.Instance.CreateEnemy();
        }
    }
}