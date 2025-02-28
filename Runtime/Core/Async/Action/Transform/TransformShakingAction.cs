using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformShakingAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformShaking>({TS_.name},{TotalTime_},{Strength_},{Vibrato_},{Randomness_})";

        private readonly float TotalTime_;
        private readonly float PerStepTime_;
        private readonly float Strength_;
        private readonly int Vibrato_;
        private readonly float Randomness_;

        private float Degress_;
        private Vector3 Position_;
        private float CurrentTime_;
        private float StepTime_;

        public TransformShakingAction(Transform transform, float time, float strength, int vibrato, float randomness = 90.0f)
            : base(transform)
        {
            Position_ = TS_.localPosition;
            TotalTime_ = MathUtils.ClampMinTime(time);
            PerStepTime_ = 1.0f / vibrato;
            Strength_ = strength;
            Vibrato_ = vibrato;
            Randomness_ = randomness;
        }
        
        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            Position_ = TS_.localPosition;
            CurrentTime_ = 0;
            StepTime_ = 0;
            Degress_ = Random.Range(0f, 360f);
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
            CurrentTime_ += deltaTime;
            StepTime_ += deltaTime;

            if (StepTime_ >= PerStepTime_)
            {
                Shake();
                StepTime_ -= PerStepTime_;
            }

            if (CurrentTime_ >= TotalTime_)
            {
                TS_.localPosition = Position_;
                IsEnd = true;
            }
        }

        private void Shake()
        {
            Degress_ = Degress_ - 180f + Random.Range(-Randomness_, Randomness_);
            
            var quaternion = Quaternion.AngleAxis(Random.Range(-Randomness_, Randomness_), Vector3.up);
            TS_.localPosition = Position_ + quaternion * MathUtils.Vector3FromAngle(Degress_, Strength_);
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformShaking(this ActionBuilder builder, Transform transform, float time, float strength, int vibrato, float randomness = 90.0f)
        {
            builder.Add(new TransformShakingAction(transform, time, strength, vibrato, randomness));
            return builder;
        }
    }
}