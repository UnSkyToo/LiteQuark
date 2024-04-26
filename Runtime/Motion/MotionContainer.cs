using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class MotionContainer : BaseMotion
    {
        public int Count => SubMotions_.Count;
        
        protected readonly List<BaseMotion> SubMotions_;

        protected MotionContainer(params BaseMotion[] args)
        {
            SubMotions_ = new List<BaseMotion>(args);
            IsEnd = SubMotions_.Count == 0;
        }

        public BaseMotion[] GetSubMotions()
        {
            return SubMotions_.ToArray();
        }

        public void AddMotion(BaseMotion motion)
        {
            SubMotions_.Add(motion);
        }
    }
}