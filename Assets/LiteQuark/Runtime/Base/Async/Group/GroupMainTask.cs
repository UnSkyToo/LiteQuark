using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class GroupMainTask : IDispose
    {
        public bool IsParallel { get; }
        public bool IsEnd { get; private set; }

        private readonly List<GroupSubTask> TaskList_;
        private readonly Action Callback_;
        private int DoneCount_;

        public GroupMainTask(bool isParallel, Action callback)
        {
            IsParallel = isParallel;
            Callback_ = callback;

            TaskList_ = new List<GroupSubTask>();
            DoneCount_ = 0;
            
            IsEnd = false;
        }
        
        public void Dispose()
        {
            foreach (var task in TaskList_)
            {
                task.Dispose();
            }
            TaskList_.Clear();
        }

        public void Execute()
        {
            IsEnd = false;
            DoneCount_ = 0;

            if (TaskList_.Count == 0)
            {
                ItemDone();
                return;
            }

            if (IsParallel)
            {
                foreach (var task in TaskList_)
                {
                    task.Execute();
                }
            }
            else
            {
                TaskList_[DoneCount_].Execute();
            }
        }

        public void ItemDone()
        {
            DoneCount_++;

            if (DoneCount_ >= TaskList_.Count)
            {
                IsEnd = true;
                Callback_?.Invoke();
                return;
            }

            if (!IsParallel)
            {
                TaskList_[DoneCount_].Execute();
            }
        }

        public GroupSubTask CreateTask(Action<GroupSubTask> taskFunc)
        {
            var task = new GroupSubTask(this, taskFunc);
            TaskList_.Add(task);
            return task;
        }

        public GroupSubTask<T> CreateTask<T>(Action<GroupSubTask, T> taskFunc, T param)
        {
            var task = new GroupSubTask<T>(this, taskFunc, param);
            TaskList_.Add(task);
            return task;
        }

        public GroupSubTask<T1, T2> CreateTask<T1, T2>(Action<GroupSubTask, T1, T2> taskFunc, T1 param1, T2 param2)
        {
            var task = new GroupSubTask<T1, T2>(this, taskFunc, param1, param2);
            TaskList_.Add(task);
            return task;
        }

        public GroupWaitTimeSubTask CreateWaitTask(float waitTime)
        {
            var task = new GroupWaitTimeSubTask(this, waitTime);
            TaskList_.Add(task);
            return task;
        }

        public GroupWaitConditionSubTask CreateWaitTask(Func<bool> conditionFunc)
        {
            var task = new GroupWaitConditionSubTask(this, conditionFunc);
            TaskList_.Add(task);
            return task;
        }
    }
}