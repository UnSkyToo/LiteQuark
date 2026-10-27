using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class LogicCenter : Singleton<LogicCenter>, ITick, IDisposeAsync
    {
        private readonly List<ILogic> _logicList = new();
        private readonly List<ILogic> _ownedLogics = new();

        private LogicCenter()
        {
        }

        public async UniTask DisposeAsync()
        {
            await UnInitializeLogic();
        }

        public void Tick(float deltaTime)
        {
            foreach (var logic in _logicList)
            {
                if (!_ownedLogics.Contains(logic))
                {
                    continue;
                }

                logic.Tick(deltaTime);
            }
        }

        internal async UniTask<bool> InitializeLogic(CancellationToken ct = default)
        {
            if (_ownedLogics.Count > 0)
            {
                throw new System.InvalidOperationException("Previous logic objects have not been disposed.");
            }

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

                var logic = TypeUtils.CreateEntryInstance(logicEntry);
                if (logic == null)
                {
                    throw new System.InvalidOperationException($"Cannot create logic: {logicEntry.AssemblyQualifiedName}");
                }

                _ownedLogics.Add(logic);

                LLog.Info("Initialize {0} logic", logicEntry.AssemblyQualifiedName);

                var result = await logic.Initialize();
                if (ct.IsCancellationRequested)
                {
                    return false;
                }

                if (!result)
                {
                    LLog.Error("Initialize {0} failed", logicEntry.AssemblyQualifiedName);
                    return false;
                }

                _logicList.Add(logic);
            }

            return !ct.IsCancellationRequested;
        }

        internal async UniTask UnInitializeLogic()
        {
            await LiteRuntime.Instance.WaitForTickExitAsync();

            for (var index = _ownedLogics.Count - 1; index >= 0; index--)
            {
                var logic =  _ownedLogics[index];
                if (!_logicList.Contains(logic))
                {
                    await DisposeOwnedLogic(logic);
                }
            }

            while (_ownedLogics.Count > 0)
            {
                await DisposeOwnedLogic(_ownedLogics[0]);
            }
        }

        private async UniTask DisposeOwnedLogic(ILogic logic)
        {
            await logic.DisposeAsync();
            _ownedLogics.Remove(logic);

            await LiteRuntime.Instance.WaitForTickExitAsync();
            _logicList.Remove(logic);
        }
    }
}
