namespace LiteQuark.Runtime
{
    public class ParallelAction : CompositeAction
    {
        public override string DebugName => $"<Parallel - {Tag_}>({Count_})";
        
        public ParallelAction(string tag, IAction[] args)
            : base(tag, args)
        {
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