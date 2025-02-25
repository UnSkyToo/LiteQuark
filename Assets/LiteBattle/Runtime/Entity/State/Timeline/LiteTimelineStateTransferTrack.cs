using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace LiteBattle.Runtime
{
    [TrackClipType(typeof(LiteTimelineStateClip))]
    [TrackColor(0.9f, 0.4f, 0.1f)]
    [DisplayName("Lite State/Transfer Track")]
    public sealed class LiteTimelineStateTransferTrack : TrackAsset
    {
        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var playable = base.CreatePlayable(graph, gameObject, clip);
            return playable;
        }
    }
}