using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformFadeAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformFade>({TS_.name},{BeginAlpha_}->{EndAlpha_},{TotalTime_},{EaseKind_})";
        
        private readonly IAlphaBox AlphaBox_;
        private readonly float BeginAlpha_;
        private readonly float EndAlpha_;
        private readonly float TotalTime_;
        private readonly EaseKind EaseKind_;
        
        private float CurrentTime_;

        public TransformFadeAction(Transform transform, float beginAlpha, float endAlpha, float time, EaseKind easeKind = EaseKind.Linear, IAlphaBox box = null)
            : base(transform)
        {
            AlphaBox_ = box ?? new AlphaBox(TS_);
            BeginAlpha_ = beginAlpha;
            EndAlpha_ = endAlpha;
            TotalTime_ = Mathf.Max(time, 0.01f);
            EaseKind_ = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            CurrentTime_ = 0f;
            AlphaBox_.SetAlpha(BeginAlpha_);
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
            
            AlphaBox_.SetAlpha(Mathf.Lerp(BeginAlpha_, EndAlpha_, v));

            if (step >= 1f)
            {
                AlphaBox_.SetAlpha(EndAlpha_);
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