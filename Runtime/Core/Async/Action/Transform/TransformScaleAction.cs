using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformScaleAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformScale>({TS.name},{_originScale}->{_targetScale},{_totalTime},{_easeKind})";
        
        private readonly Vector3 _scale;
        private readonly float _totalTime;
        private readonly bool _isRelative;
        private readonly EaseKind _easeKind;
        
        private Vector3 _originScale;
        private Vector3 _targetScale;
        private float _currentTime;

        public TransformScaleAction(Transform transform, Vector3 scale, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            _scale = scale;
            _totalTime = MathUtils.ClampMinTime(time);
            _isRelative = isRelative;
            _easeKind = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            _currentTime = 0;
            _originScale = TS.localScale;
            _targetScale = _isRelative ? _originScale + _scale : _scale;
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
            
            TS.localScale = Vector3.LerpUnclamped(_originScale, _targetScale, v);

            if (step >= 1)
            {
                TS.localScale = _targetScale;
                IsEnd = true;
            }
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformScale(this ActionBuilder builder, Transform transform, Vector3 scale, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformScaleAction(transform, scale, time, isRelative, easeKind));
            return builder;
        }
    }
}