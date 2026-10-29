namespace LiteQuark.Runtime
{
    internal sealed partial class SceneInfoCache : IDisposeAsync
    {
        internal SceneVisitorInfo GetVisitorInfo()
        {
            var info = new SceneVisitorInfo(_sceneKey);
            return info;
        }
    }
}
