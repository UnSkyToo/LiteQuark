using System;

namespace LiteQuark.Runtime
{
    public sealed class GroupSystem : ISystem, ITick
    {
        private static readonly ListEx<GroupMainTask> TaskList_ = new ListEx<GroupMainTask>();

        public GroupSystem()
        {
        }
        
        public void Initialize(Action<bool> callback)
        {
            callback?.Invoke(true);
        }

        public void Dispose()
        {
            TaskList_.Foreach((task) => task.Dispose());
            TaskList_.Clear();
        }

        public void Tick(float deltaTime)
        {
            TaskList_.Foreach((task) =>
            {
                if (task.IsEnd)
                {
                    task.Dispose();
                    TaskList_.Remove(task);
                }
            });
        }

        public GroupMainTask CreateSequenceGroup(Action callback)
        {
            var task = new GroupMainTask(false, callback);
            TaskList_.Add(task);
            return task;
        }

        public GroupMainTask CreateParallelGroup(Action callback)
        {
            var task = new GroupMainTask(true, callback);
            TaskList_.Add(task);
            return task;
        }
    }
}