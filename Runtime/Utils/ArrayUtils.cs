using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            return CloneObjectArray(array);
        }
        
        public static T[] CloneObjectArray<T>(T[] array)
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
        
        public static T[] CloneDataArray<T>(T[] array) where T : ICloneable
        {
            if (array == null)
            {
                return null;
            }
            
            var result = new T[array.Length];

            for (var index = 0; index < array.Length; ++index)
            {
                result[index] = (T)array[index].Clone();
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

        public static T[] CombineArray<T>(T[] array1, T[] array2)
        {
            var array = new T[array1.Length + array2.Length];
            Array.Copy(array1, 0, array, 0, array1.Length);
            Array.Copy(array2, 0, array, array1.Length, array2.Length);
            return array;
        }
        
        public static T[] AppendArray<T>(T[] array1, T[] array2, bool allowSame)
        {
            if (allowSame)
            {
                return CombineArray(array1, array2);
            }

            var array = new HashSet<T>(array1);
            foreach (var item in array2)
            {
                array.Add(item);
            }

            return array.ToArray();
        }
        
        public static IList AddToList(IList list, object element)
        {
            if (list == null)
            {
                return null;
            }
            
            if (element == null)
            {
                return list;
            }
            
            if (TypeUtils.IsListType(list.GetType()))
            {
                list.Add(element);
                return list;
            }
            else
            {
                var newList = TypeUtils.CreateInstance(list.GetType(), list.Count + 1) as IList;
                if (newList == null)
                {
                    return list;
                }
                
                for (var i = 0; i < list.Count; ++i)
                {
                    newList[i] = list[i];
                }

                newList[^1] = element;
                list = newList;
                return list;
            }
        }

        public static IList RemoveFromList(IList list, int index)
        {
            if (list == null)
            {
                return null;
            }
            
            if (index < 0 || index >= list.Count)
            {
                return list;
            }
            
            if (TypeUtils.IsListType(list.GetType()))
            {
                list.RemoveAt(index);
                return list;
            }
            else
            {
                var newList = TypeUtils.CreateInstance(list.GetType(), list.Count - 1) as IList;
                if (newList == null)
                {
                    return list;
                }
                
                for (var i = 0; i < index; ++i)
                {
                    newList[i] = list[i];
                }

                for (var i = index; i < list.Count - 1; ++i)
                {
                    newList[i] = list[i + 1];
                }

                list = newList;
                return list;
            }
        }
    }
}