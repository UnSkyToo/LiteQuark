using System.Collections.Generic;
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
        
        internal async UniTask<bool> InitializeLogic()
        {
            _logicList.Clear();
            
            foreach (var logicEntry in LiteRuntime.Setting.LogicList)
            {
                if (logicEntry.Disabled)
                {
                    continue;
                }
                
                LLog.Info("Initialize {0} logic", logicEntry.AssemblyQualifiedName);

                var logicType = System.Type.GetType(logicEntry.AssemblyQualifiedName);
                if (logicType == null)
                {
                    throw new System.Exception($"Can't not find logic class type : {logicEntry.AssemblyQualifiedName}");
                }
                
                if (System.Activator.CreateInstance(logicType) is not ILogic logic)
                {
                    throw new System.Exception($"Incorrect {typeof(ILogic)} type : {logicType.AssemblyQualifiedName}");
                }
                
                var result = await logic.Initialize();
                if (result)
                {
                    _logicList.Add(logic);
                }
                else
                {
                    LLog.Error("Initialize {0} failed", logicType.AssemblyQualifiedName);
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