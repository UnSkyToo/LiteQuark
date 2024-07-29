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
            TaskList_.Foreach((task, list, dt) =>
            {
                if (task.State == TaskState.Waiting)
                {
                    task.Execute();
                }

                if (task.State == TaskState.InProgress)
                {
                    task.Tick(dt);
                }

                if (task.State == TaskState.Completed)
                {
                    task.Dispose();
                    list.Remove(task);
                }
            }, TaskList_, deltaTime);
            
            lock (MainThreadLock_)
            {
                if (MainThreadTaskList_.Count > 0)
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

        public InstantiateGameObjectTask AddTask(UnityEngine.GameObject template, UnityEngine.Transform parent, int count, Action<UnityEngine.GameObject[]> callback)
        {
            var task = new InstantiateGameObjectTask(template, parent, count, callback);
            TaskList_.Add(task);
            return task;
        }

        public PipelineTask AddTask(IPipelineSubTask[] subTasks)
        {
            var task = new PipelineTask(subTasks);
            TaskList_.Add(task);
            return task;
        }

        // public IEnumerator WaitTask(IEnumerator taskFunc, Action callback = null)
        // {
        //     var task = new CoroutineTask(taskFunc, callback);
        //     TaskList_.Add(task);
        //     yield return MonoBehaviourInstance.StartCoroutine(task.Execute());
        // }

        // public WriteFileAsyncTask AddTask(string filePath, byte[] data, Action<bool> callback)
        // {
        //     var task = new WriteFileAsyncTask(filePath, data, callback);
        //     TaskList_.Add(task);
        //     return task;
        // }
        //
        // public ReadFileAsyncTask AddTask(string filePath, Action<byte[]> callback)
        // {
        //     var task = new ReadFileAsyncTask(filePath, callback);
        //     TaskList_.Add(task);
        //     return task;
        // }

        public void AddMainThreadTask(Action<object> taskFunc, object param)
        {
            lock (MainThreadLock_)
            {
                MainThreadTaskList_.Add(new MainThreadTask(taskFunc, param));
            }
        }
    }
}