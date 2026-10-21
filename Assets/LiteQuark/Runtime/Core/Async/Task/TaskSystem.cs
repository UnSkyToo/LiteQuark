using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class TaskSystem : ISystem, ITick
    {
        public const int DefaultGroupId = 0;

        public int PendingTaskCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _groups.Count; i++)
                {
                    count += _groups[i].PendingTaskCount;
                }
                return count;
            }
        }

        public int RunningTaskCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _groups.Count; i++)
                {
                    count += _groups[i].RunningTaskCount;
                }
                return count;
            }
        }

        private readonly UnityEngine.MonoBehaviour _monoBehaviourInstance = null;
        private readonly SynchronizationContext _mainThreadSynchronizationContext = null;

        private readonly List<TaskGroup> _groups = new();
        private readonly List<TaskRecord> _records = new();

        public TaskSystem()
        {
            _monoBehaviourInstance = LiteRuntime.Instance.Launcher;
            _mainThreadSynchronizationContext = SynchronizationContext.Current;

            var defaultLimit = Math.Max(1, LiteRuntime.Setting.Task.ConcurrencyLimit);
            RegisterGroup(DefaultGroupId, defaultLimit);
        }

        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            _monoBehaviourInstance?.StopAllCoroutines();

            var count = _records.Count;
            for (var i = 0; i < count; i++)
            {
                Retire(_records[i], cancel: true);
            }

            _records.Clear();
            _groups.Clear();
        }

        public void Tick(float deltaTime)
        {
            var recordCount = _records.Count;
            var groupCount = _groups.Count;

            try
            {
                for (var i = 0; i < recordCount; i++)
                {
                    TickRecord(_records[i], deltaTime);
                }

                for (var i = 0; i < groupCount; i++)
                {
                    var group = _groups[i];
                    var remaining = group.PendingTaskCount;

                    while (remaining > 0 && group.TryAdmit(out var record))
                    {
                        remaining--;
                        StartRecord(record);
                    }
                }
            }
            finally
            {
                _records.RemoveAll(static record => record.Phase == TaskSchedulePhase.Retired);
            }
        }

        private void TickRecord(TaskRecord record, float deltaTime)
        {
            if (record.Phase == TaskSchedulePhase.Retired)
            {
                return;
            }

            try
            {
                if (record.Phase == TaskSchedulePhase.Running && !record.Task.IsDone)
                {
                    record.Task.Tick(deltaTime);
                }

                if (record.Task.IsDone)
                {
                    Retire(record, cancel: false);
                }
            }
            catch (Exception ex)
            {
                LLog.Exception(ex, "Task {0} tick failed", record.Task.GetType().Name);
                Retire(record, cancel: true);
            }
        }

        private void StartRecord(TaskRecord record)
        {
            try
            {
                if (!record.Task.IsDone)
                {
                    record.Task.Execute();
                }

                if (record.Task.IsDone)
                {
                    Retire(record, cancel: false);
                }
            }
            catch (Exception ex)
            {
                LLog.Exception(ex, "Task {0} execute failed", record.Task.GetType().Name);
                Retire(record, cancel: true);
            }
        }

        private void Retire(TaskRecord record, bool cancel)
        {
            if (record.Phase == TaskSchedulePhase.Retired)
            {
                return;
            }

            record.Group.Detach(record);

            if (cancel)
            {
                try
                {
                    record.Task.Cancel();
                }
                catch(Exception ex)
                {
                    LLog.Exception(ex, "Task {0} cancel failed", record.Task.GetType().Name);
                }
            }

            try
            {
                record.Task.Dispose();
            }
            catch (Exception ex)
            {
                LLog.Exception(ex, "Task {0} dispose failed", record.Task.GetType().Name);
            }
        }

        internal void CopyTasksTo(List<ITask> destination)
        {
            destination.Clear();

            for (var i = 0; i < _records.Count; i++)
            {
                var record = _records[i];

                if (record.Phase != TaskSchedulePhase.Retired)
                {
                    destination.Add(record.Task);
                }
            }
        }

        public UnityEngine.Coroutine StartCoroutine(IEnumerator routine)
        {
            return _monoBehaviourInstance?.StartCoroutine(routine);
        }

        public void StopCoroutine(UnityEngine.Coroutine routine)
        {
            _monoBehaviourInstance?.StopCoroutine(routine);
        }

        public void RegisterGroup(int groupId, int? concurrencyLimit)
        {
            if (FindGroup(groupId) != null)
            {
                throw new ArgumentException($"Task group {groupId} already exists.", nameof(groupId));
            }

            _groups.Add(new TaskGroup(groupId, concurrencyLimit));
        }

        private TaskGroup FindGroup(int groupId)
        {
            foreach (var group in _groups)
            {
                if (group.Id == groupId)
                {
                    return group;
                }
            }

            return null;
        }

        private bool ContainsTask(ITask task)
        {
            foreach (var record in _records)
            {
                if (ReferenceEquals(record.Task, task))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddTask(ITask task, int groupId = DefaultGroupId, TaskPriority priority = TaskPriority.Normal)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var group = FindGroup(groupId);
            if (group == null)
            {
                throw new ArgumentException($"Task group {groupId} does not exist.", nameof(groupId));
            }

            if (ContainsTask(task))
            {
                throw new InvalidOperationException("Task is already registered.");
            }

            if (task.State != TaskState.Pending || task.IsExecuted || task.IsDone)
            {
                throw new InvalidOperationException("Only a new pending task can be submitted.");
            }

            var record = new TaskRecord(task, group, priority);
            _records.Add(record);
            group.Enqueue(record);
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
            AddTask(task, priority: TaskPriority.Urgent);
            return task;
        }

        internal LoadLocalBundleTask AddLoadLocalBundleTask(string bundleUri, Action<UnityEngine.AssetBundle> callback)
        {
            var task = new LoadLocalBundleTask(bundleUri, callback);
            AddTask(task, priority: TaskPriority.Urgent);
            return task;
        }

        internal LoadRemoteBundleTask AddLoadRemoteBundleTask(string bundleUri, string hash, Action<UnityEngine.AssetBundle> callback)
        {
            var task = new LoadRemoteBundleTask(bundleUri, hash, callback);
            AddTask(task, priority: TaskPriority.Urgent);
            return task;
        }

        internal LoadAssetTask<T> AddLoadAssetTask<T>(UnityEngine.AssetBundle bundle, string assetName, Action<T> callback) where T : UnityEngine.Object
        {
            var task = new LoadAssetTask<T>(bundle, assetName, callback);
            AddTask(task);
            return task;
        }

        internal LoadSceneTask AddLoadSceneTask(string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool, UnityEngine.SceneManagement.Scene> callback)
        {
            var task = new LoadSceneTask(sceneName, parameters, callback);
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
