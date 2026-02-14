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
        public float DelayTime = 1f;
        
        public RetryParam()
        {
        }
        
        public RetryParam(int timeout, int maxRetries, float delayTime)
        {
            Timeout = timeout;
            MaxRetries = maxRetries;
            DelayTime = delayTime;
        }
    }
}