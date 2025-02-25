using LiteBattle.Runtime;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    [CustomTimelineEditor(typeof(LiteTimelineStateNormalTrack))]
    public class LiteTimelineStateNormalTrackEditor : TrackEditor
    {
        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            track.name = "Normal Track";
            base.OnCreate(track, copiedFrom);
        }
    }
    
    [CustomTimelineEditor(typeof(LiteTimelineStateTransferTrack))]
    public class LiteTimelineStateTransferTrackEditor : TrackEditor
    {
        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            track.name = "Transfer Track";
            base.OnCreate(track, copiedFrom);
        }
    }
}