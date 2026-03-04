using System;
using System.Collections.Generic;
using System.Globalization;

namespace LiteQuark.Editor
{
    internal sealed class ArrayConverter : ITypeConverter
    {
        public ConfigFieldType FieldType => ConfigFieldType.IntArray;

        public bool CanConvert(string typeRaw)
        {
            return typeRaw != null && typeRaw.EndsWith("[]");
        }

        public object Convert(string value)
        {
            return value ?? string.Empty;
        }

        public void WriteJson(LitJson.JsonWriter writer, object value)
        {
        }

        public static ConfigFieldType GetArrayFieldType(string typeRaw)
        {
            switch (typeRaw)
            {
                case "int[]": return ConfigFieldType.IntArray;
                case "long[]": return ConfigFieldType.LongArray;
                case "float[]": return ConfigFieldType.FloatArray;
                case "double[]": return ConfigFieldType.DoubleArray;
                case "bool[]": return ConfigFieldType.BoolArray;
                case "string[]": return ConfigFieldType.StringArray;
                case "int[][]": return ConfigFieldType.IntArray2D;
                case "long[][]": return ConfigFieldType.LongArray2D;
                case "float[][]": return ConfigFieldType.FloatArray2D;
                case "double[][]": return ConfigFieldType.DoubleArray2D;
                case "string[][]": return ConfigFieldType.StringArray2D;
                case "int[][][]": return ConfigFieldType.IntArray3D;
                default: return ConfigFieldType.StringArray;
            }
        }

        public static object ConvertArray(string value, ConfigFieldType fieldType)
        {
            if (string.IsNullOrEmpty(value))
            {
                return GetEmptyArray(fieldType);
            }

            switch (fieldType)
            {
                case ConfigFieldType.IntArray: return ParseArray1D(value, s => int.Parse(s, CultureInfo.InvariantCulture));
                case ConfigFieldType.LongArray: return ParseArray1D(value, s => long.Parse(s, CultureInfo.InvariantCulture));
                case ConfigFieldType.FloatArray: return ParseArray1D(value, s => float.Parse(s, CultureInfo.InvariantCulture));
                case ConfigFieldType.DoubleArray: return ParseArray1D(value, s => double.Parse(s, CultureInfo.InvariantCulture));
                case ConfigFieldType.BoolArray: return ParseBoolArray(value);
                case ConfigFieldType.StringArray: return ParseStringArray(value);
                case ConfigFieldType.IntArray2D: return ParseArray2D(value, s => int.Parse(s, CultureInfo.InvariantCulture));
                case ConfigFieldType.LongArray2D: return ParseArray2D(value, s => long.Parse(s, CultureInfo.InvariantCulture));
                case ConfigFieldType.FloatArray2D: return ParseArray2D(value, s => float.Parse(s, CultureInfo.InvariantCulture));
                case ConfigFieldType.DoubleArray2D: return ParseArray2D(value, s => double.Parse(s, CultureInfo.InvariantCulture));
                case ConfigFieldType.StringArray2D: return ParseStringArray2D(value);
                case ConfigFieldType.IntArray3D: return ParseArray3D(value, s => int.Parse(s, CultureInfo.InvariantCulture));
                default: return GetEmptyArray(fieldType);
            }
        }

