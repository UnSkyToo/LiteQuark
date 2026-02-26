using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformRotateAction : TransformBaseAction
    {
        public override string DebugName => $"<Transform{(_isLocal ? "Local" : "World")}Rotate>({TS.name},{_originRotation}->{_targetRotation},{_totalTime},{_easeKind})";

        private readonly Quaternion _rotation;
        private readonly float _totalTime;
        private readonly bool _isLocal;
        private readonly EaseKind _easeKind;
        
        private Quaternion _originRotation;
        private Quaternion _targetRotation;
        private float _currentTime;

        public TransformRotateAction(Transform transform, Quaternion rotation, float time, bool isLocal = true, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            _rotation = rotation;
            _totalTime = MathUtils.ClampMinTime(time);
            _isLocal = isLocal;
            _easeKind = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            _currentTime = 0;
            _originRotation = TS.localRotation;
            _targetRotation = _rotation;
            IsDone = false;
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
            
            SetValue(Quaternion.LerpUnclamped(_originRotation, _targetRotation, v));

            if (step >= 1)
            {
                SetValue(_targetRotation);
                IsDone = true;
            }
        }
        
        private void SetValue(Quaternion value)
        {
            if (_isLocal)
            {
                TS.localRotation = value;
            }
            else
            {
                TS.rotation = value;
            }
        }
    }

    public class TransformRotateAroundAction : TransformBaseAction
    {
        public override string DebugName => $"<Transform{(IsLocal ? "Local" : "World")}RotateAround>({TS.name},{_center},{_axis},{_totalAngle},{_totalTime})";

        private readonly Vector3 _center;
        private readonly Vector3 _axis;
        private readonly float _totalAngle;
        private readonly float _totalTime;
        protected readonly bool IsLocal;

        private float _anglePerSecond;
        private float _accumulateAngle;
        private float _currentTime;

        public TransformRotateAroundAction(Transform transform, Vector3 center, Vector3 axis, float angle, float time, bool isLocal = true)
            : base(transform)
        {
            _center = center;
            _axis = axis;
            _totalAngle = angle;
            _totalTime = MathUtils.ClampMinTime(time);
            IsLocal = isLocal;
        }

        public override void Execute()
        {
            _currentTime = 0;
            _anglePerSecond = _totalAngle / _totalTime;
            _accumulateAngle = 0f;
            IsDone = false;
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
            _currentTime += deltaTime;
            var angle = CalculateNextAngle(deltaTime);

            if (IsLocal)
            {
                var vector3 = Quaternion.AngleAxis(angle, _axis) * (TS.localPosition - _center);
                TS.localPosition = _center + vector3;
            }
            else
            {
                TS.RotateAround(_center, _axis, angle);
            }
            
            if (_currentTime >= _totalTime)
            {
                IsDone = true;
            }
        }

        private float CalculateNextAngle(float deltaTime)
        {
            var angle = deltaTime * _anglePerSecond;
            
            if (_totalAngle > 0 && _accumulateAngle + angle > _totalAngle)
            {
                angle = _totalAngle - _accumulateAngle;
                _accumulateAngle = _totalAngle;
            }
            else if (_totalAngle < 0 && _accumulateAngle + angle < _totalAngle)
            {
                angle = _totalAngle - _accumulateAngle;
                _accumulateAngle = _totalAngle;
            }
            else
            {
                _accumulateAngle += angle;
            }
            
            return angle;
        }
    }

    public class TransformTargetRotateAroundAction : TransformRotateAroundAction
    {
        private readonly Vector3 _targetPosition;
        
        public TransformTargetRotateAroundAction(Transform transform, Vector3 center, Vector3 axis, Vector3 targetPosition, float angle, float time, bool isLocal = true)
            : base(transform, center, axis, angle, time, isLocal)
        {
            _targetPosition = targetPosition;
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
            if (IsLocal)
            {
                if (Vector3.Distance(TS.localPosition, _targetPosition) < 0.1f)
                {
                    TS.localPosition = _targetPosition;
                    IsDone = true;
                }
            }
            else
            {
                if (Vector3.Distance(TS.position, _targetPosition) < 0.1f)
                {
                    TS.position = _targetPosition;
                    IsDone = true;
                }
            }

            if (!IsDone)
            {
                base.Tick(deltaTime);
            }
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformRotate(this ActionBuilder builder, Transform transform, Quaternion rotation, float time, bool isLocal = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformRotateAction(transform, rotation, time, isLocal, easeKind));
            return builder;
        }

        public static ActionBuilder TransformLocalRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, float angle, float time)
        {
            builder.Add(new TransformRotateAroundAction(transform, center, axis, angle, time, true));
            return builder;
        }

        public static ActionBuilder TransformWorldRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, float angle, float time)
        {
            builder.Add(new TransformRotateAroundAction(transform, center, axis, angle, time, false));
            return builder;
        }

        public static ActionBuilder TransformLocalTargetRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, Vector3 targetPosition, float angle, float time)
        {
            builder.Add(new TransformTargetRotateAroundAction(transform, center, axis, targetPosition, angle, time, true));
            return builder;
        }

        public static ActionBuilder TransformWorldTargetRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, Vector3 targetPosition, float angle, float time)
        {
            builder.Add(new TransformTargetRotateAroundAction(transform, center, axis, targetPosition, angle, time, false));
            return builder;
        }
    }
}