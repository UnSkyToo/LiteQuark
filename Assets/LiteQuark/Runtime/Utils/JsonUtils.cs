using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LiteQuark.Runtime
{
    public static class JsonUtils
    {
        private static List<Assembly> AssemblyList_ = new List<Assembly>();

        static JsonUtils()
        {
            AssemblyList_.Clear();
            AddAssembly(typeof(JsonUtils).Assembly);
            AddAssembly(Assembly.GetExecutingAssembly());
            AddAssembly(Assembly.GetEntryAssembly());
        }
        
        public static void AddAssembly(Assembly assembly, int index = -1)
        {
            if (assembly == null)
            {
                return;
            }
            
            if (AssemblyList_.Contains(assembly))
            {
                return;
            }

            if (index < 0 || index >= AssemblyList_.Count)
            {
                AssemblyList_.Add(assembly);
            }
            else
            {
                AssemblyList_.Insert(index, assembly);
            }
        }
        
        public static string EncodeArray<T>(T[] value) where T : IJsonMainConfig
        {
            AddAssembly(Assembly.GetCallingAssembly());
            
            var textBuilder = new StringBuilder();
            var writer = new JsonTextWriter(new StringWriter(textBuilder))
            {
                Formatting = Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            };

            try
            {
                WriteDataArray(writer, value.GetType().GetElementType(), value);
                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                LLog.Error($"json encode error\n{ex.Message}");
                return string.Empty;
            }
        }

        private static void WriteData(JsonTextWriter writer, string key, Type type, object value)
        {
            writer.WritePropertyName(key);
            WriteData(writer, type, value);
        }

        private static void WriteData(JsonTextWriter writer, Type type, object value)
        {
            writer.WriteStartObject();
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                WriteProperty(writer, value, property);
            }
            writer.WriteEndObject();
        }

        private static void WriteProperty(JsonTextWriter writer, object instance, PropertyInfo property)
        {
            var key = property.Name;
            var value = property.GetValue(instance);
            
            if (typeof(IJsonConfig).IsAssignableFrom(property.PropertyType))
            {
                WriteData(writer, key, property.PropertyType, value);
            }
            else if (TypeUtils.IsListType(property.PropertyType))
            {
                var elementType = TypeUtils.GetElementType(property.PropertyType);
                if (typeof(IJsonConfig).IsAssignableFrom(elementType))
                {
                    WriteDataArray(writer, key, elementType, (IList)value);
                }
                else
                {
                    WriteObjectArray(writer, key, (IList)value);
                }
            }
            else if (property.PropertyType.IsArray)
            {
                var elementType = TypeUtils.GetElementType(property.PropertyType);
                if (typeof(IJsonConfig).IsAssignableFrom(elementType))
                {
                    WriteDataArray(writer, key, elementType, (IList)value);
                }
                else
                {
                    WriteObjectArray(writer, key, (IList)value);
                }
            }
            else
            {
                WriteObject(writer, key, value);
            }
        }

        private static void WriteDataArray<T>(JsonTextWriter writer, string key, Type type, T[] list) where T : IJsonConfig
        {
            writer.WritePropertyName(key);
            WriteDataArray(writer, type, list);
        }

        private static void WriteDataArray<T>(JsonTextWriter writer, Type type, T[] list) where T : IJsonConfig
        {
            WriteDataArray(writer, type, (IList)list);
        }

        private static void WriteDataArray(JsonTextWriter writer, string key, Type type, IList list)
        {
            writer.WritePropertyName(key);
            WriteDataArray(writer, type, list);
        }

        private static void WriteDataArray(JsonTextWriter writer, Type type, IList list)
        {
            writer.WriteStartArray();
        
            foreach (var value in list)
            {
                WriteData(writer, type, value);
            }
            
            writer.WriteEndArray();
        }
        
        private static void WriteObject(JsonTextWriter writer, string key, object value)
        {
            writer.WritePropertyName(key);
            WriteObject(writer, value);
        }

        private static void WriteObject(JsonTextWriter writer, object value)
        {
            switch (value)
            {
                case null:
                    writer.WriteNull();
                    break;
                case bool boolValue:
                    writer.WriteValue(boolValue);
                    break;
                case int intValue:
                    writer.WriteValue(intValue);
                    break;
                case float floatValue:
                    writer.WriteValue(floatValue);
                    break;
                case string stringValue:
                    writer.WriteValue(stringValue);
                    break;
                case Enum enumValue:
                    writer.WriteValue($"{enumValue.GetType().FullName}~{enumValue}");
                    break;
                default:
                    throw new ArgumentException($"error simple json value : {value}");
            }
        }

        private static void WriteObjectArray(JsonTextWriter writer, string key, IList list)
        {
            writer.WritePropertyName(key);
            WriteObjectArray(writer, list);
        }

        private static void WriteObjectArray(JsonTextWriter writer, IList list)
        {
            writer.WriteStartArray();
            
            foreach (var value in list)
            {
                WriteObject(writer, value);
            }
            
            writer.WriteEndArray();
        }

        public static object[] DecodeArray(string jsonText, Type type)
        {
            AddAssembly(Assembly.GetCallingAssembly());
            
            try
            {
                var reader = JArray.Parse(jsonText);

                var value = ReadDataArray(reader, type);
                var result = value.Cast<object>().ToArray();
                return result;
            }
            catch (Exception ex)
            {
                LLog.Error($"json decode error\n{ex.Message}");
                return null;
            }
        }

        private static object ReadData(JToken reader, Type type)
        {
            var instance = Activator.CreateInstance(type);
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                var value = ReadProperty(reader[property.Name], property.PropertyType);
                property.SetValue(instance, value);
            }

            return instance;
        }

        private static object ReadProperty(JToken reader, Type type)
        {
            if (reader == null)
            {
                return TypeUtils.CreateInstance(type);
            }

            switch (reader.Type)
            {
                case JTokenType.Array:
                    var elementType = TypeUtils.GetElementType(type);
                    if (typeof(IJsonConfig).IsAssignableFrom(elementType))
                    {
                        if (type.IsArray)
                        {
                            return ReadDataArray(reader.Value<JArray>(), elementType);
                        }
                        else
                        {
                            var list = TypeUtils.CreateInstance(type) as IList;
                            foreach (var item in ReadDataArray(reader.Value<JArray>(), elementType))
                            {
                                list.Add(item);
                            }
                            return list;
                        }
                    }
                    else
                    {
                        if (type.IsArray)
                        {
                            return ReadObjectArray(reader.Value<JArray>(), elementType);
                        }
                        else
                        {
                            var list = TypeUtils.CreateInstance(type) as IList;
                            foreach (var item in ReadObjectArray(reader.Value<JArray>(), elementType))
                            {
                                list.Add(item);
                            }
                            return list;
                        }
                    }
                case JTokenType.Object:
                    return ReadData(reader, type);
                default:
                    return ReadObject(reader);
            }
        }

        private static IList ReadDataArray(JArray reader, Type type)
        {
            var result = TypeUtils.CreateInstance(type, reader.Count) as IList;

            for (var index = 0; index < reader.Count; ++index)
            {
                result[index] = ReadData(reader[index], type);
            }

            return result;
        }

        private static IList ReadObjectArray(JArray reader, Type type)
        {
            var result = TypeUtils.CreateInstance(type, reader.Count) as IList;

            for (var index = 0; index < reader.Count; ++index)
            {
                result[index] = ReadObject(reader[index]);
            }

            return result;
        }

        private static object ReadObject(JToken reader)
        {
            switch (reader.Type)
            {
                case JTokenType.Null:
                    return null;
                case JTokenType.Boolean:
                    return reader.Value<bool>();
                case JTokenType.Integer:
                    return reader.Value<int>();
                case JTokenType.Float:
                    return reader.Value<float>();
                case JTokenType.String:
                    var str = reader.Value<string>();
                    if (str.Contains("~"))
                    {
                        return ReadEnumFromString(str);
                    }
                    return str;
                default:
                    return null;
            }
            
            object ReadEnumFromString(string value)
            {
                if (!value.Contains('.'))
                {
                    return null;
                }
                
                var chunks = value.Split('~');
                if (chunks.Length != 2)
                {
                    return null;
                }
                
                var type = TypeUtils.GetTypeWithAssembly(AssemblyList_.ToArray(), chunks[0]);
                if (type == null)
                {
                    return null;
                }

                if (!Enum.TryParse(type, chunks[1], false, out var result))
                {
                    return null;
                }

                return result;
            }
        }
    }
}