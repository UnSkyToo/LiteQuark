using LiteBattle.Runtime;

namespace LiteBattle.Editor
{
    [LiteEventEditorPerformer(typeof(LitePlayAnimationEvent))]
    public class LiteAnimationEventEditorPerformer : ILiteEventEditorPerformer
    {
        private readonly LiteAgentBinder AgentBinder_;
        private string AnimationName_;
        
        public void OnExecute(ILiteEvent evt)
        {
            if (evt is LitePlayAnimationEvent playAnimationEvent)
            {
                AnimationName_ = playAnimationEvent.AnimationName;
            }
        }

        public void OnCancel()
        {
            AnimationName_ = string.Empty;
        }

        public void OnFrame(int frame)
        {
            var time = (float) LiteTimelineHelper.FrameToTime(frame);
            LiteAgentBinder.Instance.SampleAnimation(AnimationName_, time);
        }
    }
}