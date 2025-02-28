using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class Encoder
    {
        private readonly List<Type> TypeList_ = new List<Type>();
        
        public Encoder()
        {
        }
        
        public byte[] Encode<T>(T value)
        {
            TypeList_.Clear();
            var result = new List<byte>();
            
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    Encode(writer, value);
                }
                result.AddRange(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    WriteTypeList(writer);
                }
                result.InsertRange(0, stream.ToArray());
            }

            return result.ToArray();
        }

        private void Encode<T>(BinaryWriter writer, T value)
        {
            Encode(writer, value, typeof(T));
        }

        private void Encode(BinaryWriter writer, object value, Type type)
        {
            switch (value)
            {
                case bool boolValue:
                    writer.Write(boolValue);
                    break;
                case byte byteValue:
                    writer.Write(byteValue);
                    break;
                case sbyte sbyteValue:
                    writer.Write(sbyteValue);
                    break;
                case short shortValue:
                    writer.Write(shortValue);
                    break;
                case ushort ushortValue:
                    writer.Write(ushortValue);
                    break;
                case int intValue:
                    writer.Write(intValue);
                    break;
                case uint uintValue:
                    writer.Write(uintValue);
                    break;
                case long longValue:
                    writer.Write(longValue);
                    break;
                case ulong ulongValue:
                    writer.Write(ulongValue);
                    break;
                case float floatValue:
                    writer.Write(floatValue);
                    break;
                case double doubleValue:
                    writer.Write(doubleValue);
                    break;
                case string stringValue:
                    writer.Write(stringValue);
                    break;
                case Enum enumValue:
                    writer.Write(enumValue);
                    break;
                case Rect rectValue:
                    writer.Write(rectValue);
                    break;
                case RectInt rectIntValue:
                    writer.Write(rectIntValue);
                    break;
                case Vector2 vector2Value:
                    writer.Write(vector2Value);
                    break;
                case Vector2Int vector2IntValue:
                    writer.Write(vector2IntValue);
                    break;
                case Vector3 vector3Value:
                    writer.Write(vector3Value);
                    break;
                case Vector3Int vector3IntValue:
                    writer.Write(vector3IntValue);
                    break;
                case Vector4 vector4Value:
                    writer.Write(vector4Value);
                    break;
                case Color colorValue:
                    writer.Write(colorValue);
                    break;
                case IList list:
                    EncodeList(writer, list, type);
                    break;
                case IDictionary dictionary:
                    EncodeDictionary(writer, dictionary, type);
                    break;
                case { } objectValue:
                    EncodeObject(writer, objectValue, type);
                    break;
                case null:
                    writer.Write(true);
                    break;
                default:
                    throw new Exception($"Unsupported type: {type}");
            }
        }

        private void EncodeList(BinaryWriter writer, IList list, Type type)
        {
            writer.Write(list.Count);
            foreach (var item in list)
            {
                var itemType = item?.GetType() ?? TypeUtils.GetElementType(type);
                WriteType(writer,  itemType);
                Encode(writer, item, itemType);
            }
        }

        private void EncodeDictionary(BinaryWriter writer, IDictionary dictionary, Type type)
        {
            writer.Write(dictionary.Count);
            foreach (DictionaryEntry item in dictionary)
            {
                var itemType = item.Key?.GetType() ?? TypeUtils.GetElementType(type);
                WriteType(writer, itemType);
                Encode(writer, item.Key, itemType);
                itemType = item.Value?.GetType() ?? TypeUtils.GetElementType(type);
                WriteType(writer, itemType);
                Encode(writer, item.Value, itemType);
            }
        }

        private void EncodeObject(BinaryWriter writer, object objectValue, Type type)
        {
            if (!type.IsPrimitive && (type.IsClass || type.IsValueType))
            {
                writer.Write(false);
                        
                var fields = type.GetFields();
                writer.Write(fields.Length);
                foreach (var field in fields)
                {
                    var fieldValue = field.GetValue(objectValue);
                    var fieldType = fieldValue?.GetType() ?? field.FieldType;
                    EncodeMember(writer, field.Name, fieldType, fieldValue);
                }

                var properties = type.GetProperties().Where(x => x.CanWrite).ToArray();
                writer.Write(properties.Length);
                foreach (var property in properties)
                {
                    var propertyValue = property.GetValue(objectValue);
                    var propertyType = propertyValue?.GetType() ?? property.PropertyType;
                    EncodeMember(writer, property.Name, propertyType, propertyValue);
                }
            }
            else
            {
                throw new Exception($"Unsupported type: {type}");
            }
        }
        
        private void EncodeMember(BinaryWriter writer, string memberName, Type memberType, object memberValue)
        {
            writer.Write(memberName);
            WriteType(writer, memberType);
            Encode(writer, memberValue, memberType);
        }

        private void WriteType(BinaryWriter writer, Type type)
        {
            var index = TypeList_.IndexOf(type);
            if (index == -1)
            {
                TypeList_.Add(type);
                index = TypeList_.Count - 1;
            }
            
            writer.Write((ushort)index);
        }

        private void WriteTypeList(BinaryWriter writer)
        {
            writer.Write(TypeList_.Count);
            foreach (var type in TypeList_)
            {
                writer.Write(type);
            }
        }
    }
}