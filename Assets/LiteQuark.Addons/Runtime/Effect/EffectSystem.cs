using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class EffectSystem : ISystem, ITick
    {
        private readonly SafeList<EffectObject> _effectList = new();

        public EffectSystem()
        {
        }

        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
        }

        public UniTask DisposeAsync()
        {
            foreach (var effect in _effectList)
            {
                effect.Dispose();
            }
            _effectList.Clear();

            return UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
            foreach (var effect in _effectList)
            {
                if (effect.IsDone)
                {
                    effect.Dispose();
                    _effectList.Remove(effect);
                }
                else
                {
                    effect.Tick(deltaTime);
                }
            }
        }

        public EffectObject FindEffect(ulong id)
        {
            foreach (var effect in _effectList)
            {
                if (effect.UniqueID == id)
                {
                    return effect;
                }
            }

            return null;
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

            var effect = FindEffect(id);
            effect?.Stop();
        }
    }
}
