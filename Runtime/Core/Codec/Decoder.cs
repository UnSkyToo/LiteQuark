using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class Decoder
    {
        private readonly List<Type> TypeList_ = new List<Type>();
        
        public Decoder()
        {
        }

        public T Decode<T>(byte[] data)
        {
            TypeList_.Clear();
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(stream))
                {
                    ReadTypeList(reader);
                    return Decode<T>(reader);
                }
            }
        }

        private T Decode<T>(BinaryReader reader)
        {
            return (T)Decode(reader, typeof(T));
        }

        private object Decode(BinaryReader reader, Type type)
        {
            switch (type)
            {
                case var t when t == typeof(bool):
                    return reader.ReadBoolean();
                case var t when t == typeof(byte):
                    return reader.ReadByte();
                case var t when t == typeof(sbyte):
                    return reader.ReadSByte();
                case var t when t == typeof(short):
                    return reader.ReadInt16();
                case var t when t == typeof(ushort):
                    return reader.ReadUInt16();
                case var t when t == typeof(int):
                    return reader.ReadInt32();
                case var t when t == typeof(uint):
                    return reader.ReadUInt32();
                case var t when t == typeof(long):
                    return reader.ReadInt64();
                case var t when t == typeof(ulong):
                    return reader.ReadUInt64();
                case var t when t == typeof(float):
                    return reader.ReadSingle();
                case var t when t == typeof(double):
                    return reader.ReadDouble();
                case var t when t == typeof(string):
                    return reader.ReadString();
                case var t when t.IsEnum:
                    return reader.ReadEnum();
                case var t when t == typeof(Rect):
                    return reader.ReadRect();
                case var t when t == typeof(RectInt):
                    return reader.ReadRectInt();
                case var t when t == typeof(Vector2):
                    return reader.ReadVector2();
                case var t when t == typeof(Vector2Int):
                    return reader.ReadVector2Int();
                case var t when t == typeof(Vector3):
                    return reader.ReadVector3();
                case var t when t == typeof(Vector3Int):
                    return reader.ReadVector3Int();
                case var t when t == typeof(Vector4):
                    return reader.ReadVector4();
                case var t when t == typeof(Color):
                    return reader.ReadColor();
                case var t when t.IsArray || TypeUtils.IsListType(t):
                    return DecodeList(reader, type);
                case var t when TypeUtils.IsDictionaryType(t):
                    return DecodeDictionary(reader, type);
                case var t when !t.IsPrimitive && (t.IsClass || t.IsValueType):
                    return DecodeObject(reader, type);
                default:
                    throw new Exception($"Unsupported type: {type}");
            }

            throw new Exception($"Unsupported type: {type}");
        }

        private IList DecodeList(BinaryReader reader, Type type)
        {
            var count = reader.ReadInt32();
            var list = TypeUtils.CreateInstance(type, count) as IList;

            for (var i = 0; i < count; i++)
            {
                var elementType = ReadType(reader);
                var item = Decode(reader, elementType);
                if (TypeUtils.IsListType(type))
                {
                    list.Add(item);
                }
                else
                {
                    list[i] = item;
                }
            }

            return list;
        }

        private IDictionary DecodeDictionary(BinaryReader reader, Type type)
        {
            var count = reader.ReadInt32();
            var dictionary = TypeUtils.CreateInstance(type) as IDictionary;
                    
            for (var i = 0; i < count; i++)
            {
                var keyType = ReadType(reader);
                var key = Decode(reader, keyType);
                var valueType = ReadType(reader);
                var value = Decode(reader, valueType);
                dictionary.Add(key, value);
            }
                    
            return dictionary;
        }

        private object DecodeObject(BinaryReader reader, Type type)
        {
            var isNull = reader.ReadBoolean();
            if (isNull)
            {
                return null;
            }

            var objectValue = TypeUtils.CreateInstance(type);

            var fieldCount = reader.ReadInt32();
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldName = reader.ReadString();
                var field = type.GetField(fieldName);
                var fieldType = ReadType(reader);
                var fieldValue = Decode(reader, fieldType);
                field.SetValue(objectValue, fieldValue);
            }

            var propertyCount = reader.ReadInt32();
            for (var i = 0; i < propertyCount; i++)
            {
                var propertyName = reader.ReadString();
                var property = type.GetProperty(propertyName);
                var propertyType = ReadType(reader);
                var propertyValue = Decode(reader, propertyType);
                property.SetValue(objectValue, propertyValue);
            }

            return objectValue;
        }
        
        private Type ReadType(BinaryReader reader)
        {
            var index = reader.ReadUInt16();
            return TypeList_[index];
        }

        private void ReadTypeList(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var type = reader.ReadType();
                TypeList_.Add(type);
            }
        }
    }
}