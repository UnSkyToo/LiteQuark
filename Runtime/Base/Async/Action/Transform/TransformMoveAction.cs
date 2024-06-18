using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformMoveAction : TransformBaseAction
    {
        public override string DebugName => $"<Transform{(IsLocal_ ? "Local" : "World")}Move>({TS_.name},{OriginPos_}->{TargetPos_},{TotalTime_},{EaseKind_})";

        private readonly Vector3 Position_;
        private readonly float TotalTime_;
        private readonly bool IsLocal_;
        private readonly bool IsRelative_;
        private readonly EaseKind EaseKind_;
        
        private Vector3 OriginPos_;
        private Vector3 TargetPos_;
        private float CurrentTime_;

        public TransformMoveAction(Transform transform, Vector3 position, float time, bool isLocal = true, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            Position_ = position;
            TotalTime_ = MathUtils.ClampMinTime(time);
            IsLocal_ = isLocal;
            IsRelative_ = isRelative;
            EaseKind_ = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            CurrentTime_ = 0;
            OriginPos_ = GetValue();
            TargetPos_ = IsRelative_ ? OriginPos_ + Position_ : Position_;
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
            CurrentTime_ += deltaTime;
            var step = Mathf.Clamp01(CurrentTime_ / TotalTime_);
            var v = EaseUtils.Sample(EaseKind_, step);
            
            SetValue(Vector3.LerpUnclamped(OriginPos_, TargetPos_, v));

            if (step >= 1)
            {
                SetValue(TargetPos_);
                IsEnd = true;
            }
        }

        private Vector3 GetValue()
        {
            return IsLocal_ ? TS_.localPosition : TS_.position;
        }
        
        private void SetValue(Vector3 value)
        {
            if (IsLocal_)
            {
                TS_.localPosition = value;
            }
            else
            {
                TS_.position = value;
            }
        }
    }
    
    public class TransformMovePathAction : TransformBaseAction
    {
        public override string DebugName => $"<Transform{(IsLocal_ ? "Local" : "World")}MovePath>({TS_.name},{StartPos_}->{TargetPos_},{TotalTime_},{EaseKind_})";

        private Vector3[] Paths_;
        private readonly float TotalTime_;
        private readonly float MoveSpeed_;
        private readonly bool IsLocal_;
        private readonly EaseKind EaseKind_;
        
        private int PathIndex_;
        private Vector3 StartPos_;
        private Vector3 TargetPos_;
        private float CurrentTime_;
        private float MoveTime_;

        public TransformMovePathAction(Transform transform, Vector3[] path, float time, bool isLocal = true, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            Paths_ = isRelative ? MathUtils.VectorListAdd(path, GetValue()) : path;
            MoveSpeed_ = MathUtils.VectorListLength(Paths_) / MathUtils.ClampMinTime(time);
            IsLocal_ = isLocal;
            EaseKind_ = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            IsEnd = Mathf.Approximately(MoveSpeed_, 0f);
            PathIndex_ = 0;
            MoveToNextPath();
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
            CurrentTime_ += deltaTime;
            var step = Mathf.Clamp01(CurrentTime_ / MoveTime_);
            var v = EaseUtils.Sample(EaseKind_, step);
            
            SetValue(Vector3.LerpUnclamped(StartPos_, TargetPos_, v));

            if (step >= 1)
            {
                SetValue(TargetPos_);
                MoveToNextPath();
            }
        }

        private Vector3 GetValue()
        {
            return IsLocal_ ? TS_.localPosition : TS_.position;
        }
        
        private void SetValue(Vector3 value)
        {
            if (IsLocal_)
            {
                TS_.localPosition = value;
            }
            else
            {
                TS_.position = value;
            }
        }
        
        private void MoveToNextPath()
        {
            if (PathIndex_ + 1 >= Paths_.Length)
            {
                IsEnd = true;
                return;
            }

            PathIndex_++;
            StartPos_ = Paths_[PathIndex_ - 1];
            TargetPos_ = Paths_[PathIndex_];
            CurrentTime_ = 0f;
            MoveTime_ = MathUtils.ClampMinTime(Vector3.Distance(StartPos_, TargetPos_) / MoveSpeed_);
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