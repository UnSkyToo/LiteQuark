using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 数据存储提供者接口
    /// </summary>
    public interface IDataProvider : IDispose
    {
        /// <summary>
        /// 初始化
        /// </summary>
        UniTask<bool> Initialize();

        /// <summary>
        /// 同步保存数据
        /// </summary>
        void Save<T>(string key, T data);

        /// <summary>
        /// 异步保存数据
        /// </summary>
        UniTask SaveAsync<T>(string key, T data);

        /// <summary>
        /// 同步加载数据
        /// </summary>
        T Load<T>(string key, T defaultValue = default);

        /// <summary>
        /// 异步加载数据
        /// </summary>
        UniTask<T> LoadAsync<T>(string key, T defaultValue = default);

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        bool Has(string key);

        /// <summary>
        /// 删除指定键的数据
        /// </summary>
        void Delete(string key);

        /// <summary>
        /// 删除所有数据
        /// </summary>
        void DeleteAll();
    }
}
