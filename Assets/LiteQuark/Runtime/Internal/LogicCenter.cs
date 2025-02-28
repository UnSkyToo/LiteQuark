using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed class LogicCenter : Singleton<LogicCenter>, ISubstance
    {
        private readonly List<ILogic> LogicList_ = new List<ILogic>();
        private readonly List<ILogic> LogicAddList_ = new List<ILogic>();
        
        private LogicCenter()
        {
        }

        public void Dispose()
        {
            UnInitializeLogic();
        }
        
        public void Tick(float deltaTime)
        {
            ProcessLogicAdd();
            
            foreach (var logic in LogicList_)
            {
                logic.Tick(deltaTime);
            }
        }
        
        internal void InitializeLogic()
        {
            LogicList_.Clear();
            
            foreach (var logicEntry in LiteRuntime.Setting.LogicList)
            {
                if (logicEntry.Disabled)
                {
                    continue;
                }
                
                LLog.Info($"initialize {logicEntry.AssemblyQualifiedName} system");

                var logicType = System.Type.GetType(logicEntry.AssemblyQualifiedName);
                if (logicType == null)
                {
                    throw new System.Exception($"can't not find logic class type : {logicEntry.AssemblyQualifiedName}");
                }

                if (System.Activator.CreateInstance(logicType) is not ILogic logic)
                {
                    throw new System.Exception($"incorrect logic class type : {logicEntry.AssemblyQualifiedName}");
                }

                if (!logic.Startup())
                {
                    throw new System.Exception($"{logicEntry.AssemblyQualifiedName} startup failed");
                }

                LogicList_.Add(logic);
            }
        }

        internal void UnInitializeLogic()
        {
            ProcessLogicAdd();
            
            foreach (var logic in LogicList_)
            {
                logic.Shutdown();
            }
            LogicList_.Clear();
        }
        
        public void AddLogic(ILogic logic)
        {
            LogicAddList_.Add(logic);
        }

        private void ProcessLogicAdd()
        {
            if (LogicAddList_.Count > 0)
            {
                foreach (var logic in LogicAddList_)
                {
                    if (logic == null)
                    {
                        continue;
                    }
                    
                    if (!logic.Startup())
                    {
                        throw new System.Exception($"{logic.GetType().Name} startup failed");
                    }
                    
                    LogicList_.Add(logic);
                }
                LogicAddList_.Clear();
            }
        }
    }
}