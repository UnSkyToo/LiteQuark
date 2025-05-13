using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LiteQuark.Runtime
{
    public class FileLogAppender : TextWriterLogAppender
    {
        private readonly string _rootDirectoryPath;
        
        private Stream _stream;
        private string _filePath;
        private readonly bool _appendToFile;
        
        public FileLogAppender(string rootDirectoryPath, bool appendToFile)
        {
            _rootDirectoryPath = rootDirectoryPath;
            _appendToFile = appendToFile;
        }

        protected override void OnOpen()
        {
            try
            {
                OpenFile();
            }
            catch (Exception ex)
            {
                LogErrorHandler.Error($"OpenFile({_filePath},{_appendToFile}) call failed.", ex);
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
            _filePath = string.Empty;
        }

        private void OpenFile()
        {
            lock (this)
            {
                Reset();

                _filePath = GetLogFilePath();
                EnsureFilePath(_filePath);
                CreateStream(_filePath);
            }
        }

        private void CloseFile()
        {
            lock (this)
            {
                if (_stream != null)
                {
                    _stream.Close();
                    _stream.Dispose();
                    _stream = null;
                }
            }
        }

        private void CreateStream(string filePath)
        {
            var mode = _appendToFile ? FileMode.Append : FileMode.Create;
            _stream = new FileStream(filePath, mode, FileAccess.Write, FileShare.Read);

            if (_stream != null)
            {
                var writer = new StreamWriter(_stream, Encoding.Default);
                QuiteWriter = new LogQuietTextWriter(writer);
            }
        }

        private string GetLogFilePath()
        {
            var datePattern = "yyyy-MM-dd-HH-mm-ss";
            var dateStr = DateTime.Now.ToString(datePattern, DateTimeFormatInfo.InvariantInfo);
            return Path.Combine(_rootDirectoryPath, "Logs", Name, $"Log_{dateStr}.log");
        }

        private void EnsureFilePath(string path)
        {
            var directoryPath = Path.GetDirectoryName(path);
            PathUtils.CreateDirectory(directoryPath);
        }
    }
}