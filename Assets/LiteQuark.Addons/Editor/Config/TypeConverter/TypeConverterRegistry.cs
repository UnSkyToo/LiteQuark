using System;
using System.Collections.Generic;

namespace LiteQuark.Editor
{
    internal sealed class TypeConverterRegistry
    {
        private readonly List<ITypeConverter> _converters = new List<ITypeConverter>();

        private static TypeConverterRegistry _instance;
        public static TypeConverterRegistry Instance => _instance ??= new TypeConverterRegistry();

        private TypeConverterRegistry()
        {
            Register(new IntConverter());
            Register(new LongConverter());
            Register(new FloatConverter());
            Register(new DoubleConverter());
            Register(new BoolConverter());
            Register(new StringConverter());
            Register(new Vector2Converter());
            Register(new Vector3Converter());
            Register(new Vector4Converter());
            Register(new ColorConverter());
            Register(new EnumConverter());
            Register(new ArrayConverter());
        }

        public void Register(ITypeConverter converter)
        {
            _converters.Add(converter);
        }

        public ITypeConverter GetConverter(string typeRaw)
        {
            var lower = typeRaw.Trim().ToLowerInvariant();
            foreach (var converter in _converters)
            {
                if (converter.CanConvert(lower))
                {
                    return converter;
                }
            }

            return null;
        }

        public ConfigFieldType ResolveFieldType(string typeRaw)
        {
            var lower = typeRaw.Trim().ToLowerInvariant();

            // Check array types first
            if (lower.EndsWith("[]"))
            {
                return ArrayConverter.GetArrayFieldType(lower);
            }

            // Check enum
            if (lower.StartsWith("enum:", StringComparison.OrdinalIgnoreCase))
            {
                return ConfigFieldType.Enum;
            }

            // Check basic types
            var converter = GetConverter(lower);
            if (converter != null)
            {
                return converter.FieldType;
            }

            throw new ArgumentException($"Unknown config field type: {typeRaw}");
        }

        public object ConvertValue(string value, ColumnSchema column)
        {
            // Array types use the ArrayConverter static methods
            switch (column.FieldType)
            {
                case ConfigFieldType.IntArray:
                case ConfigFieldType.LongArray:
                case ConfigFieldType.FloatArray:
                case ConfigFieldType.DoubleArray:
                case ConfigFieldType.BoolArray:
                case ConfigFieldType.StringArray:
                case ConfigFieldType.IntArray2D:
                case ConfigFieldType.LongArray2D:
                case ConfigFieldType.FloatArray2D:
                case ConfigFieldType.DoubleArray2D:
                case ConfigFieldType.StringArray2D:
                case ConfigFieldType.IntArray3D:
                    return ArrayConverter.ConvertArray(value, column.FieldType);
            }

            var converter = GetConverter(column.TypeRaw);
            if (converter != null)
            {
                return converter.Convert(value);
            }

            return value;
        }

        public void WriteJsonValue(LitJson.JsonWriter writer, object value, ColumnSchema column)
        {
            // Array types
            switch (column.FieldType)
            {
                case ConfigFieldType.IntArray:
                case ConfigFieldType.LongArray:
                case ConfigFieldType.FloatArray:
                case ConfigFieldType.DoubleArray:
                case ConfigFieldType.BoolArray:
                case ConfigFieldType.StringArray:
                case ConfigFieldType.IntArray2D:
                case ConfigFieldType.LongArray2D:
                case ConfigFieldType.FloatArray2D:
                case ConfigFieldType.DoubleArray2D:
                case ConfigFieldType.StringArray2D:
                case ConfigFieldType.IntArray3D:
                    ArrayConverter.WriteArrayJson(writer, value, column.FieldType);
                    return;
            }

            var converter = GetConverter(column.TypeRaw);
            if (converter != null)
            {
                converter.WriteJson(writer, value);
                return;
            }

            writer.Write(value?.ToString());
        }
    }
}