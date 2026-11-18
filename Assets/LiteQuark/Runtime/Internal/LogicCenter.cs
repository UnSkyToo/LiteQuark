using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class LogicCenter : Singleton<LogicCenter>, ITick, IDisposeAsync
    {
        private readonly List<ILogic> _logicList = new();

        private LogicCenter()
        {
        }

        public async UniTask DisposeAsync()
        {
            foreach (var logic in _logicList)
            {
                await LiteRuntime.FinishAtFrameBoundary(logic.DisposeAsync());
            }
            _logicList.Clear();
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
            ILogic unpublished = null;
            try
            {
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

                    unpublished = TypeUtils.CreateEntryInstance(logicEntry);
                    if (unpublished == null)
                    {
                        throw new System.InvalidOperationException($"Cannot create logic: {logicEntry.AssemblyQualifiedName}");
                    }

                    LLog.Info("Initialize {0} logic", logicEntry.AssemblyQualifiedName);
                    var initialized = await unpublished.Initialize();
                    if (ct.IsCancellationRequested)
                    {
                        return false;
                    }
                    if (!initialized)
                    {
                        LLog.Error("Initialize {0} failed", logicEntry.AssemblyQualifiedName);
                        return false;
                    }

                    _logicList.Add(unpublished);
                    unpublished = null;
                }

                return !ct.IsCancellationRequested;
            }
            catch (System.OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return false;
            }
            catch (System.Exception ex)
            {
                LLog.Exception(ex);
                return false;
            }
            finally
            {
                if (unpublished != null)
                {
                    await unpublished.DisposeAsync();
                }
            }
        }
    }
}
