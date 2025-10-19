﻿using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LoadResourceTask<T> : BaseTask where T : UnityEngine.Object
    {
        private readonly string _assetName;
        private Action<T> _callback;
        private ResourceRequest _resourceRequest;
        private UnityEngine.Object _asset;

        public LoadResourceTask(string assetName, Action<T> callback)
            : base()
        {
            _assetName = assetName;
            _callback = callback;
            _resourceRequest = null;
            _asset = null;
        }

        public override void Dispose()
        {
            _callback = null;
            _resourceRequest = null;
            _asset = null;
        }

        protected override void OnExecute()
        {
            var path = PathUtils.GetFilePathWithoutExt(_assetName);
            _resourceRequest = Resources.LoadAsync<T>(path);
            _resourceRequest.completed += OnResourceRequestLoadCompleted;
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _resourceRequest?.progress ?? 0f;
        }

        private void OnResourceRequestLoadCompleted(AsyncOperation op)
        {
            op.completed -= OnResourceRequestLoadCompleted;
            _asset = _resourceRequest.asset;
            
            LiteUtils.SafeInvoke(_callback, _asset as T);
            Complete(_asset);
        }
    }
}