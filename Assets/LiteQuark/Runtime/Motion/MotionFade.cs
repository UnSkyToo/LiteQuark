using UnityEngine;

namespace LiteQuark.Runtime
{
    public class MotionFade : BaseMotion
    {
        private readonly float TotalTime_;
        private readonly float BeginAlpha_;
        private readonly float EndAlpha_;
        private float CurrentTime_;

        private MotionAlphaBox AlphaBox_;

        public MotionFade(float time, float beginAlpha, float endAlpha)
            : base()
        {
            TotalTime_ = time;
            BeginAlpha_ = beginAlpha;
            EndAlpha_ = endAlpha;
            CurrentTime_ = 0;
        }

        public override void Enter()
        {
            IsEnd = false;
            CurrentTime_ = 0;
            AlphaBox_ = new MotionAlphaBox(Master);
            AlphaBox_.SetAlpha(BeginAlpha_);
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var t = CurrentTime_ / TotalTime_;
            if (t >= 1.0f)
            {
                AlphaBox_.SetAlpha(EndAlpha_);
                IsEnd = true;
                return;
            }

            AlphaBox_.SetAlpha(Mathf.Lerp(BeginAlpha_, EndAlpha_, t));
        }
    }

    public class MotionFadeIn : MotionFade
    {
        public MotionFadeIn(float time)
            : base(time, 0, 1)
        {
        }
    }

    public class MotionFadeOut : MotionFade
    {
        public MotionFadeOut(float time)
            : base(time, 1, 0)
        {
        }
    }
}