namespace LiteQuark.Runtime
{
    public sealed class ActionSystem : ISystem, ITick
    {
        private readonly ListEx<IAction> ActionList_ = new ListEx<IAction>();
        
        public ActionSystem()
        {
            ActionList_.Clear();
        }

        public void Dispose()
        {
            ActionList_.Foreach((action) => action.Dispose());
            ActionList_.Clear();
        }

        public void Tick(float deltaTime)
        {
            ActionList_.Foreach((action, list, dt) =>
            {
                try
                {
                    if (action.IsEnd)
                    {
                        action.FinalCallback?.Invoke();
                        action.Dispose();
                        list.Remove(action);
                    }
                    else
                    {
                        action.Tick(dt);
                    }
                }
                catch (System.Exception ex)
                {
                    action.Stop();
                    LLog.Error($"action exception : {action.GetType().Name}");
                    LLog.Exception(ex);
                }
            }, ActionList_, deltaTime);
        }

        public bool IsIdle()
        {
            return ActionList_.Count == 0;
        }

        public bool IsEnd(ulong id)
        {
            var action = FindAction(id);
            return action == null || action.IsEnd;
        }

        public ListEx<IAction> GetActionList()
        {
            return ActionList_;
        }

        public IAction FindAction(ulong id)
        {
            return ActionList_.ForeachReturn((action, targetId) => action.ID == targetId, id);
        }

        public void StopAction(ulong id)
        {
            var action = FindAction(id);
            if (action == null || action.IsEnd)
            {
                return;
            }
            action.Stop();
        }

        public ulong AddAction(IAction action, bool isSafety = false)
        {
            if (isSafety)
            {
                action.MarkSafety();
            }
            
            ActionList_.Add(action);
            action.Execute();
            return action.ID;
        }

        public ulong AddBuilder(ActionBuilder builder, bool isSafety = false)
        {
            return AddAction(builder.Flush(), isSafety);
        }

        public ActionBuilder Sequence(string tag, bool isRepeat = false)
        {
            return ActionBuilder.Sequence(tag, isRepeat);
        }

        public ActionBuilder Parallel(string tag)
        {
            return ActionBuilder.Parallel(tag);
        }
    }
}