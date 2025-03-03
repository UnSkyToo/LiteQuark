using System;
using System.IO;

namespace LiteQuark.Runtime
{
    public sealed class WriteFileAsyncTask : BaseTask
    {
        private const int BufferSize = 4096;

        private readonly FileStream Stream_;
        private readonly byte[] Data_;
        private readonly Action<bool> Callback_;
        private int Offset_;

        public WriteFileAsyncTask(string filePath, byte[] data, Action<bool> callback)
        {
            try
            {
                Stream_ = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, BufferSize, true);
                Data_ = data;
                Callback_ = callback;
                Offset_ = 0;
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
            Stream_.Close();
            Stream_.Dispose();
        }

        protected override void OnExecute()
        {
            ExecuteInternal();
        }

        private async void ExecuteInternal()
        {
            while (Stream_.CanWrite)
            {
                var writeCount = Offset_ + BufferSize < Data_.Length ? BufferSize : Data_.Length - Offset_;
                var task = Stream_.WriteAsync(Data_, Offset_, writeCount);
                await task.ConfigureAwait(false);

                if (!task.IsCompleted)
                {
                    Callback_?.Invoke(false);
                    Abort();
                    break;
                }

                Offset_ += writeCount;
                if (Offset_ >= Data_.Length)
                {
                    await Stream_.FlushAsync().ConfigureAwait(false);
                    
                    Callback_?.Invoke(true);
                    Complete();
                    break;
                }
            }
        }
    }
}