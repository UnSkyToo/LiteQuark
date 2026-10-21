namespace LiteQuark.Runtime
{
    /// <summary>
    /// 资源请求所需 Bundle 的排队优先级。<br/>
    /// 不抢占已经启动的加载，也不绕过并发限制。
    /// </summary>
    public enum AssetLoadPriority
    {
        Low = -1,
        Normal = 0,
        High = 1,
    }
}
