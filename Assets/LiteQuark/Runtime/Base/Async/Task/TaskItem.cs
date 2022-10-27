using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public class TaskItem : IDisposable
    {
        public bool IsPause { get; set; }
        public bool IsEnd { get; private set; }

        private readonly IEnumerator Item_;
        private Action Callback_;

        public TaskItem(IEnumerator item, Action callback)
            : base()
        {
            IsPause = false;
            IsEnd = false;

            Item_ = item;
            Callback_ = callback;
        }

        public void Dispose()
        {
            Callback_ = null;
        }

        public void Start()
        {
            IsPause = false;
        }

        public void Pause()
        {
            IsPause = true;
        }

        public void Stop()
        {
            Pause();
            IsEnd = true;
        }

        public IEnumerator Execute()
        {
            while (!IsEnd)
            {
                if (IsPause)
                {
                    yield return null;
                }
                else if (Item_ != null && Item_.MoveNext())
                {
                    yield return Item_.Current;
                }
                else
                {
                    IsEnd = true;
                }
            }

            Callback_?.Invoke();
        }
    }
}