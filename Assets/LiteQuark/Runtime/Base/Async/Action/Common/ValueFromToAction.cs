using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class ValueFromToAction<T> : BaseAction
    {
        public override string DebugName => $"<ValueFromTo>({EndValue_},{CurrentTime_}/{TotalTime_})";

        private readonly Func<T> Getter_;
        private readonly Action<T> Setter_;
        private readonly T EndValue_;
        private readonly float TotalTime_ = 0f;
        private readonly EaseKind EaseKind_;
        
        private float CurrentTime_ = 0f;
        private T StartValue_;
        
        protected ValueFromToAction(Func<T> getter, Action<T> setter, T value, float time, EaseKind easeKind = EaseKind.Linear)
        {
            Getter_ = getter;
            Setter_ = setter;
            EndValue_ = value;
            TotalTime_ = Mathf.Max(time, 0.01f);
            EaseKind_ = easeKind;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var step = Mathf.Clamp01(CurrentTime_ / TotalTime_);
            var v = EaseUtils.Sample(EaseKind_, step);
            
            var value = LerpUnclamped(StartValue_, EndValue_, v);
            Setter_.Invoke(value);

            if (step >= 1)
            {
                Setter_.Invoke(EndValue_);
                IsEnd = true;
            }
        }

        protected abstract T LerpUnclamped(T start, T end, float t);

        public override void Execute()
        {
            if (Getter_ == null || Setter_ == null)
            {
                IsEnd = true;
                return;
            }
            
            CurrentTime_ = 0;
            StartValue_ = Getter_.Invoke();
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
    }
}