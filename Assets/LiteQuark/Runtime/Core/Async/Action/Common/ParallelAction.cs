namespace LiteQuark.Runtime
{
    public class ParallelAction : CompositeAction
    {
        public override string DebugName => $"<Parallel - {Tag}>({Count})";
        
        public ParallelAction(string tag, IAction[] args)
            : base(tag, args)
        {
        }

        public override void Execute()
        {
            IsEnd = Count == 0;
            
            foreach (var action in SubActions)
            {
                action.Execute();
            }
        }

        public override void Tick(float deltaTime)
        {
            var endCount = 0;

            for (var index = 0; index < Count; ++index)
            {
                if (SubActions[index].IsEnd)
                {
                    endCount++;
                    continue;
                }
                
                SubActions[index].Tick(deltaTime);

                if (SubActions[index].IsEnd)
                {
                    SubActions[index].Dispose();
                }
            }
            
            if (endCount == Count)
            {
                IsEnd = true;
            }
        }
    }
}