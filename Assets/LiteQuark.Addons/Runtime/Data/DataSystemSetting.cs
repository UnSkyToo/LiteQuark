using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable, LiteLabel("数据设置")]
    public class DataSystemSetting : ISystemSetting
    {
        [Tooltip("数据存储模式\nPlayerPrefs: 应用的 PlayerPrefs\nJsonFile: .json 文本文件\nBinaryFile: .dat 字节文件\nCustom: 自定义数据提供者"), SerializeField]
        public DataProviderMode ProviderMode = DataProviderMode.PlayerPrefs;

        [ConditionalShow(nameof(ProviderMode), (int)DataProviderMode.Custom)]
        public LiteTypeEntryData<IDataProvider> CustomProviderType;

        [Tooltip("对内置数据提供者保存的内容加密"), SerializeField]
        public bool EnableEncryption = true;

        [Tooltip("加密密钥（建议每个游戏使用不同的密钥）"), ConditionalShow(nameof(EnableEncryption), true), SerializeField]
        public string EncryptionKey = "LiteQuark";

        public DataSystemSetting()
        {
        }
    }
}
