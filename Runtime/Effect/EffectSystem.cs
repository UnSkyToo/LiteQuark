using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class EffectSystem : ISystem, ITick
    {
        private readonly ListEx<EffectObject> _effectList = new ListEx<EffectObject>();
        
        public EffectSystem()
        {
            _effectList.Clear();
        }
        
        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            _effectList.Foreach((effect) => effect.Dispose());
            _effectList.Clear();
        }

        public void Tick(float deltaTime)
        {
            _effectList.Foreach((effect, list, dt) =>
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
            }, _effectList, deltaTime);
        }
        
        public BaseObject FindEffect(ulong id)
        {
            return _effectList.ForeachReturn((effect) => effect.UniqueID == id);
        }

        public ulong PlayEffect(EffectCreateInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Path))
            {
                return 0;
            }

            var effect = new EffectObject(info);
            _effectList.Add(effect);
            return effect.UniqueID;
        }
        
        public void StopEffect(ulong id)
        {
            if (id == 0)
            {
                return;
            }
            
            var effect = _effectList.ForeachReturn((effect) => effect.UniqueID == id);
            effect?.Stop();
        }
    }
}