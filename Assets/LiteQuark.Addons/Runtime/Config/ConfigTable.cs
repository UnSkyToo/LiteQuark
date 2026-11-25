using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class ConfigTable<T, TKey>
    {
        private readonly Dictionary<TKey, T> _dataMap;
        private readonly List<T> _dataList;

        internal ConfigTable(List<T> dataList, Func<T, TKey> keySelector)
        {
            _dataList = dataList;
            _dataMap = new Dictionary<TKey, T>(dataList.Count);

            foreach (var data in dataList)
            {
                _dataMap[keySelector(data)] = data;
            }
        }

        public T Get(TKey id)
        {
            if (_dataMap.TryGetValue(id, out var data))
            {
                return data;
            }

            LLog.Warning("ConfigTable<{0}> key not found: {1}", typeof(T).Name, id);
            return default;
        }

        public bool TryGet(TKey id, out T data)
        {
            return _dataMap.TryGetValue(id, out data);
        }

        public List<T> GetAll()
        {
            return _dataList;
        }

        public int Count => _dataList.Count;
    }

    public sealed class ConfigTable<T> : ConfigTable<T, int> where T : IConfigData
    {
        public ConfigTable(List<T> dataList) : base(dataList, static data => data.Id)
        {
        }
    }
}
