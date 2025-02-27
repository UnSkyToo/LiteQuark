using LiteBattle.Runtime;
using LiteQuark.Runtime;

namespace LiteBattle.Editor
{
    internal class LiteRangeBinder : IDispose
    {
        private ILiteRange CurrentRange_ = null;
        
        public LiteRangeBinder()
        {
        }

        public void Dispose()
        {
            UnBindAttackRange();
        }
        
        public bool IsBindAttackRange()
        {
            return CurrentRange_ != null;
        }

        public ILiteRange GetAttackRange()
        {
            return CurrentRange_;
        }

        public void BindAttackRange(ILiteRange range)
        {
            CurrentRange_ = range;
        }

        public void UnBindAttackRange()
        {
            CurrentRange_ = null;
        }
    }
}