namespace LiteQuark.Runtime
{
    public sealed class UnityConsoleLogAppender : LogAppenderBase
    {
        public override bool RequireLayout => true;

        public UnityConsoleLogAppender()
        {
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var msg = RenderLoggingEvent(loggingEvent);

            switch (loggingEvent.Level)
            {
                case LogLevel.Info:
                    UnityEngine.Debug.Log(msg);
                    break;
                case LogLevel.Warn:
                    UnityEngine.Debug.LogWarning(msg);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(msg);
                    break;
                case LogLevel.Fatal:
                    UnityEngine.Debug.LogError(msg);
                    break;
                default:
                    break;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.OnOpenAsset(-1)]
        private static bool OnOpenAsset(int instance, int line)
        {
            var stackTrace = GetStackTrace();
            if (!string.IsNullOrEmpty(stackTrace) && stackTrace.Contains(nameof(UnityConsoleLogAppender)))
            {
                var matches = System.Text.RegularExpressions.Regex.Match(stackTrace, @"\(at (.+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                while (matches.Success)
                {
                    var pathLine = matches.Groups[1].Value;
                    if (!pathLine.Contains("/Log/") && !string.IsNullOrEmpty(pathLine))
                    {
                        var splitIndex = pathLine.LastIndexOf(":", System.StringComparison.Ordinal);
                        var path = pathLine.Substring(0, splitIndex);
                        line = System.Convert.ToInt32(pathLine.Substring(splitIndex + 1));
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, line);
                        // UnityEditor.AssetDatabase.OpenAsset(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path), line);
                        return true;
                    }

                    matches = matches.NextMatch();
                }

                return true;
            }

            return false;
        }

        private static string GetStackTrace()
        {
            var consoleWindowType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            var filedInfo = consoleWindowType.GetField("ms_ConsoleWindow", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var instance = filedInfo?.GetValue(null);
            if (instance != null)
            {
                if ((object)UnityEditor.EditorWindow.focusedWindow == instance)
                {
                    filedInfo = consoleWindowType.GetField("m_ActiveText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var activeText = filedInfo?.GetValue(instance)?.ToString();
                    return activeText;
                }
            }

            return null;
        }
#endif
    }
}