﻿using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed class StageCenter : ISubstance
    {
        private readonly List<IStage> StageList_ = new List<IStage>();
        private int Index_ = 0;
        private IStage CurrentStage_ = null;
        
        public StageCenter()
        {
            StageList_.Add(new BootStage());
            StageList_.Add(new InitSystemStage());
            StageList_.Add(new InitLogicStage());
            StageList_.Add(new MainStage());
            
            StageList_.Add(new ErrorStage());
            
            GotoStage(0);
        }
        
        public void Dispose()
        {
            CurrentStage_?.Leave();
            CurrentStage_ = null;
        }
        
        public void Tick(float deltaTime)
        {
            if (CurrentStage_ == null)
            {
                return;
            }

            var code = CurrentStage_.Tick(deltaTime);
            switch (code)
            {
                case StageCode.Waiting:
                    break;
                case StageCode.Running:
                    break;
                case StageCode.Completed:
                    NextStage();
                    break;
                case StageCode.Error:
                    GotoStage(StageList_.Count - 1);
                    break;
            }
        }

        private void NextStage()
        {
            GotoStage(Index_ + 1);
        }

        private void GotoStage(int index)
        {
            if (index < 0 || index >= StageList_.Count)
            {
                return;
            }
            
            CurrentStage_?.Leave();
            CurrentStage_ = StageList_[index];
            CurrentStage_?.Enter();
            Index_ = index;
        }
    }
}