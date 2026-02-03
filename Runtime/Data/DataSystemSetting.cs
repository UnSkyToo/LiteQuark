using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable, LiteLabel("数据设置")]
    public class DataSystemSetting : ISystemSetting
    {
        [Tooltip("数据存储模式\nPlayerPrefs: 适合少量配置数据\nJsonFile: 适合结构化数据，可读性好\nBinaryFile: 适合大量数据，性能最好\nCustom: 自定义模式"), SerializeField]
        public DataProviderMode ProviderMode = DataProviderMode.PlayerPrefs;

        [ConditionalShow(nameof(ProviderMode), (int)DataProviderMode.Custom)]
        public LiteTypeEntryData<IDataProvider> CustomProviderType;

        [Tooltip("启用数据加密（防止玩家修改存档）"), SerializeField]
        public bool EnableEncryption = true;

        [Tooltip("加密密钥（建议每个游戏使用不同的密钥）"), ConditionalShow(nameof(EnableEncryption), true), SerializeField]
        public string EncryptionKey = "LiteQuark";

        public DataSystemSetting()
        {
        }
    }
}