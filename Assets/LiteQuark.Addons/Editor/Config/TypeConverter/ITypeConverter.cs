namespace LiteQuark.Editor
{
    internal interface ITypeConverter
    {
        ConfigFieldType FieldType { get; }

        bool CanConvert(string typeRaw);
        object Convert(string value);
        void WriteJson(LitJson.JsonWriter writer, object value);
    }
}