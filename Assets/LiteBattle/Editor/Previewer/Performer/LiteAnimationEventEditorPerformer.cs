using LiteBattle.Runtime;

namespace LiteBattle.Editor
{
    [LiteEventEditorPerformer(typeof(LitePlayAnimationEvent))]
    public class LiteAnimationEventEditorPerformer : ILiteEventEditorPerformer
    {
        private int StartFrame_;
        private string AnimationName_;
        
        public void OnExecute(ILiteEvent evt, int frame)
        {
            if (evt is LitePlayAnimationEvent playAnimationEvent)
            {
                StartFrame_ = frame;
                AnimationName_ = playAnimationEvent.AnimationName;
            }
        }

        public void OnCancel()
        {
            AnimationName_ = string.Empty;
        }

        public void OnFrame(int frame)
        {
            var time = (float)LiteTimelineHelper.FrameToTime(frame - StartFrame_);
            LiteEditorBinder.Instance.SampleAnimation(AnimationName_, time);
        }
    }
}