using System;

namespace LiteQuark.Runtime
{
    internal class AsyncCompletionGroup<T>
    {
        private int _remainingCount;
        private bool _hasError;
        private T _result;
        private readonly Action<bool, T> _onCompleted;

        public AsyncCompletionGroup(int count, Action<bool, T> onCompleted)
        {
            _remainingCount = count;
            _onCompleted = onCompleted;
        }

        public void SignalMain(T result)
        {
            _result = result;
            Signal(result != null);
        }

        public void SignalSub(bool success)
        {
            Signal(success);
        }

        private void Signal(bool success)
        {
            if (!success)
            {
                _hasError = true;
            }

            if (--_remainingCount <= 0)
            {
                _onCompleted?.Invoke(!_hasError, _result);
            }
        }
    }
}