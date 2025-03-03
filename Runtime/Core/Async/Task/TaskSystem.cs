using System;
using System.Collections;
using System.Threading;

namespace LiteQuark.Runtime
{
    public sealed class TaskSystem : ISystem, ITick
    {
        public UnityEngine.MonoBehaviour MonoBehaviourInstance { get; private set; }
        
        private readonly ListEx<ITask> TaskList_ = new ListEx<ITask>();
        private readonly object MainThreadLock_ = new object();
        // private readonly ListEx<MainThreadTask> MainThreadTaskList_ = new ListEx<MainThreadTask>();
        private readonly SynchronizationContext MainThreadSynchronizationContext_ = null;

        public TaskSystem()
        {
            MonoBehaviourInstance = LiteRuntime.Instance.Launcher;
            TaskList_.Clear();
            // MainThreadTaskList_.Clear();
            MainThreadSynchronizationContext_ = SynchronizationContext.Current;
        }

        public void Initialize(Action<bool> callback)
        {
            callback?.Invoke(true);
        }

        public void Dispose()
        {
            MonoBehaviourInstance?.StopAllCoroutines();
            
            TaskList_.Foreach((task) => task.Dispose());
            TaskList_.Clear();

            // lock (MainThreadLock_)
            // {
            //     MainThreadTaskList_.Clear();
            // }
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

                if (task.State is TaskState.Completed or TaskState.Aborted)
                {
                    task.Dispose();
                    list.Remove(task);
                }
            }, TaskList_, deltaTime);
            
            // lock (MainThreadLock_)
            // {
            //     if (MainThreadTaskList_.Count > 0)
            //     {
            //         MainThreadTaskList_.Foreach((task) => { task?.Execute(); });
            //         MainThreadTaskList_.Clear();
            //     }
            // }
        }

        public void AddTask(ITask task)
        {
            TaskList_.Add(task);
        }

        public CoroutineTask AddTask(IEnumerator taskFunc, Action callback = null)
        {
            var task = new CoroutineTask(taskFunc, callback);
            TaskList_.Add(task);
            return task;
        }

        public AsyncOperationTask AddTask(UnityEngine.AsyncOperation asyncOperation, Action callback = null)
        {
            var task = new AsyncOperationTask(asyncOperation, callback);
            TaskList_.Add(task);
            return task;
        }

        public AsyncResultTask AddTask(IAsyncResult asyncResult, Action callback = null)
        {
            var task = new AsyncResultTask(asyncResult, callback);
            TaskList_.Add(task);
            return task;
        }

        public WaitCallbackTask AddTask(Action<Action<bool>> func)
        {
            var task = new WaitCallbackTask(func);
            TaskList_.Add(task);
            return task;
        }

        public PipelineTask AddPipelineTask(ITask[] subTasks, Action<bool> callback)
        {
            var task = new PipelineTask(subTasks, callback);
            TaskList_.Add(task);
            return task;
        }
        
        public InstantiateGameObjectTask InstantiateGoTask(UnityEngine.GameObject template, UnityEngine.Transform parent, int count, Action<UnityEngine.GameObject[]> callback)
        {
            var task = new InstantiateGameObjectTask(template, parent, count, callback);
            TaskList_.Add(task);
            return task;
        }

        public AsyncOperationTask LoadSceneTask(string scenePath, UnityEngine.SceneManagement.LoadSceneMode mode, Action callback)
        {
            var task = new AsyncOperationTask(UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scenePath, mode), callback);
            TaskList_.Add(task);
            return task;
        }
        
        public UnityWebGetRequestTask UnityWebGetRequestTask(string uri, Action<UnityEngine.Networking.DownloadHandler> callback)
        {
            var task = new UnityWebGetRequestTask(uri, callback);
            TaskList_.Add(task);
            return task;
        }

        public LoadLocalBundleTask LoadLocalBundleTask(string bundleUri, Action<UnityEngine.AssetBundle> callback)
        {
            var task = new LoadLocalBundleTask(bundleUri, callback);
            TaskList_.Add(task);
            return task;
        }

        public LoadRemoteBundleTask LoadRemoteBundleTask(string bundleUri, Action<UnityEngine.AssetBundle> callback)
        {
            var task = new LoadRemoteBundleTask(bundleUri, callback);
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

        // public void AddMainThreadTask(Action<object> taskFunc, object param)
        // {
        //     lock (MainThreadLock_)
        //     {
        //         MainThreadTaskList_.Add(new MainThreadTask(taskFunc, param));
        //     }
        // }
        
        public void PostMainThreadTask(SendOrPostCallback callback, object state)
        {
            MainThreadSynchronizationContext_?.Post(callback, state);
        }

        public void SendMainThreadTask(SendOrPostCallback callback, object state)
        {
            MainThreadSynchronizationContext_?.Send(callback, state);
        }
    }
}