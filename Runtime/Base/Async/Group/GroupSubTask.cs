using System;

namespace LiteQuark.Runtime
{
    public class GroupSubTask : IDispose
    {
        protected readonly GroupMainTask MainTask_;
        protected readonly Action<GroupSubTask> TaskFunc_;

        public GroupSubTask(GroupMainTask mainTask, Action<GroupSubTask> taskFunc)
        {
            MainTask_ = mainTask;
            TaskFunc_ = taskFunc;
        }

        public virtual void Dispose()
        {
        }

        public virtual void Execute()
        {
            TaskFunc_?.Invoke(this);
        }

        public void Done()
        {
            MainTask_?.ItemDone();
        }
    }

    public sealed class GroupSubTask<T> : GroupSubTask
    {
        private readonly Action<GroupSubTask, T> NewTaskFunc_;
        private readonly T Param_;

        public GroupSubTask(GroupMainTask mainTask, Action<GroupSubTask, T> taskFunc, T param)
            : base(mainTask, null)
        {
            NewTaskFunc_ = taskFunc;
            Param_ = param;
        }

        public override void Execute()
        {
            NewTaskFunc_?.Invoke(this, Param_);
        }
    }

    public sealed class GroupSubTask<T1, T2> : GroupSubTask
    {
        private readonly Action<GroupSubTask, T1, T2> NewTaskFunc_;
        private readonly T1 Param1_;
        private readonly T2 Param2_;

        public GroupSubTask(GroupMainTask mainTask, Action<GroupSubTask, T1, T2> taskFunc, T1 param1, T2 param2)
            : base(mainTask, null)
        {
            NewTaskFunc_ = taskFunc;
            Param1_ = param1;
            Param2_ = param2;
        }

        public override void Execute()
        {
            NewTaskFunc_?.Invoke(this, Param1_, Param2_);
        }
    }

    public sealed class GroupWaitTimeSubTask : GroupSubTask
    {
        private readonly float WaitTime_;
        private ulong TimerID_;

        public GroupWaitTimeSubTask(GroupMainTask mainTask, float waitTime)
            : base(mainTask, null)
        {
        }

        public override void Dispose()
        {
            if (TimerID_ != 0)
            {
                LiteRuntime.Timer.StopTimer(TimerID_);
                TimerID_ = 0;
            }

            base.Dispose();
        }

        public override void Execute()
        {
            TimerID_ = LiteRuntime.Timer.AddTimer(WaitTime_, Done, 1);
        }
    }

    public sealed class GroupWaitConditionSubTask : GroupSubTask
    {
        private readonly Func<bool> ConditionFunc_;
        private ulong TimerID_;

        public GroupWaitConditionSubTask(GroupMainTask mainTask, Func<bool> conditionFunc)
            : base(mainTask, null)
        {
            ConditionFunc_ = conditionFunc;
        }

        public override void Dispose()
        {
            if (TimerID_ != 0)
            {
                LiteRuntime.Timer.StopTimer(TimerID_);
                TimerID_ = 0;
            }

            base.Dispose();
        }

        public override void Execute()
        {
            TimerID_ = LiteRuntime.Timer.AddTimer(0, TickFunc);
        }

        private void TickFunc()
        {
            if (ConditionFunc_?.Invoke() ?? true)
            {
                Done();
            }
        }
    }
}