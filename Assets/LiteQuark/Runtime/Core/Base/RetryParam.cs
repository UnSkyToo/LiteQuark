using UnityEngine;

namespace LiteQuark.Runtime
{
    [System.Serializable]
    public class RetryParam
    {
        public static RetryParam Default { get; } = new RetryParam(60, 3, 1f);
        
        [Tooltip("超时设置，默认60秒")] [SerializeField]
        public int Timeout = 60;

        [Tooltip("重试次数，默认3")] [Range(0, 10), SerializeField]
        public int MaxRetries = 3;

        [Tooltip("重试等待时间，默认1秒")] [Range(0.01f, 30f), SerializeField]
        public float RetryInterval = 1f;
        
        public RetryParam()
        {
        }
        
        /// <param name="timeoutSec">超时参数（秒）</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryIntervalSec">重试间隔（秒）</param>
        public RetryParam(int timeoutSec, int maxRetries, float retryIntervalSec)
        {
            Timeout = timeoutSec;
            MaxRetries = maxRetries;
            RetryInterval = retryIntervalSec;
        }
        
        public static bool IsCanRetryError(string error)
        {
            return error.Contains("timeout") || error.Contains("Unknown Error") || error.Contains("connection");
        }
    }
}