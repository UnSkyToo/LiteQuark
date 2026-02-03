using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable, LiteLabel("网络设置")]
    public class NetworkSystemSetting : ISystemSetting
    {
        [Tooltip("最大并发请求数，避免同时发送过多请求造成服务器压力"), Range(1, 20), SerializeField]
        public int MaxConcurrentRequests = 5;

        [Tooltip("默认超时时间（秒）"), Range(5, 60), SerializeField]
        public int DefaultTimeout = 10;

        [Tooltip("启用自动重试机制"), SerializeField]
        public bool EnableAutoRetry = true;

        [Tooltip("默认重试次数"), ConditionalShow(nameof(EnableAutoRetry), true), Range(1, 10), SerializeField]
        public int DefaultRetryCount = 3;

        public NetworkSystemSetting()
        {
        }
    }
}