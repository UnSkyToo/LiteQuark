namespace LiteBattle.Runtime
{
    public sealed class LiteEntityDataValue
    {
        public double FinalValue => (Base_ + Change_) * Percent_ + Delta_;
        
        private double Base_;
        private double Change_;
        private double Percent_;
        private double Delta_;

        public LiteEntityDataValue(double value)
        {
            Base_ = value;
            Change_ = 0d;
            Percent_ = 1d;
            Delta_ = 0d;
        }

        public void SetBase(double value)
        {
            Base_ = value;
        }

        public void AddChange(double value)
        {
            Change_ += value;
        }

        public void SetChange(double value)
        {
            Change_ = value;
        }

        public void AddPercent(double value)
        {
            Percent_ += value;
        }

        public void SetPercent(double value)
        {
            Percent_ = value;
        }

        public void AddDelta(double value)
        {
            Delta_ += value;
        }

        public void SetDelta(double value)
        {
            Delta_ = value;
        }
    }
}