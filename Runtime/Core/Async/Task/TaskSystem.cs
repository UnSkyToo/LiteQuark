using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class TaskSystem : ISystem, ITick
    {
        public int RunningTaskCount { get; private set; } = 0;

        private readonly UnityEngine.MonoBehaviour _monoBehaviourInstance = null;
        private readonly SynchronizationContext _mainThreadSynchronizationContext = null;
        private readonly Action<ITask, SafeList<ITask>, float> _onTickDelegate = null;
        private readonly SafeList<ITask> _taskList = new SafeList<ITask>();

        public TaskSystem()
        {
            _monoBehaviourInstance = LiteRuntime.Instance.Launcher;
            _mainThreadSynchronizationContext = SynchronizationContext.Current;
            _onTickDelegate = OnTaskTick;
            _taskList.Clear();
        }

        public UniTask<bool> Initialize()
        {
            RunningTaskCount = 0;
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            _monoBehaviourInstance?.StopAllCoroutines();
            
            _taskList.Foreach((task) => task.Dispose());
            _taskList.Clear();
            
            RunningTaskCount = 0;
        }

        public void Tick(float deltaTime)
        {
            _taskList.Foreach(_onTickDelegate, _taskList, deltaTime);
        }

        private void OnTaskTick(ITask task, SafeList<ITask> list, float dt)
        {
            if (task.State == TaskState.Pending)
            {
                try
                {
                    RunningTaskCount++;
                    task.Execute();
                }
                catch (Exception ex)
                {
                    LLog.Error("Task {0} execute failed: {1}", task.GetType().Name, ex);
                    task.Cancel();
                }
            }
            
            if (task.State == TaskState.InProgress)
            {
                try
                {
                    task.Tick(dt);
                }
                catch (Exception ex)
                {
                    LLog.Error("Task {0} tick failed: {1}", task.GetType().Name, ex);
                    task.Cancel();
                }
            }
            
            if (task.IsDone)
            {
                task.Dispose();
                list.Remove(task);
                RunningTaskCount--;
            }
        }
        
        public SafeList<ITask> GetTaskList()
        {
            return _taskList;
        }

        public UnityEngine.Coroutine StartCoroutine(IEnumerator routine)
        {
            return _monoBehaviourInstance?.StartCoroutine(routine);
        }

        public void StopCoroutine(IEnumerator routine)
        {
            _monoBehaviourInstance?.StopCoroutine(routine);
        }

        public void AddTask(ITask task)
        {
            _taskList.Add(task);
        }

        public CoroutineTask AddTask(IEnumerator taskFunc, Action callback = null)
        {
            var task = new CoroutineTask(taskFunc, callback);
            AddTask(task);
            return task;
        }

        public AsyncOperationTask AddTask(UnityEngine.AsyncOperation asyncOperation, Action callback = null)
        {
            var task = new AsyncOperationTask(asyncOperation, callback);
            AddTask(task);
            return task;
        }

        public AsyncResultTask AddTask(IAsyncResult asyncResult, Action callback = null)
        {
            var task = new AsyncResultTask(asyncResult, callback);
            AddTask(task);
            return task;
        }

        public WaitCallbackTask AddTask(Action<Action<bool>> func)
        {
            var task = new WaitCallbackTask(func);
            AddTask(task);
            return task;
        }
        
        public InstantiateGameObjectTask InstantiateGoTask(UnityEngine.GameObject template, UnityEngine.Transform parent, int count, Action<UnityEngine.GameObject[]> callback)
        {
            var task = new InstantiateGameObjectTask(template, parent, count, callback);
            AddTask(task);
            return task;
        }
        
        public UnityWebGetRequestTask UnityWebGetRequestTask(string uri, int timeout, bool forceNoCache, Action<UnityEngine.Networking.DownloadHandler> callback)
        {
            var task = new UnityWebGetRequestTask(uri, timeout, forceNoCache, callback);
            AddTask(task);
            return task;
        }
        
        internal LoadVersionPackTask LoadVersionPackTask(string uri, Action<VersionPackInfo> callback)
        {
            var task = new LoadVersionPackTask(uri, callback);
            AddTask(task);
            return task;
        }

        public LoadLocalBundleTask LoadLocalBundleTask(string bundleUri, Action<UnityEngine.AssetBundle> callback)
        {
            var task = new LoadLocalBundleTask(bundleUri, callback);
            AddTask(task);
            return task;
        }

        public LoadRemoteBundleTask LoadRemoteBundleTask(string bundleUri, Action<UnityEngine.AssetBundle> callback)
        {
            var task = new LoadRemoteBundleTask(bundleUri, callback);
            AddTask(task);
            return task;
        }

        public LoadAssetBaseTask LoadAssetTask<T>(UnityEngine.AssetBundle bundle, string name, Action<UnityEngine.Object> callback) where T : UnityEngine.Object
        {
            var task = new LoadAssetTask<T>(bundle, name, callback);
            AddTask(task);
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
            _mainThreadSynchronizationContext?.Post(callback, state);
        }

        public void SendMainThreadTask(SendOrPostCallback callback, object state)
        {
            _mainThreadSynchronizationContext?.Send(callback, state);
        }
    }
}