using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformFadeAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformFade>({TS.name},{_beginAlpha}->{_endAlpha},{_totalTime},{_easeKind})";
        
        private readonly IAlphaBox _alphaBox;
        private readonly float _beginAlpha;
        private readonly float _endAlpha;
        private readonly float _totalTime;
        private readonly EaseKind _easeKind;
        
        private float _currentTime;

        public TransformFadeAction(Transform transform, float beginAlpha, float endAlpha, float time, EaseKind easeKind = EaseKind.Linear, IAlphaBox box = null)
            : base(transform)
        {
            _alphaBox = box ?? new AlphaBox(TS);
            _beginAlpha = beginAlpha;
            _endAlpha = endAlpha;
            _totalTime = MathUtils.ClampMinTime(time);
            _easeKind = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            _currentTime = 0f;
            _alphaBox.SetAlpha(_beginAlpha);
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
            _currentTime += deltaTime;
            var step = Mathf.Clamp01(_currentTime / _totalTime);
            var v = EaseUtils.Sample(_easeKind, step);
            
            _alphaBox.SetAlpha(Mathf.Lerp(_beginAlpha, _endAlpha, v));

            if (step >= 1f)
            {
                _alphaBox.SetAlpha(_endAlpha);
                IsEnd = true;
            }
        }
    }
    
    public class TransformFadeInAction : TransformFadeAction
    {
        public TransformFadeInAction(Transform transform, float time, EaseKind easeKind = EaseKind.Linear, IAlphaBox box = null)
            : base(transform, 0, 1, time, easeKind, box)
        {
        }
    }

    public class TransformFadeOutAction : TransformFadeAction
    {
        public TransformFadeOutAction(Transform transform, float time, EaseKind easeKind = EaseKind.Linear, IAlphaBox box = null)
            : base(transform, 1, 0, time, easeKind, box)
        {
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformFade(this ActionBuilder builder, Transform transform, float beginAlpha, float endAlpha, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformFadeAction(transform, beginAlpha, endAlpha, time, easeKind));
            return builder;
        }

        public static ActionBuilder TransformFadeIn(this ActionBuilder builder, Transform transform, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformFadeInAction(transform, time, easeKind));
            return builder;
        }

        public static ActionBuilder TransformFadeOut(this ActionBuilder builder, Transform transform, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformFadeOutAction(transform, time, easeKind));
            return builder;
        }
    }
}