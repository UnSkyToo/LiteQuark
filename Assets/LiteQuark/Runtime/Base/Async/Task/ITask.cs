using System;

namespace LiteQuark.Runtime
{
    public interface ITask : IDisposable
    {
        public bool IsEnd { get; }
        public bool IsExecute { get; }
        
        public void Execute();
    }
}