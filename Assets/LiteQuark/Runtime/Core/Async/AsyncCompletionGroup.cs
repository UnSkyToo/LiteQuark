using System;

namespace LiteQuark.Runtime
{
    internal class AsyncCompletionGroup
    {
        private int _remainingCount;
        private bool _hasError;
        private readonly Action<bool> _onCompleted;

        public AsyncCompletionGroup(int count, Action<bool> onCompleted)
        {
            _remainingCount = count;
            _onCompleted = onCompleted;
        }

        public void Signal(bool success)
        {
            if (!success)
            {
                _hasError = true;
            }

            if (--_remainingCount <= 0)
            {
                _onCompleted?.Invoke(!_hasError);
            }
        }
    }
}
