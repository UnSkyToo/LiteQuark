using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal class AssetBundleLoader
    {
        internal enum Mode
        {
            FromFile,
            WebRequest,
        }

        private readonly string BundleUri_;
        private readonly Mode Mode_;
        private readonly Action<AssetBundle> Callback_;
        private LoadBundleBaseTask Task_;
        
        public AssetBundleLoader(string bundleUri, Mode mode, Action<AssetBundle> callback)
            : base()
        {
            BundleUri_ = bundleUri;
            Mode_ = mode;
            Callback_ = callback;
            
            switch (Mode_)
            {
                case Mode.FromFile:
                    Task_ = LiteRuntime.Task.LoadLocalBundleTask(BundleUri_, Callback_);
                    break;
                case Mode.WebRequest:
                    Task_ = LiteRuntime.Task.LoadRemoteBundleTask(BundleUri_, Callback_);
                    break;
            }
        }

        public AssetBundle WaitCompleted()
        {
            return Task_?.WaitCompleted();
        }
    }
}