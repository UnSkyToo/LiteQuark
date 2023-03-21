using LiteQuark.Runtime;

namespace InfiniteGame
{
    public abstract class SkillBase : ITick
    {
        public int Level { get; private set; }
        
        protected SkillBase()
        {
            Level = 1;
        }

        public abstract void Tick(float deltaTime);

        public void AddLevel()
        {
            Level++;
            OnLevelUp();
        }

        public void Attach()
        {
            OnAttach();
        }

        protected virtual void OnAttach()
        {
        }

        protected virtual void OnLevelUp()
        {
        }
    }
}