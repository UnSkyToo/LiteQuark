using System.Globalization;

namespace LiteQuark.Editor
{
    internal sealed class DoubleConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.Double;

        public bool CanConvert(string typeRaw) => typeRaw == "double";

        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0.0;
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            writer.Write((double)value);
        }
    }
}