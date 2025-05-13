namespace LiteQuark.Runtime
{
    public class WaitTimeAction : BaseAction
    {
        public override string DebugName => $"<WaitTime>({_currentTime}/{_waitTime})";

        private readonly float _waitTime = 0f;
        private float _currentTime = 0f;
        
        public WaitTimeAction(float waitTime)
        {
            _waitTime = waitTime;
        }

        public override void Tick(float deltaTime)
        {
            _currentTime -= deltaTime;
            if (_currentTime <= 0f)
            {
                IsEnd = true;
            }
        }

        public override void Execute()
        {
            _currentTime = _waitTime;
            IsEnd = _currentTime <= 0f;
        }
    }

    public class WaitFrameAction : BaseAction
    {
        public override string DebugName => $"<WaitFrame>({_currentFrame}/{_waitFrame})";

        private readonly int _waitFrame = 0;
        private int _currentFrame = 0;
        
        public WaitFrameAction(int waitFrame)
        {
            _waitFrame = waitFrame;
        }

        public override void Tick(float deltaTime)
        {
            _currentFrame--;
            if (_currentFrame <= 0)
            {
                IsEnd = true;
            }
        }

        public override void Execute()
        {
            _currentFrame = _waitFrame;
            IsEnd = _currentFrame <= 0;
        }
    }

    public class WaitUntilAction : BaseAction
    {
        public override string DebugName => $"<WaitUntil>({_conditionFunc})";
        
        private readonly System.Func<bool> _conditionFunc = null;

        public WaitUntilAction(System.Func<bool> conditionFunc)
        {
            _conditionFunc = conditionFunc;
        }

        public override void Execute()
        {
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            IsEnd = _conditionFunc?.Invoke() ?? true;
        }
    }
    
    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder WaitTime(this ActionBuilder builder, float time)
        {
            builder.Add(new WaitTimeAction(time));
            return builder;
        }
        
        public static ActionBuilder WaitFrame(this ActionBuilder builder, int frame)
        {
            builder.Add(new WaitFrameAction(frame));
            return builder;
        }
        
        public static ActionBuilder WaitUntil(this ActionBuilder builder, System.Func<bool> conditionFunc)
        {
            builder.Add(new WaitUntilAction(conditionFunc));
            return builder;
        }
    }
}