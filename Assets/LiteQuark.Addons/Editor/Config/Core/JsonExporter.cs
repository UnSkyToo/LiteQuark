using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LitJson;

namespace LiteQuark.Editor
{
    internal static class JsonExporter
    {
        public static void Export(TableSchema schema, List<RowData> rows, string outputPath)
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Sort rows by id
            ColumnSchema idColumn = null;
            foreach (var col in schema.Columns)
            {
                if (col.IsId)
                {
                    idColumn = col;
                    break;
                }
            }

            if (idColumn != null)
            {
                rows = rows.OrderBy(r =>
                {
                    if (r.Values.TryGetValue(idColumn.Name, out var val) && val is int id)
                    {
                        return id;
                    }
                    return 0;
                }).ToList();
            }

            var sb = new StringBuilder();
            var writer = new JsonWriter(sb) { PrettyPrint = true };
            var registry = TypeConverterRegistry.Instance;

            writer.WriteObjectStart();

            writer.WritePropertyName("table");
            writer.Write(schema.Name.ToLowerInvariant());

            writer.WritePropertyName("version");
            writer.Write(1);

            writer.WritePropertyName("items");
            writer.WriteArrayStart();

            foreach (var row in rows)
            {
                writer.WriteObjectStart();

                foreach (var col in schema.Columns)
                {
                    writer.WritePropertyName(col.PascalName);

                    if (row.Values.TryGetValue(col.Name, out var value))
                    {
                        registry.WriteJsonValue(writer, value, col);
                    }
                    else
                    {
                        writer.Write(null);
                    }
                }

                writer.WriteObjectEnd();
            }

            writer.WriteArrayEnd();
            writer.WriteObjectEnd();

            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}