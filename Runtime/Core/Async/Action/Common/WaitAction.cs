namespace LiteQuark.Runtime
{
    public class WaitTimeAction : BaseAction
    {
        public override string DebugName => $"<WaitTime>({CurrentTime_}/{WaitTime_})";

        private readonly float WaitTime_ = 0f;
        private float CurrentTime_ = 0f;
        
        public WaitTimeAction(float waitTime)
        {
            WaitTime_ = waitTime;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ -= deltaTime;
            if (CurrentTime_ <= 0f)
            {
                IsEnd = true;
            }
        }

        public override void Execute()
        {
            CurrentTime_ = WaitTime_;
            IsEnd = CurrentTime_ <= 0f;
        }
    }

    public class WaitFrameAction : BaseAction
    {
        public override string DebugName => $"<WaitFrame>({CurrentFrame_}/{WaitFrame_})";

        private readonly int WaitFrame_ = 0;
        private int CurrentFrame_ = 0;
        
        public WaitFrameAction(int waitFrame)
        {
            WaitFrame_ = waitFrame;
        }

        public override void Tick(float deltaTime)
        {
            CurrentFrame_--;
            if (CurrentFrame_ <= 0)
            {
                IsEnd = true;
            }
        }

        public override void Execute()
        {
            CurrentFrame_ = WaitFrame_;
            IsEnd = CurrentFrame_ <= 0;
        }
    }

    public class WaitUntilAction : BaseAction
    {
        public override string DebugName => $"<WaitUntil>({ConditionFunc_})";
        
        private readonly System.Func<bool> ConditionFunc_ = null;

        public WaitUntilAction(System.Func<bool> conditionFunc)
        {
            ConditionFunc_ = conditionFunc;
        }

        public override void Execute()
        {
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            IsEnd = ConditionFunc_?.Invoke() ?? true;
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