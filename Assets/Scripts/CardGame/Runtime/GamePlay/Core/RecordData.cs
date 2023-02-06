using System;
using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class RecordInt
    {
        private readonly RecordScopeType[] ScopeTypes_;
        private readonly Dictionary<RecordScopeType, int> Cache_ = new Dictionary<RecordScopeType, int>();

        public int Value => GetValue();

        public RecordInt()
        {
            var enumNames = Enum.GetNames(typeof(RecordScopeType));
            ScopeTypes_ = new RecordScopeType[enumNames.Length];
            for (var index = 0; index < ScopeTypes_.Length; ++index)
            {
                ScopeTypes_[index] = Enum.Parse<RecordScopeType>(enumNames[index]);
            }
            
            foreach (var type in ScopeTypes_)
            {
                Cache_.Add(type, 0);
            }
        }

        public RecordInt Clone()
        {
            var data = new RecordInt();
            
            foreach (var type in ScopeTypes_)
            {
                data.Cache_[type] = Cache_[type];
            }

            return data;
        }

        public int GetValue()
        {
            var total = 0;
            
            foreach (var type in ScopeTypes_)
            {
                total += Cache_[type];
            }

            return total;
        }

        public void ChangeValue(RecordScopeType scopeType, int changeValue)
        {
            Cache_[scopeType] += changeValue;
        }

        public void ResetValue(RecordScopeType scopeType)
        {
            Cache_[scopeType] = 0;
        }
    }

    public sealed class RecordList<T>
    {
        private readonly RecordScopeType[] ScopeTypes_;
        private readonly Dictionary<RecordScopeType, List<T>> Cache_ = new Dictionary<RecordScopeType, List<T>>();

        public RecordList()
        {
            var enumNames = Enum.GetNames(typeof(RecordScopeType));
            ScopeTypes_ = new RecordScopeType[enumNames.Length];
            for (var index = 0; index < ScopeTypes_.Length; ++index)
            {
                ScopeTypes_[index] = Enum.Parse<RecordScopeType>(enumNames[index]);
            }
            
            foreach (var type in ScopeTypes_)
            {
                Cache_.Add(type, new List<T>());
            }
        }
        
        public RecordList<T> Clone()
        {
            var data = new RecordList<T>();
            
            foreach (var type in ScopeTypes_)
            {
                data.Cache_[type] = new List<T>(Cache_[type]);
            }

            return data;
        }

        public void Add(RecordScopeType scopeType, T value)
        {
            Cache_[scopeType].Add(value);
        }

        public bool Contains(T value)
        {
            foreach (var type in ScopeTypes_)
            {
                if (Cache_[type].Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset(RecordScopeType scopeType)
        {
            Cache_[scopeType].Clear();
        }
    }
}