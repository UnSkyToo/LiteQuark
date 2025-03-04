using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class LogicCenter : Singleton<LogicCenter>, ISubstance
    {
        private readonly List<ILogic> LogicList_ = new List<ILogic>();
        
        private LogicCenter()
        {
        }

        public void Dispose()
        {
            UnInitializeLogic();
        }
        
        public void Tick(float deltaTime)
        {
            foreach (var logic in LogicList_)
            {
                logic.Tick(deltaTime);
            }
        }
        
        internal async Task<bool> InitializeLogic()
        {
            LogicList_.Clear();
            
            foreach (var logicEntry in LiteRuntime.Setting.LogicList)
            {
                if (logicEntry.Disabled)
                {
                    continue;
                }
                
                LLog.Info($"initialize {logicEntry.AssemblyQualifiedName} logic");

                var logicType = System.Type.GetType(logicEntry.AssemblyQualifiedName);
                if (logicType == null)
                {
                    throw new System.Exception($"can't not find logic class type : {logicEntry.AssemblyQualifiedName}");
                }
                
                if (System.Activator.CreateInstance(logicType) is not ILogic logic)
                {
                    throw new System.Exception($"incorrect {typeof(ILogic)} type : {logicType.AssemblyQualifiedName}");
                }
                
                var result = await logic.Initialize();
                if (result)
                {
                    LogicList_.Add(logic);
                }
                else
                {
                    LLog.Error($"Initialize {logicType.AssemblyQualifiedName} failed");
                    return false;
                }
            }
            
            return true;
        }

        internal void UnInitializeLogic()
        {
            foreach (var logic in LogicList_)
            {
                logic.Dispose();
            }
            
            LogicList_.Clear();
        }
    }
}