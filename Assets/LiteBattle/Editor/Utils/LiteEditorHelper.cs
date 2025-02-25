using System;
using System.Collections.Generic;
using System.Reflection;
using LiteBattle.Runtime;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;
using LiteConst = LiteBattle.Runtime.LiteConst;
using LiteLabelAttribute = LiteBattle.Runtime.LiteLabelAttribute;
using LitePriorityAttribute = LiteBattle.Runtime.LitePriorityAttribute;

namespace LiteBattle.Editor
{
    public static class LiteEditorHelper
    {
        public static void UnsupportedType(Type type)
        {
            LiteLog.Error($"Unsupported type: {type}");
        }

        public static bool ShowConfirmDialog(string msg)
        {
            return EditorUtility.DisplayDialog("Lite State", msg, "Confirm", "Cancel");
        }

        public static int GetStringIndex(List<string> list, string value)
        {
            return list.IndexOf(value);
        }
        
        public static T GetAttribute<T>(Type type, object[] attrs) where T : Attribute
        {
            T result = null;
            if (attrs != null)
            {
                result = (T)Array.Find(attrs, t => t is T);
            }

            if (result == null)
            {
                result = type?.GetCustomAttribute<T>();
            }
            return result;
        }
        
        public static GUIContent GetTitleFromFieldInfo(FieldInfo info)
        {
            var labelAttr = info.GetCustomAttribute<LiteLabelAttribute>();
            var title = labelAttr != null ? new GUIContent(labelAttr.Label) : new GUIContent(info.Name);
            return title;
        }
        
        public static GUIContent GetTitleFromPropertyInfo(PropertyInfo info)
        {
            var labelAttr = info.GetCustomAttribute<LiteLabelAttribute>();
            var title = labelAttr != null ? new GUIContent(labelAttr.Label) : new GUIContent(info.Name);
            return title;
        }

        public static uint GetPriorityFromType(Type type)
        {
            var priorityAttr = type.GetCustomAttribute<LitePriorityAttribute>();
            var value = priorityAttr != null ? priorityAttr.Value : 0;
            return value;
        }

        public static List<string> GetLiteStateNameList()
        {
            var stateNameList = new List<string>();

            foreach (var timelinePath in LiteStateEditor.Instance.GetCurrentAgentTimelinePathList())
            {
                stateNameList.Add(PathUtils.GetFileNameWithoutExt(timelinePath));
            }

            return stateNameList;
        }

        public static List<string> GetAnimationNameList()
        {
            if (LiteEditorBinder.Instance.IsBindAgent())
            {
                return LiteEditorBinder.Instance.GetAnimatorStateNameList();
            }

            return new List<string>();
        }

        public static List<string> GetInputKeyList()
        {
            return LiteConst.KeyName.GetKeyList();
        }

        public static List<Assembly> GetAssemblies(Type targetType)
        {
#if LITE_STATE_FULL_ASSEMBLY
            return AppDomain.CurrentDomain.GetAssemblies()
#else
            return new List<Assembly> { targetType.Assembly };
#endif
        }

        public static void PrioritySort(List<Type> sortList)
        {
            sortList.Sort((a, b) =>
            {
                var priorityA = GetPriorityFromType(a);
                var priorityB = GetPriorityFromType(b);

                if (priorityA == priorityB)
                {
                    return 0;
                }

                if (priorityA > priorityB)
                {
                    return -1;
                }

                return 1;
            });
        }

        public static List<Type> GetTypeListWithBaseType(Type baseType)
        {
            var results = new List<Type>();

            // foreach (var assembly in GetAssemblies(baseType))
            // {
            //     foreach (var type in assembly.GetTypes())
            //     {
            //         if (baseType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && !results.Contains(type))
            //         {
            //             results.Add(type);
            //         }
            //     }
            // }
            results.AddRange(TypeCache.GetTypesDerivedFrom(baseType));
            
            PrioritySort(results);

            return results;
        }

        public static string GetTypeDisplayName(Type type)
        {
            var labelAttr = LiteEditorHelper.GetAttribute<LiteLabelAttribute>(type, null);
            return labelAttr != null ? labelAttr.Label : type.Name;
        }

        public static List<string> TypeListToString(List<Type> typeList)
        {
            var results = new List<string>();

            foreach (var type in typeList)
            {
                results.Add(GetTypeDisplayName(type));
            }

            return results;
        }

        public static void ShowStandaloneGameView(string tag)
        {
            tag = string.IsNullOrWhiteSpace(tag) ? "Preview" : tag;
            
            var inst = EditorWindow.CreateInstance(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")) as EditorWindow;
            inst.position = new Rect((Screen.width - 960) / 2, 150, 960, 640);
            inst.titleContent = new GUIContent(tag);
            inst.Show();
            inst.name = tag;
        }

        public static void HideStandaloneGameView(string tag)
        {
            tag = string.IsNullOrWhiteSpace(tag) ? "Preview" : tag;
            
            var gameViews = Resources.FindObjectsOfTypeAll(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            foreach (var game in gameViews)
            {
                if (game.name == tag && game is EditorWindow previewWin)
                {
                    previewWin.Close();
                    break;
                }
            }
        }
    }
}