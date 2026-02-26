namespace LiteQuark.Runtime
{
    public class PlaySoundAction : BaseAction
    {
        public override string DebugName => $"<PlaySound>({_path},{_isLoop},{_limit},{_volume})";

        private readonly string _path;
        private readonly bool _isLoop;
        private readonly int _limit;
        private readonly float _volume;
        
        public PlaySoundAction(string path, bool isLoop, int limit, float volume)
        {
            _path = path;
            _isLoop = isLoop;
            _limit = limit;
            _volume = volume;
        }

        public override void Execute()
        {
            IsDone = true;
            LiteRuntime.Get<AudioSystem>().PlaySound(_path, _isLoop, _limit, _volume);
        }
    }

    public class MuteAllSoundAction : BaseAction
    {
        public override string DebugName => $"<MuteAllSoundAction>({_isMute})";
        
        private readonly bool _isMute;
        
        public MuteAllSoundAction(bool isMute)
        {
            _isMute = isMute;
        }

        public override void Execute()
        {
            IsDone = true;
            LiteRuntime.Get<AudioSystem>().MuteAllSound(_isMute);
        }
    }
    
    public class PlayMusicAction : BaseAction
    {
        public override string DebugName => $"<PlayMusic>({_path},{_isLoop},{_volume},{_isOnly})";

        private readonly string _path;
        private readonly bool _isLoop;
        private readonly float _volume;
        private readonly bool _isOnly;
        
        public PlayMusicAction(string path, bool isLoop, float volume, bool isOnly)
        {
            _path = path;
            _isLoop = isLoop;
            _volume = volume;
            _isOnly = isOnly;
        }

        public override void Execute()
        {
            IsDone = true;
            LiteRuntime.Get<AudioSystem>().PlayMusic(_path, _isLoop, _volume, _isOnly);
        }
    }
    
    public class MuteAllMusicAction : BaseAction
    {
        public override string DebugName => $"<MuteAllMusicAction>({_isMute})";
        
        private readonly bool _isMute;
        
        public MuteAllMusicAction(bool isMute)
        {
            _isMute = isMute;
        }

        public override void Execute()
        {
            IsDone = true;
            LiteRuntime.Get<AudioSystem>().MuteAllMusic(_isMute);
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