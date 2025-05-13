using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadBundleBaseTask : BaseTask
    {
        protected readonly string BundleUri;
        protected readonly List<LoadBundleBaseTask> ChildTasks;
        private AssetBundle _bundle;
        private Action<AssetBundle> _callback;
        private bool _isLoaded;

        protected LoadBundleBaseTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            BundleUri = bundleUri;
            ChildTasks = new List<LoadBundleBaseTask>();
            _bundle = null;
            _callback = callback;
            _isLoaded = false;
        }
        
        public override void Dispose()
        {
            _callback = null;
        }

        protected override void OnTick(float deltaTime)
        {
            var value = GetDownloadPercent() + ChildTasks.Sum(childTask => childTask.Progress);
            Progress = value / (1f + ChildTasks.Count);

            if (_isLoaded)
            {
                var isChildDone = ChildTasks.Count <= 0 || ChildTasks.All(childTask => childTask.IsDone);
                if (isChildDone)
                {
                    _callback?.Invoke(_bundle);
                    Complete(_bundle);
                }
            }
        }

        public void AddChildTask(LoadBundleBaseTask childTask)
        {
            if (childTask == null)
            {
                return;
            }
            
            ChildTasks.Add(childTask);
        }

        protected void OnBundleLoaded(AssetBundle bundle)
        {
            _bundle = bundle;
            _isLoaded = true;
        }

        public abstract AssetBundle WaitCompleted();
        protected abstract float GetDownloadPercent();
    }
}