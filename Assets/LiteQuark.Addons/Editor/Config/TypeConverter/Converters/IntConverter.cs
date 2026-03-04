using System.Globalization;

namespace LiteQuark.Editor
{
    internal sealed class IntConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.Int;

        public bool CanConvert(string typeRaw) => typeRaw == "int";

        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            writer.Write((int)value);
        }
    }
}