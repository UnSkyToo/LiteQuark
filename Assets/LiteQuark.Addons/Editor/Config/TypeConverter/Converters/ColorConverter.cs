using System.Globalization;

namespace LiteQuark.Editor
{
    internal sealed class ColorConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.Color;

        public bool CanConvert(string typeRaw) => typeRaw == "color";

        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value)) return new float[] { 0f, 0f, 0f, 1f };
            var parts = value.Split(';');
            return new float[]
            {
                parts.Length > 0 ? float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture) : 0f,
                parts.Length > 1 ? float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture) : 0f,
                parts.Length > 2 ? float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture) : 0f,
                parts.Length > 3 ? float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture) : 1f,
            };
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            var arr = (float[])value;
            writer.WriteObjectStart();
            writer.WritePropertyName("r"); writer.Write((double)arr[0]);
            writer.WritePropertyName("g"); writer.Write((double)arr[1]);
            writer.WritePropertyName("b"); writer.Write((double)arr[2]);
            writer.WritePropertyName("a"); writer.Write((double)arr[3]);
            writer.WriteObjectEnd();
        }
    }
}