using System.Globalization;

namespace LiteQuark.Editor
{
    internal sealed class Vector3Converter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.Vector3;

        public bool CanConvert(string typeRaw) => typeRaw == "vector3";

        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value)) return new float[] { 0f, 0f, 0f };
            var parts = value.Split(';');
            return new float[]
            {
                parts.Length > 0 ? float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture) : 0f,
                parts.Length > 1 ? float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture) : 0f,
                parts.Length > 2 ? float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture) : 0f,
            };
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            var arr = (float[])value;
            writer.WriteObjectStart();
            writer.WritePropertyName("x"); writer.Write((double)arr[0]);
            writer.WritePropertyName("y"); writer.Write((double)arr[1]);
            writer.WritePropertyName("z"); writer.Write((double)arr[2]);
            writer.WriteObjectEnd();
        }
    }
}