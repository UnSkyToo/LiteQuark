namespace LiteQuark.Runtime
{
    public abstract class CompositeAction : BaseAction
    {
        protected readonly string Tag;
        protected readonly int RepeatCount;
        protected readonly IAction[] SubActions;
        protected readonly int SubActionCount;

        protected CompositeAction(string tag, int repeatCount, IAction[] args)
        {
            Tag = string.IsNullOrEmpty(tag) ? "unknown" : tag;
            RepeatCount = repeatCount;
            SubActions = args ?? System.Array.Empty<IAction>();
            SubActionCount = args?.Length ?? 0;
            IsDone = SubActionCount == 0;
        }
        
        public override void Dispose()
        {
            foreach (var action in SubActions)
            {
                action.Dispose();
            }
            
            base.Dispose();
        }
        
        public override void MarkAsSafe()
        {
            base.MarkAsSafe();

            foreach (var action in SubActions)
            {
                action.MarkAsSafe();
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