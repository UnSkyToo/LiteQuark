using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable, LiteLabel("配置表设置")]
    public class ConfigSystemSetting : ISystemSetting
    {
        [Tooltip("初始化时自动加载所有已注册的配置表"), SerializeField]
        public bool LoadAllOnInitialize = false;

        public ConfigSystemSetting()
        {
        }
    }
}