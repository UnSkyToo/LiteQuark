using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    internal static class UIBinder
    {
        private const BindingFlags CustomFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type TransformType = typeof(Transform);
        private static readonly Type ComponentType = typeof(Component);

        internal static void AutoBind(BaseUI ui)
        {
            if (ui == null)
            {
                return;
            }

            BindNode(ui);
            BindComponent(ui);
            BindEvent(ui);
        }

        internal static void AutoUnbind(BaseUI ui)
        {
            UnbindEvent(ui);
        }

        private static void BindNode(BaseUI ui)
        {
            var fieldList = ui.GetType().GetFields(CustomFlags);
            foreach (var field in fieldList)
            {
                if (field.FieldType != TransformType)
                {
                    continue;
                }

                var attr = field.GetCustomAttribute<UINodeAttribute>(false);
                if (attr != null)
                {
                    var nodeValue = ui.FindChild(attr.Path);
                    if (nodeValue == null)
                    {
                        LLog.Warning($"can't bind node : {attr.Path}");
                        continue;
                    }

                    field.SetValue(ui, nodeValue);
                }
            }
        }

        private static void BindComponent(BaseUI ui)
        {
            var fieldList = ui.GetType().GetFields(CustomFlags);
            foreach (var field in fieldList)
            {
                if (!field.FieldType.IsSubclassOf(ComponentType))
                {
                    continue;
                }

                var attr = field.GetCustomAttribute<UIComponentAttribute>(false);
                if (attr != null)
                {
                    var componentValue = ui.FindComponent(attr.Path, field.FieldType);
                    if (componentValue == null)
                    {
                        LLog.Warning($"can't bind component : {attr.Path}");
                        continue;
                    }

                    field.SetValue(ui, componentValue);
                }
            }
        }

        private static void BindEvent(BaseUI ui)
        {
            var methodList = ui.GetType().GetMethods(CustomFlags);
            foreach (var method in methodList)
            {
                var attr = method.GetCustomAttribute<UIClickEventAttribute>(false);
                if (attr != null)
                {
                    try
                    {
                        ui.FindComponent<Button>(attr.Path).onClick.AddListener(Delegate.CreateDelegate(typeof(UnityAction), ui, method) as UnityAction);
                    }
                    catch (Exception ex)
                    {
                        LLog.Warning($"can't bind component : {attr.Path}\n{ex.Message}");
                    }
                }
            }
        }

        private static void UnbindEvent(BaseUI ui)
        {
            var methodList = ui.GetType().GetMethods(CustomFlags);
            foreach (var method in methodList)
            {
                var attr = method.GetCustomAttribute<UIClickEventAttribute>(false);
                if (attr != null)
                {
                    try
                    {
                        ui.FindComponent<Button>(attr.Path).onClick.RemoveAllListeners();
                    }
                    catch (Exception ex)
                    {
                        LLog.Warning($"can't unbind component : {attr.Path}\n{ex.Message}");
                    }
                }
            }
        }
    }
    
    public abstract class UIBaseAttribute : Attribute
    {
        public string Path { get; }
        
        protected UIBaseAttribute(string path)
        {
            Path = path;
        }
    }

    public class UINodeAttribute : UIBaseAttribute
    {
        public UINodeAttribute(string path)
            : base(path)
        {
        }
    }

    public class UIComponentAttribute : UIBaseAttribute
    {
        public UIComponentAttribute(string path)
            : base(path)
        {
        }
    }

    public class UIClickEventAttribute : UIBaseAttribute
    {
        public UIClickEventAttribute(string path)
            : base(path)
        {
        }
    }
}