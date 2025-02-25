using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace LiteBattle.Runtime
{
    [TrackClipType(typeof(LiteTimelineStateClip))]
    [DisplayName("Lite State/Normal Track")]
    public sealed class LiteTimelineStateNormalTrack : TrackAsset
    {
        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var playable = base.CreatePlayable(graph, gameObject, clip);
            return playable;
        }
    }
}