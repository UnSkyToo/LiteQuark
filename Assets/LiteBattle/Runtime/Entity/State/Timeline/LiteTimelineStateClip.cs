using System;
using System.ComponentModel;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace LiteBattle.Runtime
{
    [DisplayName("State")]
    [Serializable]
    public sealed class LiteTimelineStateClip : PlayableAsset, ITimelineClipAsset
    {
        public TrackAsset Track { get; set; }

        [LiteProperty("事件", LitePropertyType.OptionalType)]
        [LiteOptionalType(typeof(ILiteEvent), "Event Data")]
        [SerializeReference]
        [HideInInspector]
        public ILiteEvent Event = new LiteNullEvent();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            // var clip = Playable.Create(graph);
            // return clip;
            var res = ScriptPlayable<LiteTimelineStateBehavior>.Create(graph);
            res.GetBehaviour().Event = Event;
            return res;
        }

        // public string GetDisplayName()
        // {
        //     return "Test";
        // }

        public ClipCaps clipCaps => ClipCaps.None;
    }
}