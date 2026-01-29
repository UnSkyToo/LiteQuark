using UnityEditor;
using UnityEngine;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    internal static class ProfilerStyles
    {
        public static readonly Color SessionColor = new Color(0.9f, 0.9f, 0.5f, 1f);  // 黄色 - Session
        public static readonly Color BundleColor = new Color(0.4f, 0.6f, 0.9f, 1f);   // 蓝色 - Bundle
        public static readonly Color AssetColor = new Color(0.5f, 0.8f, 0.5f, 1f);    // 绿色 - Asset
        public static readonly Color SceneColor = new Color(0.9f, 0.7f, 0.4f, 1f);    // 橙色 - Scene
        public static readonly Color ErrorColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        public static readonly Color SuccessColor = new Color(0.3f, 0.9f, 0.3f, 1f);
        public static readonly Color LocalColor = new Color(0.5f, 0.7f, 0.5f, 1f);
        public static readonly Color RemoteColor = new Color(0.4f, 0.6f, 0.9f, 1f);
        public static readonly Color CachedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        public static readonly Color TimelineBackground = new Color(0.15f, 0.15f, 0.15f, 1f);
        public static readonly Color TimelineGridLine = new Color(0.3f, 0.3f, 0.3f, 1f);

        private static GUIStyle _notPlayingStyle;
        public static GUIStyle NotPlayingStyle
        {
            get
            {
                if (_notPlayingStyle == null)
                {
                    _notPlayingStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 18,
                        normal = { textColor = Color.gray }
                    };
                }
                return _notPlayingStyle;
            }
        }

        public static Color GetLoadTypeColor(AssetLoadEventType type)
        {
            return type switch
            {
                AssetLoadEventType.Session => SessionColor,
                AssetLoadEventType.Bundle => BundleColor,
                AssetLoadEventType.Asset => AssetColor,
                AssetLoadEventType.Scene => SceneColor,
                _ => Color.white
            };
        }

        public static Color GetLoadSourceColor(AssetLoadEventSource source)
        {
            return source switch
            {
                AssetLoadEventSource.Local => LocalColor,
                AssetLoadEventSource.Remote => RemoteColor,
                AssetLoadEventSource.Cached => CachedColor,
                _ => Color.white
            };
        }
    }
}
