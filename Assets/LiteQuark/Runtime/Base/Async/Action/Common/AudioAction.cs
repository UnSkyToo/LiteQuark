namespace LiteQuark.Runtime
{
    public class PlaySoundAction : BaseAction
    {
        public override string DebugName => $"<PlaySound>({Path_},{IsLoop_},{Limit_},{Volume_})";

        private readonly string Path_;
        private readonly bool IsLoop_;
        private readonly int Limit_;
        private readonly float Volume_;
        
        public PlaySoundAction(string path, bool isLoop, int limit, float volume)
        {
            Path_ = path;
            IsLoop_ = isLoop;
            Limit_ = limit;
            Volume_ = volume;
        }

        public override void Execute()
        {
            IsEnd = true;
            LiteRuntime.Audio.PlaySound(Path_, IsLoop_, Limit_, Volume_);
        }
    }

    public class MuteAllSoundAction : BaseAction
    {
        public override string DebugName => $"<MuteAllSoundAction>({IsMute_})";
        
        private readonly bool IsMute_;
        
        public MuteAllSoundAction(bool isMute)
        {
            IsMute_ = isMute;
        }

        public override void Execute()
        {
            IsEnd = true;
            LiteRuntime.Audio.MuteAllSound(IsMute_);
        }
    }
    
    public class PlayMusicAction : BaseAction
    {
        public override string DebugName => $"<PlayMusic>({Path_},{IsLoop_},{Volume_},{IsOnly_})";

        private readonly string Path_;
        private readonly bool IsLoop_;
        private readonly float Volume_;
        private readonly bool IsOnly_;
        
        public PlayMusicAction(string path, bool isLoop, float volume, bool isOnly)
        {
            Path_ = path;
            IsLoop_ = isLoop;
            Volume_ = volume;
            IsOnly_ = isOnly;
        }

        public override void Execute()
        {
            IsEnd = true;
            LiteRuntime.Audio.PlayMusic(Path_, IsLoop_, Volume_, IsOnly_);
        }
    }
    
    public class MuteAllMusicAction : BaseAction
    {
        public override string DebugName => $"<MuteAllMusicAction>({IsMute_})";
        
        private readonly bool IsMute_;
        
        public MuteAllMusicAction(bool isMute)
        {
            IsMute_ = isMute;
        }

        public override void Execute()
        {
            IsEnd = true;
            LiteRuntime.Audio.MuteAllMusic(IsMute_);
        }
    }
}