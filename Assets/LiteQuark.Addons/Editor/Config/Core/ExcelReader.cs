using System.Data;
using System.IO;
using ExcelDataReader;

namespace LiteQuark.Editor
{
    internal static class ExcelReader
    {
        public static DataSet ReadExcel(string filePath)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var conf = new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = false,
                        }
                    };
                    return reader.AsDataSet(conf);
                }
            }
        }
    }
}