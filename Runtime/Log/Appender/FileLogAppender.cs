using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LiteQuark.Runtime
{
    public class FileLogAppender : TextWriterLogAppender
    {
        private readonly string RootDirectoryPath_;
        
        private Stream Stream_;
        private string FilePath_;
        private bool AppendToFile_;
        
        public FileLogAppender(string rootDirectoryPath, bool appendToFile)
        {
            RootDirectoryPath_ = rootDirectoryPath;
            AppendToFile_ = appendToFile;
        }

        protected override void OnOpen()
        {
            try
            {
                OpenFile();
            }
            catch (Exception ex)
            {
                LogErrorHandler.Error($"OpenFile({FilePath_},{AppendToFile_}) call failed.", ex);
            }
        }

        protected override void OnClose()
        {
            base.OnClose();
            CloseFile();
        }

        protected override void Reset()
        {
            base.Reset();
            FilePath_ = string.Empty;
        }

        private void OpenFile()
        {
            lock (this)
            {
                Reset();

                FilePath_ = GetLogFilePath();
                EnsureFilePath(FilePath_);
                CreateStream(FilePath_);
            }
        }

        private void CloseFile()
        {
            lock (this)
            {
                if (Stream_ != null)
                {
                    Stream_.Close();
                    Stream_.Dispose();
                    Stream_ = null;
                }
            }
        }

        private void CreateStream(string filePath)
        {
            var mode = AppendToFile_ ? FileMode.Append : FileMode.Create;
            Stream_ = new FileStream(filePath, mode, FileAccess.Write, FileShare.Read);

            if (Stream_ != null)
            {
                var writer = new StreamWriter(Stream_, Encoding.Default);
                QuiteWriter_ = new LogQuietTextWriter(writer);
            }
        }

        private string GetLogFilePath()
        {
            var datePattern = "yyyy-MM-dd-HH-mm-ss";
            var dateStr = DateTime.Now.ToString(datePattern, DateTimeFormatInfo.InvariantInfo);
            return Path.Combine(RootDirectoryPath_, "Logs", Name, $"Log_{dateStr}.log");
        }

        private void EnsureFilePath(string path)
        {
            var directoryPath = Path.GetDirectoryName(path);
            PathUtils.CreateDirectory(directoryPath);
        }
    }
}