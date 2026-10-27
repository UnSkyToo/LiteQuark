namespace LiteQuark.Runtime
{
    public interface ISystem : IInitializeAsync, IDisposeAsync
    {
    }

    public interface ISystemSetting
    {
    }

    public interface ISystemSettingProvider<TSetting> where TSetting : class, ISystemSetting, new()
    {
        TSetting Setting { get; set; }
    }
}
