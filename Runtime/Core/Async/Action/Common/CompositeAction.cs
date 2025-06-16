namespace LiteQuark.Runtime
{
    public abstract class CompositeAction : BaseAction
    {
        protected readonly string Tag;
        protected readonly int RepeatCount;
        protected readonly IAction[] SubActions;
        protected readonly int Count;

        protected CompositeAction(string tag, int repeatCount, IAction[] args)
        {
            Tag = string.IsNullOrEmpty(tag) ? "unknown" : tag;
            RepeatCount = repeatCount;
            SubActions = args ?? System.Array.Empty<IAction>();
            Count = args?.Length ?? 0;
            IsEnd = Count == 0;
        }
        
        public override void Dispose()
        {
            foreach (var action in SubActions)
            {
                action.Dispose();
            }
            
            base.Dispose();
        }
        
        public override void MarkSafety()
        {
            base.MarkSafety();

            foreach (var action in SubActions)
            {
                action.MarkSafety();
            }
        }
        
        public override void Stop()
        {
            foreach (var action in SubActions)
            {
                action.Stop();
            }
            
            base.Stop();
        }

        public IAction[] GetSubActions()
        {
            return SubActions;
        }
    }
}