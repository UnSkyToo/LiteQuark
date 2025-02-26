using System;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public abstract class LiteEntityModuleBase : ITick, IDisposable
    {
        public LiteEntity Entity { get; }
        
        protected LiteEntityModuleBase(LiteEntity entity)
        {
            Entity = entity;
        }

        public abstract void Dispose();
        
        public abstract void Tick(float deltaTime);
    }
}