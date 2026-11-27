# EventSystem 事件

通过 `LiteRuntime.Event` 发布和订阅实现 `IEventData` 的类型。事件同步分发到当前模块，支持全局模块和独立模块。

## 按拥有者注册与注销

```csharp
using System;
using LiteQuark.Runtime;

public struct CoinChangedEvent : IEventData
{
    public int Count;
}

public sealed class CoinListener : IDisposable
{
    private readonly EventSubscriptionScope _scope;

    public CoinListener() // 在框架初始化后创建
    {
        _scope = LiteRuntime.Event.CreateSubscriptionScope();
        _scope.Register<CoinChangedEvent>(OnCoinChanged);
    }

    private void OnCoinChanged(CoinChangedEvent evt)
    {
        LLog.Info("Coins: {0}", evt.Count);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
```

```csharp
LiteRuntime.Event.Send(new CoinChangedEvent { Count = 100 });
```

`Send<T>()` 可发送带无参构造的事件类型。按委托注销 Lambda 时，使用注册时保存的委托。

## 作用域

| API | 用途 |
| --- | --- |
| `CreateSubscriptionScope()` | 为当前模块的一组订阅创建作用域 |
| `scope.Register<T>(callback)` / `scope.UnRegister<T>(callback)` | 注册或移除作用域内的单条监听 |
| `scope.Dispose()` | 移除该作用域的全部监听，重复释放安全 |
| `Register<T>(callback)` / `UnRegister<T>(callback)` | 模块默认分组下的监听，由注册方主动注销 |
| `CreateIndependentModule(name)` | 创建与全局事件隔离的 `EventModule` |

同一分组、同一事件类型下的重复委托只注册一次。同一个委托可以分别注册在多个作用域或默认分组。释放作用域只影响该作用域的订阅，分发仍通过原事件模块进行。

独立模块提供相同的 `Send`、`Register`、`UnRegister` 和 `CreateSubscriptionScope()`。创建者负责释放作用域和独立模块。模块名用于标识，各模块分别维护自己的监听。

## 分发语义

- 分发使用监听器快照。回调中的注册、注销和作用域释放会影响后续发送，本次发送继续执行已取得的快照。跨分组调用顺序未定义。
- 单个回调异常通过 `LiteUtils.SafeInvoke` 记录，不中断后续监听器。
- 监听不会因 GameObject 销毁自动注销。编辑器下模块释放会报告残留监听，然后清空。
- `EnterForegroundEvent`、`EnterBackgroundEvent` 是内置前后台事件。框架重启会重建全局事件模块。
- 从 Unity 主线程调用；当前集合实现没有提供跨线程同步。

实现见 [EventSystem.cs](EventSystem.cs)、[EventModule.cs](EventModule.cs) 和 [EventSubscriptionScope.cs](EventSubscriptionScope.cs)。
