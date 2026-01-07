namespace LiteQuark.Runtime
{
    /// <summary>
    /// 数据存储模式
    /// </summary>
    public enum DataProviderMode
    {
        /// <summary>
        /// PlayerPrefs存储（适合少量配置数据）
        /// </summary>
        PlayerPrefs,

        /// <summary>
        /// JSON文件存储（适合结构化数据，可读性好）
        /// </summary>
        JsonFile,

        /// <summary>
        /// 二进制文件存储（适合大量数据，性能好）
        /// </summary>
        BinaryFile
    }
}
