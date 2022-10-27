using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public class TaskManager : Singleton<TaskManager>, IManager, ITick
    {
        public UnityEngine.MonoBehaviour MonoBehaviourInstance { get; private set; }
        
        private readonly ListEx<TaskItem> TaskList_ = new ListEx<TaskItem>();
        private readonly object MainThreadLock_ = new object();
        private readonly ListEx<MainThreadTaskEntity> MainThreadTaskList_ = new ListEx<MainThreadTaskEntity>();

        public bool Startup()
        {
            MonoBehaviourInstance = LiteRuntime.Instance.MonoBehaviourInstance;
            TaskList_.Clear();
            MainThreadTaskList_.Clear();
            return true;
        }

        public void Shutdown()
        {
            MonoBehaviourInstance?.StopAllCoroutines();
            
            foreach (var entity in TaskList_)
            {
                entity.Dispose();
            }
            TaskList_.Clear();

            lock (MainThreadLock_)
            {
                MainThreadTaskList_.Clear();
            }
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

            if (MainThreadTaskList_.Count > 0)
            {
                lock (MainThreadLock_)
                {
                    MainThreadTaskList_.Foreach((task) => { task?.Invoke(); });
                    MainThreadTaskList_.Clear();
                }
            }
        }

        public TaskItem AddTask(IEnumerator taskFunc, Action callback = null)
        {
            var task = new TaskItem(taskFunc, callback);
            TaskList_.Add(task);
            MonoBehaviourInstance.StartCoroutine(task.Execute());
            return task;
        }

        public IEnumerator WaitTask(IEnumerator taskFunc, Action callback = null)
        {
            var task = new TaskItem(taskFunc, callback);
            TaskList_.Add(task);
            yield return MonoBehaviourInstance.StartCoroutine(task.Execute());
        }

        public void AddMainThreadTask(Action<object> taskFunc, object param)
        {
            lock (MainThreadLock_)
            {
                MainThreadTaskList_.Add(new MainThreadTaskEntity(taskFunc, param));
            }
        }
    }
}