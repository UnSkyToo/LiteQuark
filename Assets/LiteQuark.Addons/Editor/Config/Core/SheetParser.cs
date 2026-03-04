using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace LiteQuark.Editor
{
    internal static class SheetParser
    {
        /// <summary>
        /// Parse a DataTable into a TableSchema and row data list.
        /// Sheet layout:
        /// Row 0: field names (snake_case)
        /// Row 1: field types (int, string, float[], enum:XXX, etc.)
        /// Row 2: comments/descriptions (ignored)
        /// Row 3+: data rows
        /// </summary>
        public static (TableSchema schema, List<RowData> rows) Parse(DataTable table, string sourceFile)
        {
            if (table.Rows.Count < 3)
            {
                throw new Exception($"Sheet '{table.TableName}' has fewer than 3 rows (need name, type, comment rows).");
            }

            var nameRow = table.Rows[0];
            var typeRow = table.Rows[1];

            var columns = new List<ColumnSchema>();
            var registry = TypeConverterRegistry.Instance;

            for (var col = 0; col < table.Columns.Count; col++)
            {
                var name = nameRow[col]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                // Skip columns starting with # (comment columns)
                if (name.StartsWith("#"))
                {
                    continue;
                }

                var typeRaw = typeRow[col]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(typeRaw))
                {
                    continue;
                }

                var fieldType = registry.ResolveFieldType(typeRaw);
                var isId = name.ToLowerInvariant() == "id";
                var enumTypeName = fieldType == ConfigFieldType.Enum ? EnumConverter.GetEnumTypeName(typeRaw) : null;

                columns.Add(new ColumnSchema(name, typeRaw, fieldType, col, isId, enumTypeName));
            }

            var schema = new TableSchema(table.TableName, sourceFile, table.TableName, columns);

            var rows = new List<RowData>();
            for (var rowIdx = 3; rowIdx < table.Rows.Count; rowIdx++)
            {
                var dataRow = table.Rows[rowIdx];

                // Skip empty rows (check if first column is empty)
                var firstVal = dataRow[0]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(firstVal))
                {
                    continue;
                }

                // Skip rows starting with #
                if (firstVal.StartsWith("#"))
                {
                    continue;
                }

                var rowData = new RowData(rowIdx + 1); // 1-based for user-facing row numbers

                foreach (var column in columns)
                {
                    var cellValue = dataRow[column.Index]?.ToString()?.Trim() ?? string.Empty;

                    // Handle Excel numeric values (doubles for int/long columns)
                    var rawValue = dataRow[column.Index];
                    if (rawValue is double doubleVal)
                    {
                        switch (column.FieldType)
                        {
                            case ConfigFieldType.Int:
                            case ConfigFieldType.Enum:
                                cellValue = ((int)doubleVal).ToString(CultureInfo.InvariantCulture);
                                break;
                            case ConfigFieldType.Long:
                                cellValue = ((long)doubleVal).ToString(CultureInfo.InvariantCulture);
                                break;
                            case ConfigFieldType.Float:
                                cellValue = ((float)doubleVal).ToString(CultureInfo.InvariantCulture);
                                break;
                            case ConfigFieldType.Double:
                                cellValue = doubleVal.ToString(CultureInfo.InvariantCulture);
                                break;
                            case ConfigFieldType.Bool:
                                cellValue = doubleVal != 0 ? "true" : "false";
                                break;
                        }
                    }

                    var value = registry.ConvertValue(cellValue, column);
                    rowData.Values[column.Name] = value;
                }

                rows.Add(rowData);
            }

            return (schema, rows);
        }
    }
}