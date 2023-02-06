using System;
using System.Reflection;
using UnityEditor;

namespace LiteCard.Editor
{
    public interface IIDBinder
    {
        int ToID();
        
        void Draw(string title);
    }

    public sealed class IDBindTypeAttribute : Attribute
    {
        public Type BindType { get; }

        public IDBindTypeAttribute(Type bindType)
        {
            BindType = bindType;
        }
    }

    public static class IDBinder
    {
        public static int Draw(string title, object instance, object value)
        {
            var binder = GetIDBinder(instance, value);

            var newValue = (int)value;
            if (binder == null)
            {
                newValue = EditorGUILayout.IntField(title, newValue);
            }
            else
            {
                binder.Draw(title);
                newValue = binder.ToID();
            }

            return newValue;
        }
        
        private static IIDBinder GetIDBinder(object instance, object value)
        {
            var typeList = TypeCache.GetTypesDerivedFrom<IIDBinder>();
            
            foreach (var type in typeList)
            {
                var attr = type.GetCustomAttribute(typeof(IDBindTypeAttribute), false) as IDBindTypeAttribute;
                if (attr != null && attr.BindType == instance.GetType())
                {
                    return Activator.CreateInstance(type, instance, value) as IIDBinder;
                }
            }

            return null;
        }
    }
}