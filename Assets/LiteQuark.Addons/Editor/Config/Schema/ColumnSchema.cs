namespace LiteQuark.Editor
{
    internal sealed class ColumnSchema
    {
        public string Name { get; }
        public string TypeRaw { get; }
        public ConfigFieldType FieldType { get; }
        public int Index { get; }
        public bool IsId { get; }
        public string EnumTypeName { get; }

        public ColumnSchema(string name, string typeRaw, ConfigFieldType fieldType, int index, bool isId, string enumTypeName = null)
        {
            Name = name;
            TypeRaw = typeRaw;
            FieldType = fieldType;
            Index = index;
            IsId = isId;
            EnumTypeName = enumTypeName;
        }

        public string CSharpTypeName
        {
            get
            {
                switch (FieldType)
                {
                    case ConfigFieldType.Int: return "int";
                    case ConfigFieldType.Long: return "long";
                    case ConfigFieldType.Float: return "float";
                    case ConfigFieldType.Double: return "double";
                    case ConfigFieldType.Bool: return "bool";
                    case ConfigFieldType.String: return "string";
                    case ConfigFieldType.Vector2: return "UnityEngine.Vector2";
                    case ConfigFieldType.Vector3: return "UnityEngine.Vector3";
                    case ConfigFieldType.Vector4: return "UnityEngine.Vector4";
                    case ConfigFieldType.Color: return "UnityEngine.Color";
                    case ConfigFieldType.Enum: return EnumTypeName ?? "int";
                    case ConfigFieldType.IntArray: return "int[]";
                    case ConfigFieldType.LongArray: return "long[]";
                    case ConfigFieldType.FloatArray: return "float[]";
                    case ConfigFieldType.DoubleArray: return "double[]";
                    case ConfigFieldType.BoolArray: return "bool[]";
                    case ConfigFieldType.StringArray: return "string[]";
                    case ConfigFieldType.IntArray2D: return "int[][]";
                    case ConfigFieldType.LongArray2D: return "long[][]";
                    case ConfigFieldType.FloatArray2D: return "float[][]";
                    case ConfigFieldType.DoubleArray2D: return "double[][]";
                    case ConfigFieldType.StringArray2D: return "string[][]";
                    case ConfigFieldType.IntArray3D: return "int[][][]";
                    default: return "object";
                }
            }
        }

        public string PascalName => NameUtils.ToPascalCase(Name);
    }
}