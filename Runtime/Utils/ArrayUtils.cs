using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public static class ArrayUtils
    {   
        public static int IndexOf(this int[] array, int value)
        {
            for (var index = 0; index < array.Length; ++index)
            {
                if (array[index] == value)
                {
                    return index;
                }
            }

            return -1;
        }

        public static T[] Clone<T>(this T[] array)
        {
            return CloneArray(array);
        }
        
        public static T[] CloneArray<T>(T[] array)
        {
            if (array == null)
            {
                return null;
            }
            
            var result = new T[array.Length];

            for (var index = 0; index < array.Length; ++index)
            {
                if (array[index] is ICloneable clone)
                {
                    result[index] = (T)clone.Clone();
                }
                else
                {
                    result[index] = array[index];
                }
            }

            return result;
        }
        
        public static object[] ToArray(this IList list)
        {
            var result = new object[list.Count];
            for (var index = 0; index < list.Count; ++index)
            {
                result[index] = list[index];
            }
            return result;
        }

        public static IList ToArray(this IList list, Type type)
        {
            var result = (IList)Array.CreateInstance(type, list.Count);
            for (var index = 0; index < result.Count; ++index)
            {
                result[index] = list[index];
            }
            return result;
        }
    }
}