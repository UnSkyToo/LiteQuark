using System;
using System.IO;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class WriteFileAsyncTask : BaseTask
    {
        private const int BufferSize = 4096;

        private readonly FileStream _stream;
        private readonly byte[] _data;
        private readonly Action<bool> _callback;
        private int _offset;

        public WriteFileAsyncTask(string filePath, byte[] data, Action<bool> callback)
        {
            try
            {
                _stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, BufferSize, true);
                _data = data;
                _callback = callback;
                _offset = 0;
            }
            catch
            {
                callback?.Invoke(false);
                Abort();
                throw;
            }
        }
        
        public override void Dispose()
        {
            _stream.Close();
            _stream.Dispose();
        }

        protected override void OnExecute()
        {
            ExecuteInternal().Forget();
        }

        private async UniTaskVoid ExecuteInternal()
        {
            while (_stream.CanWrite)
            {
                var writeCount = _offset + BufferSize < _data.Length ? BufferSize : _data.Length - _offset;
                var task = _stream.WriteAsync(_data, _offset, writeCount);
                await task.ConfigureAwait(false);

                if (!task.IsCompleted)
                {
                    _callback?.Invoke(false);
                    Abort();
                    break;
                }

                _offset += writeCount;
                if (_offset >= _data.Length)
                {
                    await _stream.FlushAsync().ConfigureAwait(false);
                    
                    _callback?.Invoke(true);
                    Complete(true);
                    break;
                }
            }
        }
    }
}