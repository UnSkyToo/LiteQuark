namespace LiteQuark.Runtime
{
    public abstract class CompositeAction : BaseAction
    {
        protected readonly string Tag_;
        protected readonly IAction[] SubActions_;
        protected readonly int Count_;

        protected CompositeAction(string tag, params IAction[] args)
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

        public IAction[] GetSubActions()
        {
            return SubActions_;
        }
    }
}