using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadBundleBaseTask : BaseTask
    {
        protected readonly string BundleUri_;
        protected readonly List<LoadBundleBaseTask> ChildTasks_;
        private AssetBundle Bundle_;
        private Action<AssetBundle> Callback_;
        private bool IsLoaded_;

        protected LoadBundleBaseTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            BundleUri_ = bundleUri;
            ChildTasks_ = new List<LoadBundleBaseTask>();
            Bundle_ = null;
            Callback_ = callback;
            IsLoaded_ = false;
        }
        
        public override void Dispose()
        {
            Callback_ = null;
        }

        protected override void OnTick(float deltaTime)
        {
            var value = GetDownloadPercent() + ChildTasks_.Sum(childTask => childTask.Progress);
            Progress = value / (1f + ChildTasks_.Count);

            if (IsLoaded_)
            {
                var isChildDone = ChildTasks_.Count <= 0 || ChildTasks_.All(childTask => childTask.IsDone);
                if (isChildDone)
                {
                    Callback_?.Invoke(Bundle_);
                    Complete(Bundle_);
                }
            }
        }

        public void AddChildTask(LoadBundleBaseTask childTask)
        {
            if (childTask == null)
            {
                return;
            }
            
            ChildTasks_.Add(childTask);
        }

        protected void OnBundleLoaded(AssetBundle bundle)
        {
            Bundle_ = bundle;
            IsLoaded_ = true;
        }

        public abstract AssetBundle WaitCompleted();
        protected abstract float GetDownloadPercent();
    }
}