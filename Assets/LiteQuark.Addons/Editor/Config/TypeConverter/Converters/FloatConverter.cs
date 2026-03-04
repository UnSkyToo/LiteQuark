using System.Globalization;

namespace LiteQuark.Editor
{
    internal sealed class FloatConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.Float;

        public bool CanConvert(string typeRaw) => typeRaw == "float";

        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0f;
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            writer.Write((double)(float)value);
        }
    }
}