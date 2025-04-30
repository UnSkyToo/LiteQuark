using System.Collections.Generic;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    internal sealed class StageCenter : ISubstance
    {
        private readonly Stopwatch Watch_ = new Stopwatch();
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
            if (CurrentStage_ != null)
            {
                LLog.Info($"StageCenter: Leave {CurrentStage_.GetType().Name}");
                CurrentStage_?.Leave();
                CurrentStage_ = null;
            }
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
                    ErrorStage();
                    break;
            }
        }

        internal void ErrorStage()
        {
            GotoStage(StageList_.Count - 1);
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
            
            if (CurrentStage_ != null)
            {
                LLog.Info($"StageCenter: Leave {CurrentStage_.GetType().Name}");
                CurrentStage_.Leave();
                Watch_.Stop();
                var totalSec = Watch_.Elapsed.TotalSeconds;
                LLog.Info($"StageCenter: {CurrentStage_.GetType().Name} duration {totalSec}s");
            }
            
            CurrentStage_ = StageList_[index];
            Index_ = index;
            
            if (CurrentStage_ != null)
            {
                LLog.Info($"StageCenter: Enter {CurrentStage_.GetType().Name}");
                Watch_.Restart();
                CurrentStage_.Enter();
            }
        }
    }
}