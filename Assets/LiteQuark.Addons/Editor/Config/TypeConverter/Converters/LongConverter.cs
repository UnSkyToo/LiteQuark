using System.Globalization;

namespace LiteQuark.Editor
{
    internal sealed class LongConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.Long;

        public bool CanConvert(string typeRaw) => typeRaw == "long";

        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0L;
            return long.Parse(value, CultureInfo.InvariantCulture);
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            writer.Write((long)value);
        }
    }
}