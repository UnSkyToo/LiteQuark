using System;

namespace LiteQuark.Runtime
{
    public class MotionSequence : MotionContainer
    {
        private BaseMotion Current_;
        protected int Index_;

        public MotionSequence()
            : base(Array.Empty<BaseMotion>())
        {
        }

        public MotionSequence(params BaseMotion[] args)
            : base()
        {
            Index_ = -1;
        }

        public override void Enter()
        {
            IsEnd = Count == 0;
            ActiveNextMotion();
        }

        public override void Exit()
        {
        }

        public override void Tick(float deltaTime)
        {
            if (Current_ != null)
            {
                if (Current_.IsEnd)
                {
                    ActiveNextMotion();
                }
                else
                {
                    Current_.Tick(deltaTime);
                }
            }
        }

        protected virtual int GetNextMotionIndex()
        {
            Index_++;
            if (Index_ >= Count)
            {
                return -1;
            }

            return Index_;
        }

        private void ActiveNextMotion()
        {
            Current_?.Exit();

            Index_ = GetNextMotionIndex();
            if (Index_ == -1)
            {
                Current_ = null;
                IsEnd = true;
            }
            else
            {
                Current_ = SubMotions_[Index_];
            }

            if (Current_ != null)
            {
                Current_.Master = Master;
                Current_?.Enter();

                if (Current_?.IsEnd == true)
                {
                    ActiveNextMotion();
                }
            }
        }
    }

    public class MotionRepeatSequence : MotionSequence
    {
        public MotionRepeatSequence(params BaseMotion[] args)
            : base(args)
        {
        }

        protected override int GetNextMotionIndex()
        {
            Index_++;
            if (Index_ >= Count)
            {
                return 0;
            }

            return Index_;
        }
    }
}