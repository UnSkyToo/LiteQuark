using System;
using System.Globalization;

namespace LiteQuark.Editor
{
    internal sealed class EnumConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.Enum;

        public bool CanConvert(string typeRaw)
        {
            return typeRaw != null && typeRaw.StartsWith("enum:", StringComparison.OrdinalIgnoreCase);
        }

        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            writer.Write((int)value);
        }

        public static string GetEnumTypeName(string typeRaw)
        {
            if (typeRaw != null && typeRaw.StartsWith("enum:", StringComparison.OrdinalIgnoreCase))
            {
                return typeRaw.Substring(5).Trim();
            }

            return null;
        }
    }
}