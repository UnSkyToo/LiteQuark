using System;
using System.Collections;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public class UnityWebGetRequestTask : BaseTask
    {
        private readonly string Uri_;
        private Action<DownloadHandler> Callback_;
        private UnityWebRequest Request_;
        
        public UnityWebGetRequestTask(string uri, Action<DownloadHandler> callback)
            : base()
        {
            Uri_ = uri;
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
            Request_ = UnityWebRequest.Get(Uri_);
            var asyncOperation = Request_.SendWebRequest();
            yield return asyncOperation;

            if (Request_.result != UnityWebRequest.Result.Success)
            {
                LLog.Error($"get request uri : {Uri_}\r\n{Request_.error}");
                Callback_?.Invoke(null);
            }
            else
            {
                Callback_?.Invoke(Request_.downloadHandler);
            }

            Stop();
        }
    }
}