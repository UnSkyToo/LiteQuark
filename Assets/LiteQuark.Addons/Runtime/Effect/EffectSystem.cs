using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class EffectSystem : ISystem, ITick
    {
        private readonly ListEx<EffectObject> EffectList_ = new ListEx<EffectObject>();
        
        public EffectSystem()
        {
            EffectList_.Clear();
        }
        
        public Task<bool> Initialize()
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            EffectList_.Foreach((effect) => effect.Dispose());
            EffectList_.Clear();
        }

        public void Tick(float deltaTime)
        {
            EffectList_.Foreach((effect, list, dt) =>
            {
                if (effect.IsEnd)
                {
                    effect.Dispose();
                    list.Remove(effect);
                }
                else
                {
                    effect.Tick(dt);
                }
            }, EffectList_, deltaTime);
        }

        public BaseObject FindEffect(ulong id)
        {
            return EffectList_.ForeachReturn((effect) => effect.UniqueID == id);
        }

        public ulong PlayEffect(EffectCreateInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Path))
            {
                return 0;
            }

            var effect = new EffectObject(info);
            EffectList_.Add(effect);
            return effect.UniqueID;
        }
        
        public void StopEffect(ulong id)
        {
            if (id == 0)
            {
                return;
            }
            
            var effect = EffectList_.ForeachReturn((effect) => effect.UniqueID == id);
            effect?.Stop();
        }
    }
}