using System;
using System.ComponentModel;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Runtime
{
    [DisplayName("State Marker")]
    [Serializable]
    public sealed class LiteTimelineStateMarker : Marker
    {
        [LiteProperty("事件", LitePropertyType.OptionalType)]
        [LiteOptionalType(typeof(ILiteEvent), "Event Data")]
        [SerializeReference]
        [HideInInspector]
        public ILiteEvent Event = new LiteNullEvent();
    }
}