using System.Collections.Generic;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    internal sealed class StageCenter : ISubstance
    {
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly List<IStage> _stageList = new List<IStage>();
        private int _index = 0;
        private IStage _currentStage = null;
        
        public StageCenter()
        {
            _stageList.Add(new BootStage());
            _stageList.Add(new InitSystemStage());
            _stageList.Add(new InitLogicStage());
            _stageList.Add(new MainStage());
            
            _stageList.Add(new ErrorStage());
            
            GotoStage(0);
        }
        
        public void Dispose()
        {
            if (_currentStage != null)
            {
                LLog.Info($"StageCenter: Leave {_currentStage.GetType().Name}");
                _currentStage?.Leave();
                _currentStage = null;
            }
        }
        
        public void Tick(float deltaTime)
        {
            if (_currentStage == null)
            {
                return;
            }

            var code = _currentStage.Tick(deltaTime);
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
            GotoStage(_stageList.Count - 1);
        }

        private void NextStage()
        {
            GotoStage(_index + 1);
        }

        private void GotoStage(int index)
        {
            if (index < 0 || index >= _stageList.Count)
            {
                return;
            }
            
            if (_currentStage != null)
            {
                LLog.Info($"StageCenter: Leave {_currentStage.GetType().Name}");
                _currentStage.Leave();
                _watch.Stop();
                var totalSec = _watch.Elapsed.TotalSeconds;
                LLog.Info($"StageCenter: {_currentStage.GetType().Name} duration {totalSec}s");
            }
            
            _currentStage = _stageList[index];
            _index = index;
            
            if (_currentStage != null)
            {
                LLog.Info($"StageCenter: Enter {_currentStage.GetType().Name}");
                _watch.Restart();
                _currentStage.Enter();
            }
        }
    }
}