using System.Collections.Generic;

namespace LiteQuark.Editor
{
    internal static class Validator
    {
        public static List<ValidationError> Validate(TableSchema schema, List<RowData> rows)
        {
            var errors = new List<ValidationError>();

            // Check that there is an id column
            ColumnSchema idColumn = null;
            foreach (var col in schema.Columns)
            {
                if (col.IsId)
                {
                    idColumn = col;
                    break;
                }
            }

            if (idColumn == null)
            {
                errors.Add(new ValidationError(0, 0, "id", "Table is missing an 'id' column."));
                return errors;
            }

            // Check id type is int
            if (idColumn.FieldType != ConfigFieldType.Int)
            {
                errors.Add(new ValidationError(0, idColumn.Index, "id", $"'id' column must be of type 'int', got '{idColumn.TypeRaw}'."));
            }

            // Validate rows
            var idSet = new HashSet<int>();

            foreach (var row in rows)
            {
                // Check id exists and is valid
                if (!row.Values.TryGetValue(idColumn.Name, out var idValue))
                {
                    errors.Add(new ValidationError(row.RowIndex, idColumn.Index, "id", "Missing id value."));
                    continue;
                }

                if (!(idValue is int id))
                {
                    errors.Add(new ValidationError(row.RowIndex, idColumn.Index, "id", $"Id value is not an integer: {idValue}"));
                    continue;
                }

                // Check id is positive
                if (id <= 0)
                {
                    errors.Add(new ValidationError(row.RowIndex, idColumn.Index, "id", $"Id must be a positive integer, got {id}."));
                }

                // Check id uniqueness
                if (!idSet.Add(id))
                {
                    errors.Add(new ValidationError(row.RowIndex, idColumn.Index, "id", $"Duplicate id: {id}."));
                }

                // Check required fields (non-null)
                foreach (var col in schema.Columns)
                {
                    if (col.IsId) continue;

                    if (!row.Values.ContainsKey(col.Name))
                    {
                        errors.Add(new ValidationError(row.RowIndex, col.Index, col.Name, "Missing value."));
                    }
                }
            }

            return errors;
        }
    }
}