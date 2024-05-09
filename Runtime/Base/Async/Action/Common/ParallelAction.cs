namespace LiteQuark.Runtime
{
    public class ParallelAction : BaseAction
    {
        public override string DebugName => $"<Parallel - {Tag_}>({Count_})";

        private readonly string Tag_;
        private readonly IAction[] SubActions_;
        private readonly int Count_;
        
        public ParallelAction(string tag, params IAction[] args)
        {
            Tag_ = string.IsNullOrEmpty(tag) ? "unknown" : tag;
            SubActions_ = args ?? System.Array.Empty<IAction>();
            Count_ = args?.Length ?? 0;
            IsEnd = Count_ == 0;
        }

        public override void Dispose()
        {
            foreach (var action in SubActions_)
            {
                action.Dispose();
            }
            
            base.Dispose();
        }

        public override void MarkSafety()
        {
            base.MarkSafety();

            foreach (var action in SubActions_)
            {
                action.MarkSafety();
            }
        }

        public override void Stop()
        {
            foreach (var action in SubActions_)
            {
                action.Stop();
            }
            
            base.Stop();
        }

        public override void Execute()
        {
            IsEnd = Count_ == 0;
            
            foreach (var action in SubActions_)
            {
                action.Execute();
            }
        }

        public override void Tick(float deltaTime)
        {
            var endCount = 0;

            for (var index = 0; index < Count_; ++index)
            {
                if (SubActions_[index].IsEnd)
                {
                    endCount++;
                    continue;
                }
                
                SubActions_[index].Tick(deltaTime);

                if (SubActions_[index].IsEnd)
                {
                    SubActions_[index].Dispose();
                }
            }
            
            if (endCount == Count_)
            {
                IsEnd = true;
            }
        }
    }
}