# LiteQuark Framework

LiteQuark 将系统服务（`ISystem`）与业务逻辑（`ILogic`）分开管理，通过 `LiteRuntime` 访问已初始化的服务。本文描述当前源码；发布历史见 [CHANGELOG](CHANGELOG.md)。

## 接入与启动

1. 在引导场景的 GameObject 上挂载 `LiteLauncher`。
2. 在 Launcher 的 `LogicList` 中添加业务类，在 `SystemList` 中添加需要的扩展系统。分别配置 DebugSetting 和 ReleaseSetting。
3. 业务类实现 `Initialize()`、`Tick()`、`Dispose()`；系统初始化完成后才开始初始化业务。

```csharp
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;

public sealed class GameLogic : ILogic
{
    public UniTask<bool> Initialize()
    {
        LLog.Info("Game logic initialized");
        return UniTask.FromResult(true);
    }

    public void Tick(float deltaTime)
    {
    }

    public void Dispose()
    {
    }
}
```

开发构建和编辑器默认使用 DebugSetting，普通构建使用 ReleaseSetting；编辑器可通过 `_simulateReleaseBuild` 模拟 Release 设置。仅创建调用 `DontDestroyOnLoad` 的组件不会启动框架。

## 生命周期与注册

`Idle → InitSystem → InitLogic → Running`，初始化失败进入 `Error`；关闭后回到 `Idle`。`LiteLauncher` 驱动每帧更新、前后台通知和退出清理。启动过程异步执行，业务对服务的访问应放在 `ILogic.Initialize()` 或之后。

内置系统按 [LiteConst.InternalSystem](Runtime/Base/LiteConst.cs) 的优先级从大到小初始化：

`Log → Event → Timer → Task → Asset → ObjectPool → Action`

扩展系统按配置列表顺序接在内置系统之后；业务按 `LogicList` 顺序初始化。系统关闭时按相反顺序释放。当前没有 `BaseSystem` 或 Stage/Pipeline 注册机制。

| 扩展点 | 约定 |
| --- | --- |
| `ISystem` | `UniTask<bool> Initialize()`、`Dispose()`；逐帧更新时同时实现 `ITick` |
| `ILogic` | `UniTask<bool> Initialize()`、`Tick(float)`、`Dispose()` |
| `ISystemSettingProvider<TSetting>` | 实现可写 `Setting` 属性，初始化前由框架注入配置 |
| 配置类 | 实现 `ISystemSetting`，使用可序列化类型和无参构造函数 |

扩展系统加入 `LiteSetting.SystemList`，对应配置放入 `SystemSettings`。缺少配置时框架创建默认实例并记录警告。更改启动配置应在初始化前完成；修改列表不会动态注册系统。

## 服务与模块

```csharp
var events = LiteRuntime.Event;
var assets = LiteRuntime.Asset;
var timers = LiteRuntime.Timer;
// 泛型入口适用于所有已注册系统；未注册时返回 null。
var tasks = LiteRuntime.Get<TaskSystem>();
```

| 核心模块 | 说明 |
| --- | --- |
| [Asset](Runtime/Asset/README.md) | Editor / AssetBundle 加载、Handle 所有权、缓存与预加载 |
| [Event](Runtime/Core/Event/README.md) | 类型化事件、Tag 注销、独立事件模块 |
| [Task](Runtime/Core/Async/Task/README.md) | 协程、回调任务、并发限制、主线程投递 |
| [Timer](Runtime/Core/Async/Timer/README.md) | 时间回调、重复执行、暂停与取消 |
| [Action](Runtime/Core/Async/Action/README.md) | Transform 补间、序列/并行、条件等待 |
| [ObjectPool](Runtime/Core/ObjectPool/README.md) | GameObject、集合复用及预生成 |
| Log | `LLog` 与 `LiteRuntime.Log`，由 `LiteRuntime.Setting.Log` 配置 |

UI、Audio、Effect、Network、Data、Config 属于可选的 `LiteQuark.Addons` 包，不会自动注册。安装并注册后使用 `LiteRuntime.Get<UISystem>()` 等泛型入口；网络入口为 `LiteRuntime.Get<NetworkSystem>().Http`。

## 资源与构建

资源根目录为 `Assets/StandaloneAssets`。例如实际文件 `Assets/StandaloneAssets/Prefabs/Player.prefab` 的加载路径是 `Prefabs/Player.prefab`，包含扩展名。路径会统一斜杠和大小写。

Editor 模式直接访问项目资源；非编辑器环境会改用 Bundle。发布前通过 `Lite > Builder` 生成资源包和版本文件，使用 BuiltIn 或 Remote 定位模式。资源的加载、实例化和卸载统一由 Handle 管理，详见 [Asset 指南](Runtime/Asset/README.md)。

## 依赖与版本

源码工程使用 Unity `2022.3.62f3`；包版本与声明的最低 Unity 版本见 [package.json](package.json)。核心随包包含 UniTask、LitJson 和 IngameDebugConsole。Runtime 与 Editor 分属独立程序集。

LiteQuark 使用 MIT 许可证；第三方组件保留各自的许可证。

## 许可证

MIT License

Copyright (c) 2026 LiteQuark Framework

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---
