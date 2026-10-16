# EventSystem 事件

通过 `LiteRuntime.Event` 发布和订阅实现 `IEventData` 的类型。事件同步分发到当前模块，支持全局模块和独立模块。

## 注册、发送与注销

```csharp
using System;
using LiteQuark.Runtime;

public struct CoinChangedEvent : IEventData
{
    public int Count;
}

public sealed class CoinListener : IDisposable
{
    public CoinListener() // 在框架初始化后创建
    {
        LiteRuntime.Event.Register<CoinChangedEvent>(OnCoinChanged);
    }

    private void OnCoinChanged(CoinChangedEvent evt)
    {
        LLog.Info("Coins: {0}", evt.Count);
    }

    public void Dispose()
    {
        LiteRuntime.Event.UnRegister<CoinChangedEvent>(OnCoinChanged);
    }
}
```

```csharp
LiteRuntime.Event.Send(new CoinChangedEvent { Count = 100 });
```

`Send<T>()` 可发送带无参构造的事件类型。使用 Lambda 注册时，保存原委托用于注销；重新创建相同内容的 Lambda 不是原监听器。

## 作用域

| API | 用途 |
| --- | --- |
| `Register<T>(callback)` / `UnRegister<T>(callback)` | 模块默认 Tag 下的监听 |
| `Register<T>(tag, callback)` / `UnRegister<T>(tag, callback)` | 指定整数 Tag 的监听 |
| `UnRegisterAll(tag)` | 移除该 Tag 下所有事件类型的监听 |
| `CreateIndependentModule(name)` | 创建与全局事件隔离的 `EventModule` |

Tag 由调用者分配，应在模块内区分不同拥有者；注销时使用与注册相同的 Tag。同一 Tag/事件类型下的重复委托不会重复注册。

独立模块提供相同的 Send/Register/UnRegister API。创建者负责 `Dispose()`，EventSystem 不会跟踪并代为释放独立模块。模块名用于标识，不会让两个模块共享监听。

## 分发语义

- 分发使用监听器快照；回调中注册或注销不会改变本次已取得的快照，不应依赖跨 Tag 的调用顺序。
- 单个回调异常通过 `LiteUtils.SafeInvoke` 记录，不中断后续监听器。
- 监听不会因 GameObject 销毁自动注销。编辑器下模块释放会报告残留监听，然后清空。
- `EnterForegroundEvent`、`EnterBackgroundEvent` 是内置前后台事件。框架重启会重建全局事件模块。
- 从 Unity 主线程调用；当前集合实现没有提供跨线程同步。

实现见 [EventSystem.cs](EventSystem.cs) 和 [EventModule.cs](EventModule.cs)。
