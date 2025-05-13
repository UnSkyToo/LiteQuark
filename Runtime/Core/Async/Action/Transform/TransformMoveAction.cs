using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformMoveAction : TransformBaseAction
    {
        public override string DebugName => $"<Transform{(_isLocal ? "Local" : "World")}Move>({TS.name},{_originPos}->{_targetPos},{_totalTime},{_easeKind})";

        private readonly Vector3 _position;
        private readonly float _totalTime;
        private readonly bool _isLocal;
        private readonly bool _isRelative;
        private readonly EaseKind _easeKind;
        
        private Vector3 _originPos;
        private Vector3 _targetPos;
        private float _currentTime;

        public TransformMoveAction(Transform transform, Vector3 position, float time, bool isLocal = true, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            _position = position;
            _totalTime = MathUtils.ClampMinTime(time);
            _isLocal = isLocal;
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
            
            SetValue(Vector3.LerpUnclamped(_originPos, _targetPos, v));

            if (step >= 1)
            {
                SetValue(_targetPos);
                IsEnd = true;
            }
        }

        private Vector3 GetValue()
        {
            return _isLocal ? TS.localPosition : TS.position;
        }
        
        private void SetValue(Vector3 value)
        {
            if (_isLocal)
            {
                TS.localPosition = value;
            }
            else
            {
                TS.position = value;
            }
        }
    }
    
    public class TransformMovePathAction : TransformBaseAction
    {
        public override string DebugName => $"<Transform{(_isLocal ? "Local" : "World")}MovePath>({TS.name},{_startPos}->{_targetPos},{_totalTime},{_easeKind})";

        private readonly Vector3[] _paths;
        private readonly float _totalTime;
        private readonly float _moveSpeed;
        private readonly bool _isLocal;
        private readonly EaseKind _easeKind;
        
        private int _pathIndex;
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _currentTime;
        private float _moveTime;

        public TransformMovePathAction(Transform transform, Vector3[] path, float time, bool isLocal = true, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            _paths = isRelative ? MathUtils.VectorListAdd(path, GetValue()) : path;
            _moveSpeed = MathUtils.VectorListLength(_paths) / MathUtils.ClampMinTime(time);
            _isLocal = isLocal;
            _easeKind = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            IsEnd = Mathf.Approximately(_moveSpeed, 0f);
            _pathIndex = 0;
            MoveToNextPath();
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
            ProcessMove(deltaTime);
        }

        private void ProcessMove(float time)
        {
            _currentTime += time;
            var step = Mathf.Clamp01(_currentTime / _moveTime);
            var v = EaseUtils.Sample(_easeKind, step);
            
            SetValue(Vector3.LerpUnclamped(_startPos, _targetPos, v));

            if (step >= 1)
            {
                var remainTime = _currentTime - _moveTime;
                SetValue(_targetPos);
                MoveToNextPath();

                if (!IsEnd && remainTime > 0)
                {
                    ProcessMove(remainTime);
                }
            }
        }

        private Vector3 GetValue()
        {
            return _isLocal ? TS.localPosition : TS.position;
        }
        
        private void SetValue(Vector3 value)
        {
            if (_isLocal)
            {
                TS.localPosition = value;
            }
            else
            {
                TS.position = value;
            }
        }
        
        private void MoveToNextPath()
        {
            if (_pathIndex + 1 >= _paths.Length)
            {
                IsEnd = true;
                return;
            }

            _pathIndex++;
            _startPos = _paths[_pathIndex - 1];
            _targetPos = _paths[_pathIndex];
            _currentTime = 0f;
            _moveTime = MathUtils.ClampMinTime(Vector3.Distance(_startPos, _targetPos) / _moveSpeed);
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformLocalMove(this ActionBuilder builder, Transform transform, Vector3 position, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformMoveAction(transform, position, time, true, isRelative, easeKind));
            return builder;
        }

        public static ActionBuilder TransformWorldMove(this ActionBuilder builder, Transform transform, Vector3 position, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformMoveAction(transform, position, time, false, isRelative, easeKind));
            return builder;
        }

        public static ActionBuilder TransformLocalMovePath(this ActionBuilder builder, Transform transform, Vector3[] path, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformMovePathAction(transform, path, time, true, isRelative, easeKind));
            return builder;
        }

        public static ActionBuilder TransformWorldMovePath(this ActionBuilder builder, Transform transform, Vector3[] path, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformMovePathAction(transform, path, time, false, isRelative, easeKind));
            return builder;
        }
    }
}