using System;
using System.Collections.Generic;
using System.IO;

namespace LiteQuark.Runtime
{
    public sealed class ReadFileAsyncTask : BaseTask
    {
        private const int BufferSize = 4096;

        private readonly FileStream Stream_;
        private readonly List<byte> Data_;
        private readonly byte[] Buffer_ = new byte[BufferSize];
        private readonly Action<byte[]> Callback_;

        public ReadFileAsyncTask(string filePath, Action<byte[]> callback)
        {
            try
            {
                Stream_ = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferSize, true);
                Data_ = new List<byte>();
                Callback_ = callback;
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
            Stream_.Close();
            Stream_.Dispose();
        }

        protected override void OnExecute()
        {
            ExecuteInternal();
        }

        private async void ExecuteInternal()
        {
            while (Stream_.CanRead)
            {
                var task = Stream_.ReadAsync(Buffer_, 0, BufferSize);
                var readCount = await task.ConfigureAwait(false);

                if (!task.IsCompleted)
                {
                    Callback_?.Invoke(null);
                    Abort();
                    break;
                }

                if (readCount == BufferSize)
                {
                    Data_.AddRange(Buffer_);
                }
                else if (readCount > 0)
                {
                    for (var i = 0; i < readCount; i++)
                    {
                        Data_.Add(Buffer_[i]);
                    }
                }
                else
                {
                    Callback_?.Invoke(Data_.ToArray());
                    Complete();
                    break;
                }
            }
        }
    }
}