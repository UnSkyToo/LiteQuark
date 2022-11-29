using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public sealed class TaskSystem : ISystem, ITick
    {
        public UnityEngine.MonoBehaviour MonoBehaviourInstance { get; private set; }
        
        private readonly ListEx<ITask> TaskList_ = new ListEx<ITask>();
        private readonly object MainThreadLock_ = new object();
        private readonly ListEx<MainThreadTask> MainThreadTaskList_ = new ListEx<MainThreadTask>();

        public TaskSystem()
        {
            MonoBehaviourInstance = LiteRuntime.Instance.Launcher;
            TaskList_.Clear();
            MainThreadTaskList_.Clear();
        }

        public void Dispose()
        {
            MonoBehaviourInstance?.StopAllCoroutines();
            
            TaskList_.Foreach((task) => task.Dispose());
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
                if (!task.IsExecute)
                {
                    task.Execute();
                }
                
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
                    MainThreadTaskList_.Foreach((task) => { task?.Execute(); });
                    MainThreadTaskList_.Clear();
                }
            }
        }

        public CoroutineTask AddTask(IEnumerator taskFunc, Action callback = null)
        {
            var task = new CoroutineTask(taskFunc, callback);
            TaskList_.Add(task);
            return task;
        }

        // public IEnumerator WaitTask(IEnumerator taskFunc, Action callback = null)
        // {
        //     var task = new CoroutineTask(taskFunc, callback);
        //     TaskList_.Add(task);
        //     yield return MonoBehaviourInstance.StartCoroutine(task.Execute());
        // }

        public WriteFileAsyncTask AddTask(string filePath, byte[] data, Action<bool> callback)
        {
            var task = new WriteFileAsyncTask(filePath, data, callback);
            TaskList_.Add(task);
            return task;
        }

        public ReadFileAsyncTask AddTask(string filePath, Action<byte[]> callback)
        {
            var task = new ReadFileAsyncTask(filePath, callback);
            TaskList_.Add(task);
            return task;
        }

        public void AddMainThreadTask(Action<object> taskFunc, object param)
        {
            lock (MainThreadLock_)
            {
                MainThreadTaskList_.Add(new MainThreadTask(taskFunc, param));
            }
        }
    }
}