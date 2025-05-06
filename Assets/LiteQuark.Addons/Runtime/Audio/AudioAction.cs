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
            LiteRuntime.Get<AudioSystem>().PlaySound(Path_, IsLoop_, Limit_, Volume_);
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
            LiteRuntime.Get<AudioSystem>().MuteAllSound(IsMute_);
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
            LiteRuntime.Get<AudioSystem>().PlayMusic(Path_, IsLoop_, Volume_, IsOnly_);
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
            LiteRuntime.Get<AudioSystem>().MuteAllMusic(IsMute_);
        }
    }

    public static class ActionBuilderAudioExtend
    {
        public static ActionBuilder PlaySound(this ActionBuilder builder, string path, bool isLoop = false, int limit = 0, float volume = 1.0f)
        {
            builder.Add(new PlaySoundAction(path, isLoop, limit, volume));
            return builder;
        }

        public static ActionBuilder MuteAllSound(this ActionBuilder builder, bool isMute)
        {
            builder.Add(new MuteAllSoundAction(isMute));
            return builder;
        }

        public static ActionBuilder PlayMusic(this ActionBuilder builder, string path, bool isLoop = true, float volume = 1.0f, bool isOnly = true)
        {
            builder.Add(new PlayMusicAction(path, isLoop, volume, isOnly));
            return builder;
        }

        public static ActionBuilder MuteAllMusic(this ActionBuilder builder, bool isMute)
        {
            builder.Add(new MuteAllMusicAction(isMute));
            return builder;
        }
    }
}