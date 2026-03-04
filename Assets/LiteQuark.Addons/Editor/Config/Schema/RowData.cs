using System.Collections.Generic;

namespace LiteQuark.Editor
{
    internal sealed class RowData
    {
        public int RowIndex { get; }
        public Dictionary<string, object> Values { get; }

        public RowData(int rowIndex)
        {
            RowIndex = rowIndex;
            Values = new Dictionary<string, object>();
        }
    }
}