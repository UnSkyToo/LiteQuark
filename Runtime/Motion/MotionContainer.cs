using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class MotionContainer : MotionBase
    {
        public int Count => SubMotions_.Count;
        
        protected readonly List<MotionBase> SubMotions_;

        protected MotionContainer(params MotionBase[] args)
        {
            SubMotions_ = new List<MotionBase>(args);
            IsEnd = SubMotions_.Count == 0;
        }

        public MotionBase[] GetSubMotions()
        {
            return SubMotions_.ToArray();
        }

        public void AddMotion(MotionBase motion)
        {
            SubMotions_.Add(motion);
        }
    }
}