namespace LiteQuark.Runtime
{
    internal sealed partial class SceneInfoCache : IDispose
    {
        internal SceneVisitorInfo GetVisitorInfo()
        {
            var info = new SceneVisitorInfo(_sceneName);
            return info;
        }
    }
}
