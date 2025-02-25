using System.Collections.Generic;

namespace LiteBattle.Runtime
{
    public class LiteContext
    {
        private const string TagKey = "lite_context_tag";
        
        private readonly LiteContext Outer_ = null;
        private readonly Dictionary<string, LiteContextDataBase> DataList_ = new Dictionary<string, LiteContextDataBase>();

        public LiteContext(LiteContext outer)
        {
            Outer_ = outer;
        }

        public void SetData<T>(string key, T val, int lifeCycle)
        {
            DataList_[key] = new LiteContextData<T>(val, lifeCycle);
        }

        public T GetData<T>(string key, T defaultValue)
        {
            if (!DataList_.ContainsKey(key))
            {
                if (Outer_ != null)
                {
                    return Outer_.GetData<T>(key, defaultValue);
                }
                
                return defaultValue;
            }

            var data = DataList_[key];
            
            if (data.IsValid() && data is LiteContextData<T> realData)
            {
                return realData.Value;
            }

            return defaultValue;
        }
        
        public void SetTag(LiteTag tag, bool value, int lifeCycle)
        {
            SetData($"{TagKey}_{tag}", value, lifeCycle);
        }

        public bool GetTag(LiteTag tag)
        {
            return GetData($"{TagKey}_{tag}", false);
        }
        
        // public void SetTag(LiteTag tag, bool value, int lifeCycle)
        // {
        //     var data = GetData(TagKey, 0u);
        //
        //     if (value)
        //     {
        //         data |= (uint) tag;
        //     }
        //     else
        //     {
        //         data &= ~((uint) tag);
        //     }
        //     
        //     SetData(TagKey, data, lifeCycle);
        // }
        //
        // public bool GetTag(LiteTag tag)
        // {
        //     var data = GetData(TagKey, 0u);
        //
        //     return (data & (uint) tag) != 0;
        // }

        public void Tick()
        {
            foreach (var cache in DataList_)
            {
                cache.Value.Tick();
            }
        }

        private abstract class LiteContextDataBase
        {
            private int LifeCycle_;

            protected LiteContextDataBase(int lifeCycle)
            {
                LifeCycle_ = lifeCycle;
            }

            public bool IsValid()
            {
                return LifeCycle_ > 0;
            }

            public void Tick()
            {
                LifeCycle_--;
            }
        }

        private class LiteContextData<T> : LiteContextDataBase
        {
            public T Value { get; }

            public LiteContextData(T value, int lifeCycle)
                : base(lifeCycle)
            {
                Value = value;
            }
        }
    }
}