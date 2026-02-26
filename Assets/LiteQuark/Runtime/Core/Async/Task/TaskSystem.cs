using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class TaskSystem : ISystem, ITick
    {
        public int RunningTaskCount { get; private set; } = 0;
        public int PendingTaskCount { get; private set; } = 0;

        private readonly UnityEngine.MonoBehaviour _monoBehaviourInstance = null;
        private readonly SynchronizationContext _mainThreadSynchronizationContext = null;
        private readonly Action<ITask, SafeList<ITask>, float> _onTickDelegate = null;
        private readonly SafeList<ITask> _taskList = new SafeList<ITask>();
        private readonly int _concurrencyLimit = 20;
        private readonly TaskPriority _ignoreLimitPriority = TaskPriority.High;

        public TaskSystem()
        {
            _monoBehaviourInstance = LiteRuntime.Instance.Launcher;
            _mainThreadSynchronizationContext = SynchronizationContext.Current;
            _onTickDelegate = OnTaskTick;
            _concurrencyLimit = Math.Max(1, LiteRuntime.Setting.Task.ConcurrencyLimit);
            _ignoreLimitPriority = LiteRuntime.Setting.Task.IgnoreLimitPriority;
        }

        public UniTask<bool> Initialize()
        {
            RunningTaskCount = 0;
            PendingTaskCount = 0;
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            _monoBehaviourInstance?.StopAllCoroutines();
            
            _taskList.Foreach((task) =>
            {
                task.Cancel();
                task.Dispose();
            });
            _taskList.Clear();
            
            RunningTaskCount = 0;
            PendingTaskCount = 0;
        }

        public void Tick(float deltaTime)
        {
            _taskList.Foreach(_onTickDelegate, _taskList, deltaTime);
        }

        private void OnTaskTick(ITask task, SafeList<ITask> list, float dt)
        {
            if (task.State == TaskState.Pending)
            {
                if (RunningTaskCount < _concurrencyLimit || task.Priority >= _ignoreLimitPriority)
                {
                    RunningTaskCount++;
                    PendingTaskCount--;
                    
                    try
                    {
                        task.Execute();
                    }
                    catch (Exception ex)
                    {
                        LLog.Error("Task {0} execute failed: {1}", task.GetType().Name, ex);
                        task.Cancel();
                    }
                }
                else
                {
                    return;
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

                if (task.IsExecuted)
                {
                    RunningTaskCount = Math.Max(0, RunningTaskCount - 1);
                }
                else
                {
                    PendingTaskCount = Math.Max(0, PendingTaskCount - 1);
                }
            }
        }
        
        internal SafeList<ITask> GetTaskList()
        {
            return _taskList;
        }

        public UnityEngine.Coroutine StartCoroutine(IEnumerator routine)
        {
            return _monoBehaviourInstance?.StartCoroutine(routine);
        }

        public void StopCoroutine(UnityEngine.Coroutine routine)
        {
            _monoBehaviourInstance?.StopCoroutine(routine);
        }

        public void AddTask(ITask task)
        {
            _taskList.Add(task);
            PendingTaskCount++;
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

        public MainThreadTask AddMainThreadTask(Action<object> func, object param)
        {
            var task = new MainThreadTask(func, param);
            AddTask(task);
            return task;
        }
        
        public InstantiateGameObjectTask AddInstantiateGoTask(UnityEngine.GameObject template, UnityEngine.Transform parent, int count, Action<UnityEngine.GameObject[]> callback)
        {
            var task = new InstantiateGameObjectTask(template, parent, count, callback);
            AddTask(task);
            return task;
        }
        
        public UnityWebGetRequestTask AddUnityWebGetRequestTask(string uri, RetryParam retry, bool forceNoCache, Action<UnityEngine.Networking.DownloadHandler> callback)
        {
            var task = new UnityWebGetRequestTask(uri, retry, forceNoCache, callback);
            AddTask(task);
            return task;
        }
        
        internal LoadVersionPackTask AddLoadVersionPackTask(string uri, Action<VersionPackInfo> callback)
        {
            var task = new LoadVersionPackTask(uri, callback);
            AddTask(task);
            return task;
        }

        internal LoadLocalBundleTask AddLoadLocalBundleTask(string bundleUri, Action<UnityEngine.AssetBundle> callback)
        {
            var task = new LoadLocalBundleTask(bundleUri, callback);
            AddTask(task);
            return task;
        }

        internal LoadRemoteBundleTask AddLoadRemoteBundleTask(string bundleUri, string hash, Action<UnityEngine.AssetBundle> callback)
        {
            var task = new LoadRemoteBundleTask(bundleUri, hash, callback);
            AddTask(task);
            return task;
        }

        internal LoadAssetTask<T> AddLoadAssetTask<T>(UnityEngine.AssetBundle bundle, string assetName, Action<UnityEngine.Object> callback) where T : UnityEngine.Object
        {
            var task = new LoadAssetTask<T>(bundle, assetName, callback);
            AddTask(task);
            return task;
        }

        internal LoadSceneTask AddLoadSceneTask(string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            var task = new LoadSceneTask(sceneName, parameters, callback);
            AddTask(task);
            return task;
        }
        
        internal LoadResourceTask<T> AddLoadResourceTask<T>(string assetName, Action<T> callback) where T : UnityEngine.Object
        {
            var task = new LoadResourceTask<T>(assetName, callback);
            AddTask(task);
            return task;
        }

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