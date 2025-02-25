using LiteBattle.Runtime;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    [CustomTimelineEditor(typeof(LiteTimelineStateClip))]
    internal sealed class LiteTimelineStateClipEditor : ClipEditor
    {
        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            clip.displayName = "State Clip";
            clip.duration = 0.5f;
            if (clip.asset is LiteTimelineStateClip stateClip)
            {
                stateClip.Track = track;
            }
            base.OnCreate(clip, track, clonedFrom);
        }

        public override void OnClipChanged(TimelineClip clip)
        {
            if(clip.asset is LiteTimelineStateClip stateClip)
            {
                if (stateClip.Event is LitePlayAnimationEvent playAnimationEvent)
                {
                    clip.start = 0;
                    
                    var stateLength = LiteEditorBinder.Instance.GetAnimatorStateLength(playAnimationEvent.AnimationName);
                    if (stateLength > 0)
                    {
                        clip.duration = stateLength;
                        LiteTimelineHelper.SetVisibleTimeRange(new Vector2(0, (float) clip.duration * 1.5f));
                    }
                    
                    clip.displayName = $"{GetCommonDisplayName(clip)} {playAnimationEvent.AnimationName}";
                }
                else if (stateClip.Event is LiteTransferEvent transferEvent)
                {
                    clip.displayName = $"{GetCommonDisplayName(clip)} {transferEvent.StateName}";
                }
                else
                {
                    clip.displayName = GetCommonDisplayName(clip);
                }
                // clip.displayName = stateClip.GetDisplayName();
            }
        }

        private string GetCommonDisplayName(TimelineClip clip)
        {
            return $"{LiteTimelineHelper.TimeToFrame(clip.start)}:{LiteTimelineHelper.TimeToFrame(clip.duration)}";
        }
    }
}