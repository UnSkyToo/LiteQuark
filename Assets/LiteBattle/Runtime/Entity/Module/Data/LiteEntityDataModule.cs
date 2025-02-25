using System.Collections.Generic;

namespace LiteBattle.Runtime
{
    public sealed class LiteEntityDataModule : LiteEntityModuleBase
    {
        private readonly Dictionary<LiteEntityDataType, LiteEntityDataValue> DataList_ = new Dictionary<LiteEntityDataType, LiteEntityDataValue>();

        public LiteEntityDataModule(LiteEntity entity)
            : base(entity)
        {
        }

        public override void Dispose()
        {
        }

        public override void Tick(float deltaTime)
        {
        }

        public void UpdateDataList(Dictionary<LiteEntityDataType, double> values)
        {
            foreach (var current in values)
            {
                GetData(current.Key).SetBase(current.Value);
            }
        }

        private LiteEntityDataValue GetData(LiteEntityDataType dataType)
        {
            if (!DataList_.ContainsKey(dataType))
            {
                DataList_.Add(dataType, new LiteEntityDataValue(0));
            }

            return DataList_[dataType];
        }

        public double FinalValue(LiteEntityDataType dataType)
        {
            return GetData(dataType).FinalValue;
        }

        public void AddChange(LiteEntityDataType dataType, double value)
        {
            GetData(dataType).AddChange(value);
        }

        public void AddDelta(LiteEntityDataType dataType, double value)
        {
            GetData(dataType).AddDelta(value);
        }
    }
}