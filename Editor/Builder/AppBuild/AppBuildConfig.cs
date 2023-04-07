using UnityEditor;

namespace LiteQuark.Editor
{
    public sealed class AppBuildConfig
    {
        /// <summary>
        /// Enable App build step
        /// </summary>
        public bool Enable { get; set; } = true;
        
        /// <summary>
        /// Build app options
        /// </summary>
        public BuildOptions Options { get; set; } = BuildOptions.None;

        public string ProduceName { get; set; } = "game";

        public string Version { get; set; } = "1.0.0";

        public int BuildCode { get; set; } = 1;

        /// <summary>
        /// Script backend
        /// </summary>
        public ScriptingImplementation Backend { get; set; } = ScriptingImplementation.IL2CPP;

        /// <summary>
        /// Is development build Mode
        /// </summary>
        public bool IsDevelopmentBuild { get; set; } = true;
    }
}