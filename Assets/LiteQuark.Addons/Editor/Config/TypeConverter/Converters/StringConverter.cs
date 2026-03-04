namespace LiteQuark.Editor
{
    internal sealed class StringConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.String;

        public bool CanConvert(string typeRaw) => typeRaw == "string";

        public object Convert(string value)
        {
            return value ?? string.Empty;
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            writer.Write((string)value);
        }
    }
}