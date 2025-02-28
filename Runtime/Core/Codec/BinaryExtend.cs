using System;
using System.IO;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class BinaryExtend
    {
        public static void Write(this BinaryWriter writer, Enum value)
        {
            writer.Write(value.GetType());
            writer.Write(value.ToString());
        }
        
        public static Enum ReadEnum(this BinaryReader reader)
        {
            var type = reader.ReadType();
            var value = reader.ReadString();
            if (Enum.TryParse(type, value, out var enumValue))
            {
                return enumValue as Enum;
            }

            return default;
        }

        public static void Write(this BinaryWriter writer, Rect value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.width);
            writer.Write(value.height);
        }
        
        public static Rect ReadRect(this BinaryReader reader)
        {
            return new Rect(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this BinaryWriter writer, RectInt value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.width);
            writer.Write(value.height);
        }
        
        public static RectInt ReadRectInt(this BinaryReader reader)
        {
            return new RectInt(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }
        
        public static void Write(this BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }
        
        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this BinaryWriter writer, Vector2Int value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }
        
        public static Vector2Int ReadVector2Int(this BinaryReader reader)
        {
            return new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
        }
        
        public static void Write(this BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }
        
        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this BinaryWriter writer, Vector3Int value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }
        
        public static Vector3Int ReadVector3Int(this BinaryReader reader)
        {
            return new Vector3Int(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }
        
        public static void Write(this BinaryWriter writer, Vector4 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }
        
        public static Vector4 ReadVector4(this BinaryReader reader)
        {
            return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
        
        public static void Write(this BinaryWriter writer, Color value)
        {
            writer.Write(value.r);
            writer.Write(value.g);
            writer.Write(value.b);
            writer.Write(value.a);
        }
        
        public static Color ReadColor(this BinaryReader reader)
        {
            return new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this BinaryWriter writer, Type type)
        {
            var assemblyQualifiedName = type?.AssemblyQualifiedName ?? string.Empty;
            // 减少序列化Type的大小，系统程序集直接使用FullName
            if (type.Assembly.FullName.Contains("mscorlib"))
            {
                assemblyQualifiedName = string.Empty;
            }
            writer.Write(assemblyQualifiedName);
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            {
                writer.Write(type?.FullName ?? string.Empty);
            }
        }
        
        public static Type ReadType(this BinaryReader reader)
        {
            var assemblyQualifiedName = reader.ReadString();
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            {
                return Type.GetType(reader.ReadString());
            }
            return Type.GetType(assemblyQualifiedName);
        }
    }
}