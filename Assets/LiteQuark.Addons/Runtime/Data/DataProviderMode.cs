namespace LiteQuark.Runtime
{
    public enum DataProviderMode
    {
        /// <summary>以字符串形式写入应用的 PlayerPrefs。</summary>
        PlayerPrefs,

        /// <summary>以 .json 文件保存明文 JSON 或 Base64 密文。</summary>
        JsonFile,

        /// <summary>以 .dat 文件保存 UTF-8 JSON 字节或加密后的字节。</summary>
        BinaryFile,

        /// <summary>使用自定义 <see cref="IDataProvider"/>。</summary>
        Custom
    }
}
