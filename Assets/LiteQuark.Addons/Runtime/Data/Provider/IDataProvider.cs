using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IDataProvider : IInitializeAsync, IDispose
    {
        void Save<T>(string key, T data);

        UniTask SaveAsync<T>(string key, T data);

        T Load<T>(string key, T defaultValue = default);

        UniTask<T> LoadAsync<T>(string key, T defaultValue = default);

        bool Has(string key);

        void Delete(string key);

        /// <summary>清空提供者的存储，实际范围由实现决定。</summary>
        void DeleteAll();
    }
}
