using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LiteCard.GamePlay;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LiteCard.Editor
{
    public static class ViewUtils
    {
        public static bool DrawDataList<T>(ref Vector2 scrollPos, List<T> value) where T : IJsonConfig
        {
            if (value == null)
            {
                value = TypeUtils.CreateInstance<List<T>>();
            }

            EditorGUI.BeginChangeCheck();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawDataList(null, value, typeof(T));
            EditorGUILayout.EndScrollView();
            return EditorGUI.EndChangeCheck();
        }

        private static IList DrawDataList(string title, object[] value, Type type)
        {
            var array = TypeUtils.CreateGenericList(type, value);
            var list = DrawDataList(title, array, type);
            return list.ToArray(type);
        }

        private static IList DrawDataList(string title, IList value, Type type)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (!string.IsNullOrWhiteSpace(title))
                {
                    EditorGUILayout.LabelField(title, GUILayout.ExpandWidth(false));
                }
                
                if (GUILayout.Button("Create"))
                {
                    value.Add(Activator.CreateInstance(type));
                }
            }

            for (var index = 0; index < value.Count; ++index)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawData(null, value[index], type);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("↑") && index > 0)
                            {
                                (value[index - 1], value[index]) = (value[index], value[index - 1]);
                            }

                            if (GUILayout.Button("↓") && index < value.Count - 1)
                            {
                                (value[index], value[index + 1]) = (value[index + 1], value[index]);
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Duplicate"))
                            {
                                var cloneVal = (value[index] as IJsonConfig)?.Clone();
                                if (cloneVal != null)
                                {
                                    value.Add(cloneVal);
                                }
                            }

                            if (GUILayout.Button("Delete"))
                            {
                                value.RemoveAt(index);
                                return value;
                            }
                        }
                    }
                }
            }

            return value;
        }

        public static object DrawData(string title, object value, Type type)
        {
            if (value == null)
            {
                value = TypeUtils.CreateInstance(type);
            }

            using (new EditorGUILayout.VerticalScope("FrameBox"))
            {
                var isFoldout = true;
                var foldoutInfo = type.GetField("IsFoldout");
                if (foldoutInfo != null)
                {
                    if (title == null && value is IJsonMainID mainID)
                    {
                        title = mainID.GetMainID().ToString();
                    }
                    
                    isFoldout = EditorGUILayout.Foldout((bool)foldoutInfo.GetValue(value), title);
                    foldoutInfo.SetValue(value, isFoldout);
                }

                if (isFoldout)
                {
                    if (foldoutInfo == null && !string.IsNullOrWhiteSpace(title))
                    {
                        EditorGUILayout.LabelField(title);
                    }

                    EditorGUI.indentLevel++;

                    var properties = type.GetProperties();
                    foreach (var property in properties)
                    {
                        DrawProperty(value, property);
                    }

                    EditorGUI.indentLevel--;
                }
            }
            
            return value;
        }

        private static void DrawProperty(object instance, PropertyInfo property)
        {
            var value = property.GetValue(instance);
            
            if (value == null)
            {
                value = TypeUtils.CreateInstance(property.PropertyType);
            }
            
            var newValue = value;

            var readonlyAttr = property.GetCustomAttribute<EditorDataReadOnlyAttribute>();
            if (readonlyAttr != null)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            if (property.PropertyType == typeof(bool))
            {
                newValue = DrawBool(property.Name, (bool)value, property.PropertyType, property.GetCustomAttributes(true));
            }
            else if (property.PropertyType == typeof(int))
            {
                if (instance is IJsonMainConfig && property.Name == nameof(IJsonMainConfig.ID))
                {
                    newValue = IDBinder.Draw(property.Name, instance, value);
                }
                else
                {
                    newValue = DrawInt(property.Name, (int)value, property.PropertyType, property.GetCustomAttributes(true));
                }
            }
            else if (property.PropertyType == typeof(float))
            {
                newValue = DrawFloat(property.Name, (float)value, property.PropertyType, property.GetCustomAttributes(true));
            }
            else if (property.PropertyType == typeof(string))
            {
                newValue = DrawString(property.Name, (string)value, property.PropertyType, property.GetCustomAttributes(true));
            }
            else if (property.PropertyType.IsEnum)
            {
                newValue = DrawEnum(property.Name, (Enum)value, property.PropertyType, property.GetCustomAttributes(true));
            }
            else if (typeof(IJsonConfig).IsAssignableFrom(property.PropertyType))
            {
                newValue = DrawData(property.Name, value, property.PropertyType);
            }
            else if (TypeUtils.IsListType(property.PropertyType))
            {
                var elementType = TypeUtils.GetElementType(property.PropertyType);
                if (typeof(IJsonConfig).IsAssignableFrom(elementType))
                {
                    EditorGUI.indentLevel++;
                    newValue = DrawDataList(property.Name, (IList)value, elementType);
                    EditorGUI.indentLevel--;
                }
                else if (elementType == typeof(object))
                {
                    newValue = DrawObjectArray(property.Name, (IList)value, instance, property);
                }
            }
            else if (property.PropertyType.IsArray)
            {
                var elementType = TypeUtils.GetElementType(property.PropertyType);
                if (elementType == typeof(object))
                {
                    newValue = DrawObjectArray(property.Name, (object[])value, instance, property);
                }
                else if (typeof(IJsonConfig).IsAssignableFrom(elementType))
                {
                    EditorGUI.indentLevel++;
                    newValue = DrawDataList(property.Name, (object[])value, elementType);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    newValue = LiteEditor.LiteReorderableListWrap.GetWrap((IList)value).Draw(new GUIContent(property.Name));
                }
            }

            if (readonlyAttr != null)
            {
                EditorGUI.EndDisabledGroup();
            }
            
            property.SetValue(instance, newValue);
        }

        private static bool DrawBool(string name, bool value, Type type, object[] attrs)
        {
            return string.IsNullOrEmpty(name) ? EditorGUILayout.Toggle(value) : EditorGUILayout.Toggle(name, value);
        }
        
        private static int DrawInt(string name, int value, Type type, object[] attrs)
        {
            var popupAttr = TypeUtils.GetAttribute<EditorDataPopupAttribute>(type, attrs);
            if (popupAttr != null)
            {
                var popupArray = CardAttributeSource.GetPopupData(popupAttr.Type);
                var index = popupArray.Value.IndexOf(value);
                if (index == -1)
                {
                    index = 0;
                }

                var newIndex = EditorGUILayout.Popup(name, index, popupArray.Display);
                return popupArray.Value[newIndex];
            }
            else
            {
                return string.IsNullOrEmpty(name) ? EditorGUILayout.IntField(value) : EditorGUILayout.IntField(name, value);
            }
        }

        private static float DrawFloat(string name, float value, Type type, object[] attrs)
        {
            return string.IsNullOrEmpty(name) ? EditorGUILayout.FloatField(value) : EditorGUILayout.FloatField(name, value);
        }

        private static string DrawString(string name, string value, Type type, object[] attrs)
        {
            var assetAttr = TypeUtils.GetAttribute<EditorDataAssetAttribute>(type, attrs);
            if (assetAttr != null)
            {
                return DrawAssetField(name, value, assetAttr);
            }
            else
            {
                return string.IsNullOrEmpty(name) ? EditorGUILayout.TextField(value) : EditorGUILayout.TextField(name, value);
            }
        }
        
        private static readonly Dictionary<string, Object> PathToAssetCache_ = new ();
        private static string DrawAssetField(string name, string value, EditorDataAssetAttribute assetAttr)
        {
            Object asset = null;

            if (!string.IsNullOrEmpty(value))
            {
                if (PathToAssetCache_.ContainsKey(value))
                {
                    asset = PathToAssetCache_[value];
                }
                else
                {
                    asset = AssetDatabase.LoadAssetAtPath(PathUtils.GetFullPathInAssetRoot(value), assetAttr.AssetType);
                    if (asset != null)
                    {
                        PathToAssetCache_.Add(value, asset);
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            asset = EditorGUILayout.ObjectField(name, asset, assetAttr.AssetType, true);
            if (EditorGUI.EndChangeCheck())
            {
                var path = PathUtils.GetRelativeAssetRootPath(AssetDatabase.GetAssetPath(asset));
                return path;
            }

            return value;
        }

        private static Enum DrawEnum(string name, Enum value, Type type, object[] attrs)
        {
            var flagAttr = TypeUtils.GetAttribute<EditorDataEnumFlagAttribute>(type, attrs);
            if (flagAttr != null)
            {
                return string.IsNullOrEmpty(name) ? EditorGUILayout.EnumFlagsField(value) : EditorGUILayout.EnumFlagsField(name, value);
            }
            else
            {
                return string.IsNullOrEmpty(name) ? EditorGUILayout.EnumPopup(value) : EditorGUILayout.EnumPopup(name, value);
            }
        }

        private static object DrawObject(string name, object value, Type type, object[] attrs)
        {
            switch (value)
            {
                case bool boolValue:
                    return DrawBool(name, boolValue, type, attrs);
                case int intValue:
                    return DrawInt(name, intValue, type, attrs);
                case float floatValue:
                    return DrawFloat(name, floatValue, type, attrs);
                case string stringValue:
                    return DrawString(name, stringValue, type, attrs);
                case Enum enumValue:
                    return DrawEnum(name, enumValue, type, attrs);
            }

            return value;
        }

        private static object[] DrawObjectArray(string name, object[] arrayValue, object instance, PropertyInfo property)
        {
            var list = DrawObjectArray(name, new List<object>(arrayValue), instance, property);
            return list.ToArray();
        }

        private static IList DrawObjectArray(string name, IList arrayValue, object instance, PropertyInfo property)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var objectArrayAttr = property.GetCustomAttribute<EditorObjectArrayAttribute>();
                if (objectArrayAttr != null)
                {
                    var binderValue = instance.GetType().GetProperty(objectArrayAttr.Binder)?.GetValue(instance);
                    if (binderValue != null)
                    {
                        var arrayResult = CardAttributeSource.GetObjectArray(objectArrayAttr.Type, binderValue);
                        if (arrayResult != null)
                        {
                            return DrawObjectArrayFixed(name, arrayValue, arrayResult);
                        }
                    }
                }

                throw new Exception($"object array need [EditorObjectArray] attribute : {instance.GetType()} - {name}");
            }
        }

        private static IList DrawObjectArrayFixed(string name, IList arrayValue, EditorObjectArrayResult arrayResult)
        {
            if (arrayValue.Count != arrayResult.Value.Length)
            {
                arrayValue.Clear();
                foreach (var type in arrayResult.Value)
                {
                    arrayValue.Add(TypeUtils.CreateInstance(type));
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(name);

                using (new EditorGUILayout.VerticalScope())
                {
                    for (var index = 0; index < arrayValue.Count; ++index)
                    {
                        arrayValue[index] = DrawObject(arrayResult.Display[index], arrayValue[index], arrayValue[index].GetType(), arrayResult.Attrs[index]);
                    }
                }

                return arrayValue;
            }
        }
    }
}