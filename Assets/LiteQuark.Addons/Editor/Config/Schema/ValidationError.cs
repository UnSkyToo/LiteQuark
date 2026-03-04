namespace LiteQuark.Editor
{
    internal sealed class ValidationError
    {
        public int Row { get; }
        public int Column { get; }
        public string ColumnName { get; }
        public string Message { get; }

        public ValidationError(int row, int column, string columnName, string message)
        {
            Row = row;
            Column = column;
            ColumnName = columnName;
            Message = message;
        }

        public override string ToString()
        {
            return $"[Row {Row}, Column {Column} ({ColumnName})]: {Message}";
        }
    }
}