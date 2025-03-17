using LiteBattle.Runtime;
using UnityEngine;

namespace LiteBattle.Editor
{
    [LiteEventEditorPerformer(typeof(LitePlayEffectEvent))]
    public class LiteEffectEventEditorPerformer : ILiteEventEditorPerformer
    {
        private int StartFrame_;
        private string EffectPath_;
        private Vector3 EffectPosition_;
        
        public void OnExecute(ILiteEvent evt, int frame)
        {
            if (evt is LitePlayEffectEvent playEffectEvent)
            {
                StartFrame_ = frame;
                EffectPath_ = playEffectEvent.EffectPath;
                EffectPosition_ = playEffectEvent.EffectPosition;
            }
        }
        
        public void OnCancel()
        {
            LiteEditorBinder.Instance.StopEffect(EffectPath_);
            EffectPath_ = string.Empty;
        }

        public void OnFrame(int frame)
        {
            var time = (float)LiteTimelineHelper.FrameToTime(frame - StartFrame_);
            LiteEditorBinder.Instance.SampleEffect(EffectPath_, EffectPosition_, time);
        }
    }
}