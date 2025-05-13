using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class ActionSystem : ISystem, ITick
    {
        private readonly ListEx<IAction> _actionList = new ListEx<IAction>();
        
        public ActionSystem()
        {
            _actionList.Clear();
        }
        
        public Task<bool> Initialize()
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            _actionList.Foreach((action) => action.Dispose());
            _actionList.Clear();
        }

        public void Tick(float deltaTime)
        {
            _actionList.Foreach(OnActionTick, _actionList, deltaTime);
        }

        private void OnActionTick(IAction action, ListEx<IAction> list, float dt)
        {
            try
            {
                if (action.IsEnd)
                {
                    action.FinalCallback?.Invoke(action);
                    action.Dispose();
                    list.Remove(action);
                }
                else
                {
                    action.Tick(dt);
                }
            }
            catch
            {
                action.Stop();
                throw;
            }
        }

        public bool IsIdle()
        {
            return _actionList.Count == 0;
        }

        public bool IsEnd(ulong id)
        {
            var action = FindAction(id);
            return action == null || action.IsEnd;
        }

        public ListEx<IAction> GetActionList()
        {
            return _actionList;
        }

        public IAction FindAction(ulong id)
        {
            if (id == 0)
            {
                return null;
            }
            
            return _actionList.ForeachReturn((action, targetId) => action.ID == targetId, id);
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
            if (isSafety || LiteRuntime.Setting.Action.SafetyMode)
            {
                action.MarkSafety();
            }
            
            _actionList.Add(action);
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