        public static void WriteArrayJson(LitJson.JsonWriter writer, object value, ConfigFieldType fieldType)
        {
            switch (fieldType)
            {
                case ConfigFieldType.IntArray:
                    WriteArray1D(writer, (int[])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.LongArray:
                    WriteArray1D(writer, (long[])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.FloatArray:
                    WriteArray1D(writer, (float[])value, (w, v) => w.Write((double)v));
                    break;
                case ConfigFieldType.DoubleArray:
                    WriteArray1D(writer, (double[])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.BoolArray:
                    WriteArray1D(writer, (bool[])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.StringArray:
                    WriteArray1D(writer, (string[])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.IntArray2D:
                    WriteArray2D(writer, (int[][])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.LongArray2D:
                    WriteArray2D(writer, (long[][])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.FloatArray2D:
                    WriteArray2D(writer, (float[][])value, (w, v) => w.Write((double)v));
                    break;
                case ConfigFieldType.DoubleArray2D:
                    WriteArray2D(writer, (double[][])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.StringArray2D:
                    WriteArray2D(writer, (string[][])value, (w, v) => w.Write(v));
                    break;
                case ConfigFieldType.IntArray3D:
                    WriteArray3D(writer, (int[][][])value, (w, v) => w.Write(v));
                    break;
            }
        }

        private static T[] ParseArray1D<T>(string value, Func<string, T> parse)
        {
            var parts = value.Split(';');
            var result = new List<T>();
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add(parse(trimmed));
                }
            }
            return result.ToArray();
        }

        private static bool[] ParseBoolArray(string value)
        {
            var parts = value.Split(';');
            var result = new List<bool>();
            foreach (var part in parts)
            {
                var trimmed = part.Trim().ToLowerInvariant();
                if (trimmed.Length > 0)
                {
                    result.Add(trimmed == "true" || trimmed == "1" || trimmed == "yes");
                }
            }
            return result.ToArray();
        }

        private static string[] ParseStringArray(string value)
        {
            var parts = value.Split(';');
            var result = new List<string>();
            foreach (var part in parts)
            {
                result.Add(part.Trim());
            }
            return result.ToArray();
        }

        private static T[][] ParseArray2D<T>(string value, Func<string, T> parse)
        {
            var rows = value.Split('|');
            var result = new List<T[]>();
            foreach (var row in rows)
            {
                var trimmedRow = row.Trim();
                if (trimmedRow.Length > 0)
                {
                    result.Add(ParseArray1D(trimmedRow, parse));
                }
            }
            return result.ToArray();
        }

        private static string[][] ParseStringArray2D(string value)
        {
            var rows = value.Split('|');
            var result = new List<string[]>();
            foreach (var row in rows)
            {
                var trimmedRow = row.Trim();
                if (trimmedRow.Length > 0)
                {
                    result.Add(ParseStringArray(trimmedRow));
                }
            }
            return result.ToArray();
        }

        private static T[][][] ParseArray3D<T>(string value, Func<string, T> parse)
        {
            var layers = value.Split('^');
            var result = new List<T[][]>();
            foreach (var layer in layers)
            {
                var trimmedLayer = layer.Trim();
                if (trimmedLayer.Length > 0)
                {
                    result.Add(ParseArray2D(trimmedLayer, parse));
                }
            }
            return result.ToArray();
        }

        private static void WriteArray1D<T>(LitJson.JsonWriter writer, T[] arr, Action<LitJson.JsonWriter, T> write)
        {
            writer.WriteArrayStart();
            foreach (var item in arr)
            {
                write(writer, item);
            }
            writer.WriteArrayEnd();
        }

        private static void WriteArray2D<T>(LitJson.JsonWriter writer, T[][] arr, Action<LitJson.JsonWriter, T> write)
        {
            writer.WriteArrayStart();
            foreach (var row in arr)
            {
                WriteArray1D(writer, row, write);
            }
            writer.WriteArrayEnd();
        }

        private static void WriteArray3D<T>(LitJson.JsonWriter writer, T[][][] arr, Action<LitJson.JsonWriter, T> write)
        {
            writer.WriteArrayStart();
            foreach (var layer in arr)
            {
                WriteArray2D(writer, layer, write);
            }
            writer.WriteArrayEnd();
        }

        private static object GetEmptyArray(ConfigFieldType fieldType)
        {
            switch (fieldType)
            {
                case ConfigFieldType.IntArray: return Array.Empty<int>();
                case ConfigFieldType.LongArray: return Array.Empty<long>();
                case ConfigFieldType.FloatArray: return Array.Empty<float>();
                case ConfigFieldType.DoubleArray: return Array.Empty<double>();
                case ConfigFieldType.BoolArray: return Array.Empty<bool>();
                case ConfigFieldType.StringArray: return Array.Empty<string>();
                case ConfigFieldType.IntArray2D: return Array.Empty<int[]>();
                case ConfigFieldType.LongArray2D: return Array.Empty<long[]>();
                case ConfigFieldType.FloatArray2D: return Array.Empty<float[]>();
                case ConfigFieldType.DoubleArray2D: return Array.Empty<double[]>();
                case ConfigFieldType.StringArray2D: return Array.Empty<string[]>();
                case ConfigFieldType.IntArray3D: return Array.Empty<int[][]>();
                default: return Array.Empty<object>();
            }
        }
    }
}