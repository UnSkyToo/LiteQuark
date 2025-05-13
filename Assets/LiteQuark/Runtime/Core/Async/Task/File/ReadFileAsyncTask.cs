using System;
using System.Collections.Generic;
using System.IO;

namespace LiteQuark.Runtime
{
    public sealed class ReadFileAsyncTask : BaseTask
    {
        private const int BufferSize = 4096;

        private readonly FileStream _stream;
        private readonly List<byte> _data;
        private readonly byte[] _buffer = new byte[BufferSize];
        private readonly Action<byte[]> _callback;

        public ReadFileAsyncTask(string filePath, Action<byte[]> callback)
        {
            try
            {
                _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferSize, true);
                _data = new List<byte>();
                _callback = callback;
            }
            catch
            {
                callback?.Invoke(null);
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
            ExecuteInternal();
        }

        private async void ExecuteInternal()
        {
            while (_stream.CanRead)
            {
                var task = _stream.ReadAsync(_buffer, 0, BufferSize);
                var readCount = await task.ConfigureAwait(false);

                if (!task.IsCompleted)
                {
                    _callback?.Invoke(null);
                    Abort();
                    break;
                }

                if (readCount == BufferSize)
                {
                    _data.AddRange(_buffer);
                }
                else if (readCount > 0)
                {
                    for (var i = 0; i < readCount; i++)
                    {
                        _data.Add(_buffer[i]);
                    }
                }
                else
                {
                    var data = _data.ToArray();
                    _callback?.Invoke(data);
                    Complete(data);
                    break;
                }
            }
        }
    }
}