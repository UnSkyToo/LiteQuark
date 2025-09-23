using System;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class ActionSystem : ISystem, ITick
    {
        private readonly Action<IAction, SafeList<IAction>, float> _onTickDelegate;
        private readonly SafeList<IAction> _actionList = new SafeList<IAction>();
        
        public ActionSystem()
        {
            _onTickDelegate = OnActionTick;
            _actionList.Clear();
        }
        
        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            _actionList.Foreach((action) => action.Dispose());
            _actionList.Clear();
        }

        public void Tick(float deltaTime)
        {
            _actionList.Foreach(_onTickDelegate, _actionList, deltaTime);
        }

        private void OnActionTick(IAction action, SafeList<IAction> list, float dt)
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

        public SafeList<IAction> GetActionList()
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
            OnActionTick(action, _actionList, 0);
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

        public ActionBuilder Sequence(string tag, int repeatCount = 1)
        {
            return ActionBuilder.Sequence(tag, repeatCount);
        }

        public ActionBuilder Parallel(string tag, int repeatCount = 1)
        {
            return ActionBuilder.Parallel(tag, repeatCount);
        }
    }
}