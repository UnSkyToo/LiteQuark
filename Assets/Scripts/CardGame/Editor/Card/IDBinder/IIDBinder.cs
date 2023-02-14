using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace LiteCard.Editor
{
    public interface IIDBinder
    {
        int ToID(object instance, int id);
        
        void Draw(string title, object instance, int id);
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
        private static Dictionary<Type, IIDBinder> BinderCache_ = new Dictionary<Type, IIDBinder>();

        public static int Draw(string title, object instance, object value)
        {
            var type = instance.GetType();
            if (!BinderCache_.TryGetValue(type, out var binder))
            {
                binder = GetIDBinder(type);
                BinderCache_.Add(type, binder);
            }

            var newValue = (int)value;
            if (binder == null)
            {
                newValue = EditorGUILayout.IntField(title, newValue);
            }
            else
            {
                binder.Draw(title, instance, newValue);
                newValue = binder.ToID(instance, newValue);
            }

            return newValue;
        }
        
        private static IIDBinder GetIDBinder(Type idType)
        {
            var typeList = TypeCache.GetTypesDerivedFrom<IIDBinder>();
            
            foreach (var type in typeList)
            {
                var attr = type.GetCustomAttribute(typeof(IDBindTypeAttribute), false) as IDBindTypeAttribute;
                if (attr != null && attr.BindType == idType)
                {
                    return Activator.CreateInstance(type) as IIDBinder;
                }
            }

            return null;
        }
    }
}