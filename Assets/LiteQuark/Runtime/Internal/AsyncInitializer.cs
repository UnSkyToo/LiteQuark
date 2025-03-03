using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed class AsyncInitializer<T> where T : IInitializeAsync
    {
        private readonly Queue<System.Type> InitializeQueue_ = new();
        private readonly System.Action<T, bool> Callback_;
        private T CurrentInit_ = default;
        
        public AsyncInitializer(List<System.Type> initType, System.Action<T, bool> callback)
        {
            foreach (var type in initType)
            {
                InitializeQueue_.Enqueue(type);
            }

            Callback_ = callback;
        }

        public void StartInitialize()
        {
            InitNext();
        }

        private void InitNext()
        {
            if (InitializeQueue_.Count == 0)
            {
                Callback_?.Invoke(default, false);
                return;
            }

            var type = InitializeQueue_.Dequeue();
            LLog.Info($"Initialize {type.AssemblyQualifiedName}");

            if (System.Activator.CreateInstance(type) is not T instance)
            {
                throw new System.Exception($"incorrect {typeof(T)} type : {type.AssemblyQualifiedName}");
            }
            
            CurrentInit_ = instance;
            instance.Initialize(OnInitCallback);
        }

        private void OnInitCallback(bool isCompleted)
        {
            if (isCompleted)
            {
                Callback_?.Invoke(CurrentInit_, false);
                InitNext();
            }
            else
            {
                LLog.Error($"Initialize {CurrentInit_.GetType().AssemblyQualifiedName} failed");
                Callback_?.Invoke(default, true);
            }
        }
    }
}