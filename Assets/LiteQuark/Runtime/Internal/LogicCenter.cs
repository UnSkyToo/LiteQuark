using System.Collections.Generic;

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
        
        internal void InitializeLogic(System.Action<bool> callback)
        {
            LogicList_.Clear();

            var initTypeList = new List<System.Type>();
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
                
                initTypeList.Add(logicType);
            }
            
            new AsyncInitializer<ILogic>(initTypeList, (logic, isError) =>
            {
                if (isError)
                {
                    callback?.Invoke(false);
                    return;
                }

                if (logic != null)
                {
                    LogicList_.Add(logic);
                }
                else
                {
                    callback?.Invoke(true);
                }
            }).StartInitialize();
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