using System.Collections.Generic;

namespace LiteQuark.Editor
{
    
    public class CustomBuildConfig
    {
        /// <summary>
        /// Enable custom build step
        /// </summary>
        public bool Enable { get; set; } = true;

        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}