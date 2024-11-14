using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public static class LiteEditorLayoutDrawer
    {
        public static void LabelError(string label)
        {
            using (new ColorScope(Color.red))
            {
                EditorGUILayout.LabelField(label);
            }
        }
        
        /// <summary>
        /// <para>Support Data Type</para>
        /// <para>Primitive : bool, int, long, float, double, string, Enum</para>
        /// <para>UnityEngine : Rect, RectInt, Vector2, Vector2Int, Vector3, Vector3Int, Vector4, Color, GameObject</para>
        /// <para>Other : List, Object, ObjectList, CustomPopupList, OptionalType, OptionalTypeList</para>
        /// </summary>
        public static T DrawObject<T>(string name, T data) where T : class
        {
            return DrawObject(name, data, LitePropertyType.Object) as T;
        }
        
        /// <summary>
        /// <para>Support Data Type</para>
        /// <para>Primitive : bool, int, long, float, double, string, Enum</para>
        /// <para>UnityEngine : Rect, RectInt, Vector2, Vector2Int, Vector3, Vector3Int, Vector4, Color, GameObject</para>
        /// <para>Other : List, Object, ObjectList, CustomPopupList, OptionalType, OptionalTypeList</para>
        /// </summary>
        public static T DrawObject<T>(GUIContent title, T data) where T : class
        {
            return DrawObject(title, data, LitePropertyType.Object) as T;
        }
        
        /// <summary>
        /// <para>Support Data Type</para>
        /// <para>Primitive : bool, int, long, float, double, string, Enum</para>
        /// <para>UnityEngine : Rect, RectInt, Vector2, Vector2Int, Vector3, Vector3Int, Vector4, Color, GameObject</para>
        /// <para>Other : List, Object, ObjectList, CustomPopupList, OptionalType, OptionalTypeList</para>
        /// </summary>
        public static object DrawObject(string name, object data, LitePropertyType propertyType)
        {
            return DrawObject(new GUIContent(name), data, propertyType);
        }
        
        /// <summary>
        /// <para>Support Data Type</para>
        /// <para>Primitive : bool, int, long, float, double, string, Enum</para>
        /// <para>UnityEngine : Rect, RectInt, Vector2, Vector2Int, Vector3, Vector3Int, Vector4, Color, GameObject</para>
        /// <para>Other : List, Object, ObjectList, CustomPopupList, OptionalType, OptionalTypeList</para>
        /// </summary>
        public static object DrawObject(GUIContent title, object data, LitePropertyType propertyType)
        {
            if (data == null)
            {
                return null;
            }
            
            var obj = DrawElement(title, data, data.GetType(), propertyType);
            return obj;
        }
        
        private static object DrawElement(GUIContent title, object data, Type type, LitePropertyType propertyType, object[] attrs = null)
        {
            if (data == null && TypeUtils.CanCreateType(type))
            {
                data = TypeUtils.CreateInstance(type);
            }

            switch (propertyType)
            {
                #region Primitive
                case LitePropertyType.Bool:
                    data = EditorGUILayout.Toggle(title, (bool) data);
                    break;
                case LitePropertyType.Int:
                    data = DrawIntField(title, (int) data, type, attrs);
                    break;
                case LitePropertyType.Long:
                    data = EditorGUILayout.LongField(title, (long) data);
                    break;
                case LitePropertyType.Float:
                    data = DrawFloatField(title, (float) data, type, attrs);
                    break;
                case LitePropertyType.Double:
                    data = DrawDoubleField(title, (double) data, type, attrs);
                    break;
                case LitePropertyType.String:
                    data = DrawStringField(title, (string) data, type, attrs);
                    break;
                case LitePropertyType.Enum:
                    data = DrawEnumField(title, (Enum) data, type, attrs);
                    break;
                #endregion
                #region UnityEngine
                case LitePropertyType.Rect:
                    data = EditorGUILayout.RectField(title, (Rect) data);
                    break;
                case LitePropertyType.RectInt:
                    data = EditorGUILayout.RectIntField(title, (RectInt) data);
                    break;
                case LitePropertyType.Vector2:
                    data = EditorGUILayout.Vector2Field(title, (Vector2) data);
                    break;
                case LitePropertyType.Vector2Int:
                    data = EditorGUILayout.Vector2IntField(title, (Vector2Int) data);
                    break;
                case LitePropertyType.Vector3:
                    data = EditorGUILayout.Vector3Field(title, (Vector3) data);
                    break;
                case LitePropertyType.Vector3Int:
                    data = EditorGUILayout.Vector3IntField(title, (Vector3Int) data);
                    break;
                case LitePropertyType.Vector4:
                    data = EditorGUILayout.Vector4Field(title, (Vector4) data);
                    break;
                case LitePropertyType.Color:
                    data = EditorGUILayout.ColorField(title, (Color) data);
                    break;
                case LitePropertyType.GameObject:
                    data = DrawGameObjectFiled(title, (string) data);
                    break;
                #endregion
                #region Other
                case LitePropertyType.List:
                    data = DrawList(title, (IList) data);
                    break;
                case LitePropertyType.Object:
                    data = DrawObjectInternal(title, data, type);
                    break;
                case LitePropertyType.ObjectList:
                    data = DrawObjectList(title, data, attrs);
                    break;
                case LitePropertyType.CustomPopupList:
                    data = DrawCustomPopupStringList(title, (string) data, attrs);
                    break;
                case LitePropertyType.OptionalType:
                    data = DrawOptionalTypeData(title, data, attrs);
                    break;
                case LitePropertyType.OptionalTypeList:
                    data = DrawOptionalTypeList(title, data, attrs);
                    break;
                #endregion
                default:
                    LiteEditorUtils.UnsupportedType("PropertyType", type);
                    break;
            }
            return data;
        }
        
        private static object DrawIntField(GUIContent title, int v, Type type, object[] attrs)
        {
            var rangeIntAttr = TypeUtils.GetAttribute<LiteIntRangeAttribute>(type, attrs);
            if (rangeIntAttr != null)
            {
                return EditorGUILayout.IntSlider(title, v, rangeIntAttr.Min, rangeIntAttr.Max);
            }
            
            var delayedIntAttr = TypeUtils.GetAttribute<LiteDelayedAttribute>(type, attrs);
            return delayedIntAttr == null ? EditorGUILayout.IntField(title, v) : EditorGUILayout.DelayedIntField(title, v);
        }
        
        private static float DrawFloatField(GUIContent title, float v, Type type, object[] attrs)
        {
            var rangeIntAttr = TypeUtils.GetAttribute<LiteFloatRangeAttribute>(type, attrs);
            if (rangeIntAttr != null)
            {
                return EditorGUILayout.Slider(title, v, rangeIntAttr.Min, rangeIntAttr.Max);
            }
            
            var delayedFloatAttr = TypeUtils.GetAttribute<LiteDelayedAttribute>(type, attrs);
            return delayedFloatAttr == null ? EditorGUILayout.FloatField(title, v) : EditorGUILayout.DelayedFloatField(title, v);
        }
        
        private static double DrawDoubleField(GUIContent title, double v, Type type, object[] attrs)
        {
            var delayedDoubleAttr = TypeUtils.GetAttribute<LiteDelayedAttribute>(type, attrs);
            return delayedDoubleAttr == null ? EditorGUILayout.DoubleField(title, v) : EditorGUILayout.DelayedDoubleField(title, v);
        }

        private static string DrawStringField(GUIContent title, string v, Type type, object[] attrs)
        {
            var delayedStringAttr = TypeUtils.GetAttribute<LiteDelayedAttribute>(type, attrs);
            return delayedStringAttr == null ? EditorGUILayout.TextField(title, v) : EditorGUILayout.DelayedTextField(title, v);
        }

        private static Enum DrawEnumField(GUIContent title, Enum v, Type type, object[] attrs)
        {
            var enumFlagsAttr = TypeUtils.GetAttribute<LiteEnumFlagsAttribute>(type, attrs);
            return enumFlagsAttr == null ? EditorGUILayout.EnumPopup(title, v) : EditorGUILayout.EnumFlagsField(title, v);
        }

        private static readonly Dictionary<string, GameObject> Path2GoCache_ = new Dictionary<string, GameObject>();
        private static string DrawGameObjectFiled(GUIContent title, string v)
        {
            GameObject go = null;

            if (!string.IsNullOrEmpty(v))
            {
                if (Path2GoCache_.ContainsKey(v))
                {
                    go = Path2GoCache_[v];
                }
                else
                {
                    var goPath = PathUtils.GetFullPathInAssetRoot(v);
                    go = AssetDatabase.LoadAssetAtPath<GameObject>(goPath);
                    if (go != null)
                    {
                        Path2GoCache_.Add(v, go);
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            go = EditorGUILayout.ObjectField(title, go, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                var path = PathUtils.GetRelativeAssetRootPath(AssetDatabase.GetAssetPath(go));
                return path;
            }

            return v;
        }
        
        private static object DrawList(GUIContent title, IList data)
        {
            LiteReorderableListWrap.GetWrap(data)?.Draw(title);
            return data;
        }
        
        private static readonly Dictionary<object, bool> Foldout_ = new Dictionary<object, bool>();
        private static object DrawObjectInternal(GUIContent title, object data, Type type)
        {
            if (data == null)
            {
                data = TypeUtils.CreateInstance(type);
            }
            
            var depth = 0;
            if (title != GUIContent.none)
            {
                if (!Foldout_.ContainsKey(data))
                {
                    Foldout_.Add(data, true);
                }
                
                Foldout_[data] = EditorGUILayout.Foldout(Foldout_[data], title);
                // EditorGUILayout.LabelField(title);
                depth = 1;
                
                if (!Foldout_[data])
                {
                    return data;
                }
            }
            
            using (new GUILayout.VerticalScope())
            {
                EditorGUI.indentLevel += depth;
                if (!type.IsPrimitive && (type.IsClass || type.IsValueType))
                {
                    var enableSourceData = LiteEnableSourceAttribute.GetSourceData(data);
                    
                    // var fields = type.GetFields().OrderBy(p => p.MetadataToken).ToArray();
                    var fields = TypeUtils.PrioritySort(type.GetFields());
                    foreach (var field in fields)
                    {
                        if (LiteEnableCheckerAttribute.Check(field, enableSourceData))
                        {
                            DrawField(data, field);
                        }
                    }

                    // var properties = type.GetProperties().OrderBy(p => p.MetadataToken).ToArray();
                    var properties = TypeUtils.PrioritySort(type.GetProperties());
                    foreach (var property in properties)
                    {
                        if (LiteEnableCheckerAttribute.Check(property, enableSourceData))
                        {
                            DrawProperty(data, property);
                        }
                    }
                }
                // else if (type.IsPrimitive)
                // {
                //     data = DrawElement(title, data, type);
                // }
                else
                {
                    LiteEditorUtils.UnsupportedType(string.Empty, type);
                }
                EditorGUI.indentLevel -= depth;
            }

            return data;
        }

        private static void DrawField(object target, FieldInfo info)
        {
            var litePropertyAttrs = info.GetCustomAttributes(typeof(LitePropertyAttribute), false);
            if (litePropertyAttrs.Length == 1 && litePropertyAttrs[0] is LitePropertyAttribute propertyAttr)
            {
                // var title = LiteEditorUtils.GetTitleFromFieldInfo(info);
                var title = new GUIContent(propertyAttr.Name);
                var fieldValue = info.GetValue(target);
                var attrs = info.GetCustomAttributes(true);
                EditorGUI.BeginChangeCheck();
                fieldValue = DrawElement(title, fieldValue, info.FieldType, propertyAttr.Type, attrs);
                if (EditorGUI.EndChangeCheck())
                {
                    info.SetValue(target, fieldValue);
                }
            }
        }

        private static void DrawProperty(object target, PropertyInfo info)
        {
            var litePropertyAttrs = info.GetCustomAttributes(typeof(LitePropertyAttribute), false);
            if (litePropertyAttrs.Length == 1 && litePropertyAttrs[0] is LitePropertyAttribute propertyAttr)
            {
                var title = new GUIContent(propertyAttr.Name);
                // var title = LiteEditorUtils.GetTitleFromMemberInfo(info);
                var propertyValue = info.GetValue(target);
                var attrs = info.GetCustomAttributes(true);
                
                EditorGUI.BeginDisabledGroup(!info.CanWrite);
                EditorGUI.BeginChangeCheck();
                propertyValue = DrawElement(title, propertyValue, info.PropertyType, propertyAttr.Type, attrs);
                if (EditorGUI.EndChangeCheck())
                {
                    info.SetValue(target, propertyValue);
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private static object DrawObjectList(GUIContent title, object v, object[] attrs)
        {
            return DrawCustomList(title, v, TypeUtils.GetElementType(v.GetType()), null,
                (list, i) => DrawObject(new GUIContent($"Item {i}"), list[i], LitePropertyType.Object));
        }

        private static string DrawCustomPopupStringList(GUIContent title, string v, object[] attrs)
        {
            var customAttr = TypeUtils.GetAttribute<LiteCustomPopupListAttribute>(null, attrs);
            var list = customAttr?.GetListFunc?.Invoke() ?? new List<string>{"error custom list"};
            return DrawPopupStringList(title, list, v);
        }
        
        private static string DrawPopupStringList(GUIContent title, List<string> list, string v)
        {
            var selectIndex = list.IndexOf(v);
            
            EditorGUI.BeginChangeCheck();
            selectIndex = EditorGUILayout.Popup(title, selectIndex, list.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                var newValue = selectIndex >= 0 && selectIndex < list.Count ? list[selectIndex] : (list.Count > 0 ? list[0] : string.Empty);
                return newValue;
            }

            return v;
        }
        
        private static object DrawOptionalTypeSelector(GUIContent title, Type baseType, object v)
        {
            var typeList = LiteEditorUtils.GetTypeListWithBaseType(baseType);
            var nameList = TypeUtils.TypeListToString(typeList);
            var currentType = v?.GetType();
            var selectIndex = nameList.IndexOf(TypeUtils.GetTypeDisplayName(currentType));
            
            EditorGUI.BeginChangeCheck();
            selectIndex = EditorGUILayout.Popup(title, selectIndex, nameList.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                var newType = selectIndex >= 0 && selectIndex < typeList.Count ? typeList[selectIndex] : currentType;
                if (newType != currentType)
                {
                    v = TypeUtils.CreateInstance(newType);
                }
            }

            return v;
        }

        private static object DrawOptionalTypeData(GUIContent title, object v, object[] attrs)
        {
            var optionalTypeAttr = TypeUtils.GetAttribute<LiteOptionalTypeAttribute>(null, attrs);
            if (optionalTypeAttr == null)
            {
                return v;
            }

            v = DrawOptionalTypeSelector(title, optionalTypeAttr.BaseType, v);
            return DrawIHasData(optionalTypeAttr.DataTitle, v, optionalTypeAttr.BaseType);
        }

        private static object DrawOptionalTypeList(GUIContent title, object v, object[] attrs)
        {
            var optionalTypeListAttr = TypeUtils.GetAttribute<LiteOptionalTypeListAttribute>(null, attrs);
            if (optionalTypeListAttr == null)
            {
                return v;
            }

            return DrawCustomList(title, v, optionalTypeListAttr.DefaultType,
                (list, i) => DrawOptionalTypeSelector(new GUIContent($"{optionalTypeListAttr.ElementTitle} {i}"), optionalTypeListAttr.BaseType, list[i]),
                (list, i) => DrawIHasData(optionalTypeListAttr.DataTitle, list[i], optionalTypeListAttr.BaseType));
        }

        private static object DrawCustomList(GUIContent title, object v, Type elementType, Func<IList, int, object> drawHeader, Func<IList, int, object> drawObject)
        {
            var list = v as IList;
            if (list == null)
            {
                return v;
            }

            using (new EditorGUILayout.VerticalScope(LiteEditorStyle.FrameBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.HorizontalScope(LiteEditorStyle.FrameBox))
                    {
                        // GUILayout.Label(title);
                        if (!Foldout_.ContainsKey(v))
                        {
                            Foldout_.Add(v, true);
                        }
                
                        Foldout_[v] = EditorGUILayout.Foldout(Foldout_[v], title);
                        if (!Foldout_[v])
                        {
                            return v;
                        }
                        
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{list.Count} items");
                    }

                    using (new EditorGUILayout.HorizontalScope(LiteEditorStyle.FrameBox))
                    {
                        if (GUILayout.Button("+"))
                        {
                            list = ArrayUtils.AddToList(list, TypeUtils.CreateInstance(elementType));
                        }
                    }
                }

                for (var i = 0; i < list.Count; ++i)
                {
                    using (new EditorGUILayout.VerticalScope(LiteEditorStyle.FrameBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(EditorGUI.indentLevel * 15);
                            if (GUILayout.Button("↑", GUILayout.ExpandWidth(false)) && i > 0)
                            {
                                (list[i - 1], list[i]) = (list[i], list[i - 1]);
                            }

                            if (GUILayout.Button("↓", GUILayout.ExpandWidth(false)) && i < list.Count - 1)
                            {
                                (list[i], list[i + 1]) = (list[i + 1], list[i]);
                            }

                            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                            {
                                if (LiteEditorUtils.ShowConfirmDialog($"Item {i} ?"))
                                {
                                    list = ArrayUtils.RemoveFromList(list, i);
                                    continue;
                                }
                            }

                            if (drawHeader != null)
                            {
                                list[i] = drawHeader.Invoke(list, i);
                            }
                        }

                        if (drawObject != null)
                        {
                            list[i] = drawObject.Invoke(list, i);
                        }
                    }
                }
            }

            return list;
        }

        private static object DrawIHasData(string title, object data, Type type)
        {
            // if (typeof(IHasData).IsAssignableFrom(type))
            if (type.IsAssignableTo<IHasData>())
            {
                if (data is IHasData { HasData: true })
                {
                    return DrawObject(new GUIContent(title), data, LitePropertyType.Object);
                }

                return data;
            }

            LabelError($"{type} not implement IHasData");
            return data;
        }

        [InitializeOnLoadMethod]
        private static void Clear()
        {
            Foldout_.Clear();
            Path2GoCache_.Clear();
        }
    }
}