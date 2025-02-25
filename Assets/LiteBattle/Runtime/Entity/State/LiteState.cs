using System.Collections.Generic;

namespace LiteBattle.Runtime
{
    public sealed class LiteState
    {
        public string Name { get; }
        public LiteAgent Agent => Machine.Agent;
        public LiteStateMachine Machine { get; private set; }
        public float Duration { get; }
        
        private readonly List<LiteClip> Clips_ = new List<LiteClip>();
        private readonly List<LiteClip> SignalClips_ = new List<LiteClip>();
        private readonly List<LiteClip> DurationClips_ = new List<LiteClip>();
        private float PreviousTime_ = 0f;
        private float CurrentTime_ = 0f;

        public LiteState(LiteStateData stateData)
        {
            Name = stateData.Name;
            Clips_.AddRange(stateData.Clips);

            foreach (var clip in Clips_)
            {
                Duration = LiteMathHelper.Max(Duration, clip.Start + clip.Duration);
            }
        }

        public float GetCurrentTime()
        {
            return CurrentTime_;
        }
        
        public bool IsEnd()
        {
            return CurrentTime_ >= Duration;
        }

        public void Enter(LiteStateMachine machine)
        {
            Machine = machine;
            
            SignalClips_.Clear();
            DurationClips_.Clear();
            
            CurrentTime_ = 0;
            PreviousTime_ = 0;
            
            // LiteLog.Info($"Enter {Name}");
        }

        public void Leave()
        {
            foreach (var clip in SignalClips_)
            {
                clip.Leave(this);
            }
            SignalClips_.Clear();

            foreach (var clip in DurationClips_)
            {
                clip.Leave(this);
            }
            DurationClips_.Clear();
        }
        
        public void Tick(float deltaTime)
        {
            PreviousTime_ = CurrentTime_;
            CurrentTime_ += deltaTime;
            
            ProcessClip(deltaTime);
        }

        private void ProcessClip(float deltaTime)
        {
            foreach (var clip in Clips_)
            {
                if (clip.Start >= PreviousTime_ && clip.Start <= CurrentTime_)
                {
                    if (TriggerClip(clip, deltaTime) == LiteEventSignal.Break)
                    {
                        return;
                    }
                }
            }

            var removeList = new List<LiteClip>();
            foreach (var clip in DurationClips_)
            {
                if (clip.InRange(CurrentTime_))
                {
                    if (clip.Tick(this, deltaTime) == LiteEventSignal.Break)
                    {
                        return;
                    }
                }
                else
                {
                    removeList.Add(clip);
                }
            }

            foreach (var clip in removeList)
            {
                clip.Leave(this);
                DurationClips_.Remove(clip);
            }
        }

        private LiteEventSignal TriggerClip(LiteClip clip, float deltaTime)
        {
            if (SignalClips_.Contains(clip) || DurationClips_.Contains(clip))
            {
                return LiteEventSignal.Continue;
            }
            
            clip.Enter(this);
            
            switch (clip.Kind)
            {
                case LiteClipKind.Signal:
                    SignalClips_.Add(clip);
                    return clip.Tick(this, deltaTime);
                case LiteClipKind.Duration:
                    DurationClips_.Add(clip);
                    break;
            }

            return LiteEventSignal.Continue;
        }
    }
}