using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class ValueFromToAction<T> : BaseAction
    {
        public override string DebugName => $"<ValueFromTo>({_endValue},{_currentTime}/{_totalTime})";

        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private readonly T _endValue;
        private readonly float _totalTime = 0f;
        private readonly EaseKind _easeKind;
        
        private float _currentTime = 0f;
        private T _startValue;
        
        protected ValueFromToAction(Func<T> getter, Action<T> setter, T value, float time, EaseKind easeKind = EaseKind.Linear)
        {
            _getter = getter;
            _setter = setter;
            _endValue = value;
            _totalTime = MathUtils.ClampMinTime(time);
            _easeKind = easeKind;
        }

        public override void Tick(float deltaTime)
        {
            _currentTime += deltaTime;
            var step = Mathf.Clamp01(_currentTime / _totalTime);
            var v = EaseUtils.Sample(_easeKind, step);
            
            var value = LerpUnclamped(_startValue, _endValue, v);
            _setter.Invoke(value);

            if (step >= 1)
            {
                _setter.Invoke(_endValue);
                IsEnd = true;
            }
        }

        protected abstract T LerpUnclamped(T start, T end, float t);

        public override void Execute()
        {
            if (_getter == null || _setter == null)
            {
                IsEnd = true;
                return;
            }
            
            _currentTime = 0;
            _startValue = _getter.Invoke();
            IsEnd = false;
        }
    }

    public class FloatValueFromToAction : ValueFromToAction<float>
    {
        public FloatValueFromToAction(Func<float> getter, Action<float> setter, float value, float time, EaseKind easeKind = EaseKind.Linear)
            : base(getter, setter, value, time, easeKind)
        {
        }

        protected override float LerpUnclamped(float start, float end, float t)
        {
            return Mathf.LerpUnclamped(start, end, t);
        }
    }
    
    public class IntValueFromToAction : ValueFromToAction<int>
    {
        public IntValueFromToAction(Func<int> getter, Action<int> setter, int value, float time, EaseKind easeKind = EaseKind.Linear)
            : base(getter, setter, value, time, easeKind)
        {
        }

        protected override int LerpUnclamped(int start, int end, float t)
        {
            return Mathf.RoundToInt(start + (end - start) * t);
        }
    }
    
    public class ColorValueFromToAction : ValueFromToAction<Color>
    {
        public ColorValueFromToAction(Func<Color> getter, Action<Color> setter, Color value, float time, EaseKind easeKind = EaseKind.Linear)
            : base(getter, setter, value, time, easeKind)
        {
        }

        protected override Color LerpUnclamped(Color start, Color end, float t)
        {
            return Color.LerpUnclamped(start, end, t);
        }
    }
    
    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder ValueFromTo(this ActionBuilder builder, Func<float> getter, Action<float> setter, float value, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new FloatValueFromToAction(getter, setter, value, time, easeKind));
            return builder;
        }
        
        public static ActionBuilder ValueFromTo(this ActionBuilder builder, Func<int> getter, Action<int> setter, int value, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new IntValueFromToAction(getter, setter, value, time, easeKind));
            return builder;
        }
        
        public static ActionBuilder ValueFromTo(this ActionBuilder builder, Func<Color> getter, Action<Color> setter, Color value, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new ColorValueFromToAction(getter, setter, value, time, easeKind));
            return builder;
        }
    }
}