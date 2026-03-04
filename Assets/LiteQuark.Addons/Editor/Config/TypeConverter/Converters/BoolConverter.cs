namespace LiteQuark.Editor
{
    internal sealed class BoolConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.Bool;

        public bool CanConvert(string typeRaw) => typeRaw == "bool";

        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var lower = value.Trim().ToLowerInvariant();
            return lower == "true" || lower == "1" || lower == "yes";
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
            writer.Write((bool)value);
        }
    }
}