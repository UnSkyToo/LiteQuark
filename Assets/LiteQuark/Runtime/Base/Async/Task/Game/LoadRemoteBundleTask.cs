using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public class LoadRemoteBundleTask : BaseTask
    {
        private readonly string BundleUri_;
        private Action<AssetBundle> Callback_;
        private UnityWebRequest Request_;
        
        public LoadRemoteBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            BundleUri_ = bundleUri;
            Callback_ = callback;
        }

        public override void Dispose()
        {
            Callback_ = null;
        }

        protected override void OnExecute()
        {
            LiteRuntime.Task.MonoBehaviourInstance.StartCoroutine(ExecuteInternal());
        }

        private IEnumerator ExecuteInternal()
        {
            Request_ = UnityWebRequestAssetBundle.GetAssetBundle(BundleUri_);
            var asyncOperation = Request_.SendWebRequest();
            yield return asyncOperation;

            if (Request_.result != UnityWebRequest.Result.Success)
            {
                LLog.Error($"Failed to download bundle : {BundleUri_}");
                Callback_?.Invoke(null);
            }
            else
            {
                var bundle = DownloadHandlerAssetBundle.GetContent(Request_);
                Callback_?.Invoke(bundle);
            }

            Stop();
        }
    }
}