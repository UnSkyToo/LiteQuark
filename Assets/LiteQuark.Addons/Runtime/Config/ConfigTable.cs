using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class ConfigTable<T> where T : IConfigData
    {
        private readonly Dictionary<int, T> _dataMap;
        private readonly List<T> _dataList;

        public ConfigTable(List<T> dataList)
        {
            _dataList = dataList;
            _dataMap = new Dictionary<int, T>(dataList.Count);

            foreach (var data in dataList)
            {
                _dataMap[data.Id] = data;
            }
        }

        public T Get(int id)
        {
            if (_dataMap.TryGetValue(id, out var data))
            {
                return data;
            }

            LLog.Warning("ConfigTable<{0}> id not found: {1}", typeof(T).Name, id);
            return default;
        }

        public bool TryGet(int id, out T data)
        {
            return _dataMap.TryGetValue(id, out data);
        }

        public List<T> GetAll()
        {
            return _dataList;
        }

        public int Count => _dataList.Count;
    }
}