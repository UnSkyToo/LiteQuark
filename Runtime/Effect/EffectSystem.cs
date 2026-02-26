using System;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class EffectSystem : ISystem, ITick
    {
        private readonly Action<EffectObject, SafeList<EffectObject>, float> _onTickDelegate = null;
        private readonly SafeList<EffectObject> _effectList = new SafeList<EffectObject>();
        
        public EffectSystem()
        {
            _effectList.Clear();
            _onTickDelegate = OnEffectTick;
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
            _effectList.Foreach(_onTickDelegate, _effectList, deltaTime);
        }
        
        private void OnEffectTick(EffectObject effect, SafeList<EffectObject> list, float dt)
        {
            if (effect.IsDone)
            {
                effect.Dispose();
                list.Remove(effect);
            }
            else
            {
                effect.Tick(dt);
            }
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