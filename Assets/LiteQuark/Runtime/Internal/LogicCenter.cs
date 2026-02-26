using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class LogicCenter : Singleton<LogicCenter>, ISubstance
    {
        private readonly List<ILogic> _logicList = new List<ILogic>();
        
        private LogicCenter()
        {
        }

        public void Dispose()
        {
            UnInitializeLogic();
        }
        
        public void Tick(float deltaTime)
        {
            foreach (var logic in _logicList)
            {
                logic.Tick(deltaTime);
            }
        }
        
        internal async UniTask<bool> InitializeLogic(CancellationToken ct = default)
        {
            _logicList.Clear();
            
            foreach (var logicEntry in LiteRuntime.Setting.LogicList)
            {
                if (ct.IsCancellationRequested)
                {
                    return false;
                }

                if (logicEntry.Disabled)
                {
                    continue;
                }
                
                LLog.Info("Initialize {0} logic", logicEntry.AssemblyQualifiedName);

                var logic = TypeUtils.CreateEntryInstance(logicEntry);
                
                var result = await logic.Initialize();
                if (ct.IsCancellationRequested)
                {
                    if (result)
                    {
                        logic.Dispose();
                    }
                    return false;
                }

                if (result)
                {
                    _logicList.Add(logic);
                }
                else
                {
                    LLog.Error("Initialize {0} failed", logicEntry.AssemblyQualifiedName);
                    return false;
                }
            }
            
            return true;
        }

        internal void UnInitializeLogic()
        {
            foreach (var logic in _logicList)
            {
                logic.Dispose();
            }
            
            _logicList.Clear();
        }
    }
}