using System;

namespace LiteQuark.Runtime
{
    public class MotionParallel : MotionContainer
    {
        public MotionParallel()
            : base(Array.Empty<BaseMotion>())
        {
        }
        
        public MotionParallel(params BaseMotion[] args)
            : base(args)
        {
        }

        public override void Enter()
        {
            IsEnd = Count == 0;

            for (var index = 0; index < Count; ++index)
            {
                SubMotions_[index].Master = Master;
                SubMotions_[index].Enter();
            }
        }

        public override void Exit()
        {
            for (var index = 0; index < Count; ++index)
            {
                SubMotions_[index].Exit();
            }
        }

        public override void Tick(float deltaTime)
        {
            var endCount = 0;

            for (var index = 0; index < Count; ++index)
            {
                if (SubMotions_[index].IsEnd)
                {
                    endCount++;
                    continue;
                }

                SubMotions_[index].Tick(deltaTime);
            }

            if (endCount == Count)
            {
                IsEnd = true;
            }
        }
    }
}