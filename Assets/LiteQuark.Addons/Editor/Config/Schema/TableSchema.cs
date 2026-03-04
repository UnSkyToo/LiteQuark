using System.Collections.Generic;

namespace LiteQuark.Editor
{
    internal sealed class TableSchema
    {
        public string Name { get; }
        public string SourceFile { get; }
        public string SheetName { get; }
        public List<ColumnSchema> Columns { get; }

        public TableSchema(string name, string sourceFile, string sheetName, List<ColumnSchema> columns)
        {
            Name = name;
            SourceFile = sourceFile;
            SheetName = sheetName;
            Columns = columns;
        }

        public string PascalName => NameUtils.ToPascalCase(Name);
    }
}