using System;
using UnityEngine.Playables;

namespace LiteBattle.Runtime
{
    [Serializable]
    public sealed class LiteTimelineStateBehavior : PlayableBehaviour
    {
        public static event Action<double> OnTimeChange;
        
        public ILiteEvent Event;
        
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // var progress = playable.GetTime() / playable.GetDuration();
            // Debug.Log(Event.GetType() + " " + progress);
            if (Event is LitePlayAnimationEvent)
            {
                OnTimeChange?.Invoke(playable.GetTime());
            }
        }

        public override void OnGraphStart(Playable playable)
        {
        }

        public override void OnGraphStop(Playable playable)
        {
        }
    }
}