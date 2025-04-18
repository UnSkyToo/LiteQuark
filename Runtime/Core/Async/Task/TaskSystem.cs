using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class TaskSystem : ISystem, ITick
    {
        private readonly UnityEngine.MonoBehaviour MonoBehaviourInstance_ = null;
        private readonly SynchronizationContext MainThreadSynchronizationContext_ = null;
        private readonly ListEx<ITask> TaskList_ = new ListEx<ITask>();

        public TaskSystem()
        {
            MonoBehaviourInstance_ = LiteRuntime.Instance.Launcher;
            MainThreadSynchronizationContext_ = SynchronizationContext.Current;
            TaskList_.Clear();
        }

        public Task<bool> Initialize()
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            MonoBehaviourInstance_?.StopAllCoroutines();
            
            TaskList_.Foreach((task) => task.Dispose());
            TaskList_.Clear();
        }

        public void Tick(float deltaTime)
        {
            TaskList_.Foreach((task, list, dt) =>
            {
                task.Tick(dt);

                if (task.IsDone)
                {
                    task.Dispose();
                    list.Remove(task);
                }
            }, TaskList_, deltaTime);
        }

        public UnityEngine.Coroutine StartCoroutine(IEnumerator routine)
        {
            return MonoBehaviourInstance_?.StartCoroutine(routine);
        }

        public void StopCoroutine(IEnumerator routine)
        {
            MonoBehaviourInstance_?.StopCoroutine(routine);
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

        public ParallelTask ParallelTask(ITask[] subTasks, Action<bool> callback)
        {
            var task = new ParallelTask(subTasks, callback);
            TaskList_.Add(task);
            return task;
        }

        public SequenceTask SequenceTask(ITask[] subTasks, Action<bool> callback)
        {
            var task = new SequenceTask(subTasks, callback);
            TaskList_.Add(task);
            return task;
        }
        
        public InstantiateGameObjectTask InstantiateGoTask(UnityEngine.GameObject template, UnityEngine.Transform parent, int count, Action<UnityEngine.GameObject[]> callback)
        {
            var task = new InstantiateGameObjectTask(template, parent, count, callback);
            TaskList_.Add(task);
            return task;
        }
        
        public UnityWebGetRequestTask UnityWebGetRequestTask(string uri, int timeout, bool forceNoCache, Action<UnityEngine.Networking.DownloadHandler> callback)
        {
            var task = new UnityWebGetRequestTask(uri, timeout, forceNoCache, callback);
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

        public LoadAssetBaseTask LoadAssetTask<T>(UnityEngine.AssetBundle bundle, string name, Action<UnityEngine.Object> callback) where T : UnityEngine.Object
        {
            var task = new LoadAssetTask<T>(bundle, name, callback);
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