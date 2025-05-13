using UnityEngine;

namespace LiteQuark.Runtime
{
    public class RectTransformMoveAction : RectTransformBaseAction
    {
        public override string DebugName => $"<RectTransformMove>({RT.name},{_originPos}->{_targetPos},{_totalTime},{_easeKind})";

        private readonly Vector2 _position;
        private readonly float _totalTime;
        private readonly bool _isRelative;
        private readonly EaseKind _easeKind;
        
        private Vector2 _originPos;
        private Vector2 _targetPos;
        private float _currentTime;

        public RectTransformMoveAction(RectTransform transform, Vector2 position, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            _position = position;
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
            _originPos = GetValue();
            _targetPos = _isRelative ? _originPos + _position : _position;
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
            
            SetValue(Vector2.LerpUnclamped(_originPos, _targetPos, v));

            if (step >= 1)
            {
                SetValue(_targetPos);
                IsEnd = true;
            }
        }

        private Vector2 GetValue()
        {
            return RT.anchoredPosition;
        }
        
        private void SetValue(Vector2 value)
        {
            RT.anchoredPosition = value;
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder RectTransformMove(this ActionBuilder builder, RectTransform transform, Vector2 position, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new RectTransformMoveAction(transform, position, time, isRelative, easeKind));
            return builder;
        }
    }
}