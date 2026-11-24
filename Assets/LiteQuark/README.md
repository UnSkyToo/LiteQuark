# LiteQuark Framework

LiteQuark 将系统服务（`ISystem`）与可选的游戏入口（`IGameEntry`）分开管理，通过 `LiteRuntime` 访问已初始化的服务。本文描述当前源码；发布历史见 [CHANGELOG](CHANGELOG.md)。

## 安装

在 Package Manager 使用 `https://github.com/UnSkyToo/LiteQuark.git#LiteQuark`，或将本目录连同 `.meta` 文件复制到项目中。发布分支内容会更新，可复现安装应选择核对过的 tag 或 commit。版本和最低 Unity 声明见 [package.json](package.json)，声明不代表所有旧编辑器都经过兼容验证。

## 接入与启动

1. 在引导场景的 GameObject 上挂载 `LiteLauncher`。
2. 在 Launcher 直接编辑 **Common / Task / Asset / Action / Log**，选择可选的 **Game Entry**。在 **System List** 点 **+** 选择扩展系统，需要的参数会随该条目创建。
3. 入口使用有公共无参构造函数的普通 C# 类。系统就绪后 Runtime 创建并等待入口初始化，关闭时先等待入口释放。按需实现 `ITick`、`ILateTick` 或两者。

```csharp
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;

[UnityEngine.Scripting.Preserve]
public sealed class GameEntry : IGameEntry
{
    public UniTask<bool> Initialize()
    {
        LLog.Info("Game entry initialized");
        return UniTask.FromResult(true);
    }

    public UniTask DisposeAsync()
    {
        return UniTask.CompletedTask;
    }
}
```

### Release 默认值与 Development 覆盖

一个项目使用一个常驻启动场景。`LiteLauncher` 在 Awake 时调用 `DontDestroyOnLoad`，Runtime 重启复用同一个 Launcher。单场景项目可以通过 Prefab 管理玩法；需要独立关卡时，用 `LoadSceneHandle` 叠加加载，离开时释放对应 Handle。两个 Demo 是可独立运行的启动场景，各自保存配置。

Launcher 的 **Environment** 同时选择当前编辑环境和下次 Editor 启动或重启的预览环境。**Release** 页直接编辑默认值、共享字段及 System List。**Development** 页显示实际生效值，普通字段、游戏入口和系统列表结构只读，模块与系统条目仍可展开。

扩展设置里的列表和数组也可以在 Development 页展开，元素中的 `BuildValue<T>` 可单独覆盖。增删、排序和修改普通字段都在 Release 页完成。

在 Development 页，`BuildValue<T>` 字段右侧有 **Override**。未勾选时显示 Release 值并锁定输入；勾选后只编辑 Development 值。取消勾选立即恢复继承，再次勾选可继续使用上次的开发值。显式覆盖支持 `false`、`0`、空字符串及 `LogLevel.None`。资源模式、CDN、日志级别、日志窗口和异常抛出策略使用此机制。扩展模块可声明自己的 `BuildValue<T>`，初始化时用 `Resolve(LiteRuntime.Setting.Environment)` 取值。

普通 Player 使用 Release，Development Build 使用 Development。两个 Demo 的 Release 使用 BuiltIn 和 Error/Fatal 日志；Development 使用 Simulate、全部级别和日志窗口。当前环境的资源模式为 Remote 时，Inspector 会检查实际生效的 CDN 地址；空值或无效的绝对 URI 会显示警告。预览 BuiltIn 需要准备本地 Bundle。

每个扩展系统的参数直接保存在它的 **System List** 注册项中，禁用保留参数，拖动条目会一起移动参数。系统升级后需要新增配置时，在 Release 页点该行的 **Create Settings**；配置类型发生变化时，先核对旧值再点 **Replace Settings**。替换会创建新类型的默认值，可用 Undo 恢复旧值。UI 相机在 UISystem 项内绑定，可直接拖入场景对象。

Launcher 每次启动或重启都解析一份独立的运行配置。通过 `LiteRuntime.Setting.Asset` 和 `LiteRuntime.Setting.Log` 等模块读取有效值；运行副本中的场景对象引用仍指向同一个 Unity 对象。集成方显式调用 `Startup(launcher, setting)` 时，重启沿用调用方提供的有效设置。

## 生命周期与注册

已配置入口时启动顺序为 `Idle → InitSystem → InitGameEntry → Running`，入口留空时从 InitSystem 直接进入 Running，`Startup()` 只允许从 Idle 调用。启动过程异步执行，业务对服务的访问应放在 `IGameEntry.Initialize()` 或之后。`LiteLauncher` 驱动每帧更新、前后台通知和退出清理。

Runtime 统一管理各个系统和游戏入口的生命周期。初始化方法持有当前实例直到发布；失败或取消时，先就地释放该实例，已发布的依赖保持可用。System 成功后立即可查询；本轮系统遍历中发布的对象从下一次系统遍历起参与更新。当前实例释放成功后，Runtime 报告启动失败并关闭已发布对象。错误通知中的订阅者异常会记录到日志，回滚继续执行。清理成功后回到 Idle，Idle 不表示初始化成功。初始化期间的关闭请求会阻止后续初始化并等待当前 Initialize 退出，不会强行终止模块内部的异步操作。

`Shutdown()` 返回 UniTask，进入 ShuttingDown 后先等待游戏入口释放，再逆序等待 System 的 DisposeAsync；完成后进入 Idle，释放已发布或未发布实例抛异常时进入 ShutdownFailed，停止更新并拒绝再次关闭或启动。关闭期间重复 Shutdown 被拒绝，错误报告不会覆盖关闭状态。关闭时停止入口更新，系统推进不受 IsPause 和业务时间缩放阻挡；系统从开始释放起停止更新。DisposeAsync 可以等待尚未关闭的 Timer、Task 或 PlayerLoop，不应等待本系统自己的 Tick。Error 停止框架更新；运行期错误仍可显式 Shutdown。

业务需要重启时调用 `LiteRuntime.Instance.Restart()`，不要直接释放已注册系统或继续使用旧实例。框架集成层调用 Shutdown 时应等待其完成。系统和游戏入口的 DisposeAsync 需要支持部分初始化状态；同步清理可直接返回 `UniTask.CompletedTask`，业务持有的 Handle、Lease 等仍按各自契约 Dispose。

Restart 仅在 Running 接受请求，下一帧执行；执行前的重复请求合并为一次。ShuttingDown 期间请求被忽略，普通 Shutdown 不会因此转为重启。如果重启期间收到应用退出通知，旧系统继续关闭，完成后停在 Idle。Idle、InitSystem、InitGameEntry、Error 和 ShutdownFailed 调用 Restart 抛出 InvalidOperationException。已接受的请求在执行时必须仍处于 Running，中途进入错误或开始关闭会放弃该请求。启动失败后如需重试，应等清理回到 Idle 后调用 Startup。后台超时自动重启也仅在 Running 触发，以前台事件处理结束后的状态为准；初始化期间返回前台继续原初始化流程。

资源加载中或玩法场景仍在场时可以请求 Restart 或 Shutdown。需要回到纯启动场景的项目，由业务释放 SceneHandle 并完成退场；旧 Bundle 会随资源系统关闭而释放。F5 和后台超时重启走同一流程。详见[资源关闭](Runtime/Asset/README.md#系统关闭)。应用退出回调只尽力发起清理，不保证跨帧等待能够完成；必要的数据保存应在退出前完成。

Editor 停止 Play Mode 时，Unity 可能先销毁场景对象，随后异步清理续体才执行。释放逻辑访问 Unity 对象前应使用 `obj != null` 检查其原生对象是否仍存在；`?.` 只检查托管引用。已经由 Unity 销毁的对象无需再次 Destroy。

每次启动或重启，[SystemCenter](Runtime/Internal/SystemCenter.cs) 按固定顺序逐个创建并初始化内置系统：`Log → Event → Timer → Task → Asset → ObjectPool → Action`。

所有内置系统就绪后，扩展系统按 Launcher 的 **System List** 从上到下初始化。禁用项会跳过，同一类型只初始化一次。每轮启动按 `LiteSetting.GameEntry` 保存的程序集限定类型名创建新的入口实例。仅通过 Inspector 引用的入口应在具体类上使用 `[Preserve]` 或由项目的 `link.xml` 保留其无参构造。

| 扩展点 | 约定 |
| --- | --- |
| `ISystem`、`IGameEntry` | `UniTask<bool> Initialize()`、`UniTask DisposeAsync()` |
| `ITick` | Unity Update 中调用 `Tick(float deltaTime)`。先按注册顺序更新系统，再更新 Running 状态的游戏入口 |
| `ILateTick` | Unity LateUpdate 中调用 `LateTick(float deltaTime)`。先更新游戏入口，再按注册顺序更新系统，可独立于 `ITick` 实现 |
| `ISystemSettingProvider<TSetting>` | 实现可写 `Setting` 属性，初始化前由框架注入配置 |
| 配置类 | 实现 `ISystemSetting`，使用可序列化类型和无参构造函数 |

程序化注册使用 `LiteSystemEntry`，将系统类型名和可选的 `Setting` 放在同一条目。未指定参数时，具有配置契约的系统使用默认实例并记录警告。更改启动配置应在初始化前完成；修改列表不会动态注册系统。

晚更新复用本帧普通更新的 deltaTime，适合在游戏入口完成相机跟随后，由系统投影世界坐标。`LiteRuntime.Instance.CanLateTick` 表示 Runtime 处于 Running 且未暂停，分发子对象时可用它判断是否继续。暂停、关闭或错误会停止后续晚更新。自定义驱动先调用 `Tick(deltaTime)`，同一帧再调用一次 `LateTick()`。跳过普通更新的帧也跳过晚更新。

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
| [Asset](Runtime/Asset/README.md) | Simulate / BuiltIn / Remote 加载、Handle 所有权、缓存与预加载 |
| [Event](Runtime/Core/Event/README.md) | 类型化事件、Tag 注销、独立事件模块 |
| [Task](Runtime/Core/Async/Task/README.md) | 协程、回调任务、并发限制、主线程投递 |
| [Timer](Runtime/Core/Async/Timer/README.md) | 时间回调、重复执行、暂停与取消 |
| [Action](Runtime/Core/Async/Action/README.md) | Transform 补间、序列/并行、条件等待 |
| [ObjectPool](Runtime/Core/ObjectPool/README.md) | GameObject、集合复用及预生成 |
| Log | `LLog` 与 `LiteRuntime.Log`，由 `LiteRuntime.Setting.Log` 配置。`Levels` 过滤框架日志（None 关闭全部级别），`ShowLogViewer` 单独控制窗口；Development 和 Release 均使用所选策略，入口可留空 |

UI、Audio、Effect、Network、Data、Config 属于可选的 `LiteQuark.Addons` 包，不会自动注册。安装并注册后使用 `LiteRuntime.Get<UISystem>()` 等泛型入口；网络入口为 `LiteRuntime.Get<NetworkSystem>().Http`。

## 资源与构建

资源默认位于 `Assets/StandaloneAssets`，加载路径包含扩展名，可使用根目录相对路径或完整 Assets 项目路径。加载、实例化、预加载和场景所有权通过 Handle 管理；持有期间使用结果，结束时 Dispose。路径规则、错误处理和配置见 [Asset 指南](Runtime/Asset/README.md)。

AssetSystem 在初始化时选择加载后端，交由 AssetManager 统一执行 Bundle 管理流程。Development 默认 Simulate，在编辑器中收集项目资源进行模拟，非编辑器环境自动使用 BuiltIn 本地资源包；可显式选择 BuiltIn 或 Remote 验证真实 Bundle。通过 `Lite > Builder` 构建资源包和版本文件；目标平台须安装对应 Unity 构建支持。命令行入口为 `LiteQuark.Editor.ProjectBuilderCommandLine.Build`，参数见 [ProjectBuilderCommandLine.cs](Editor/Builder/ProjectBuilderCommandLine.cs)。`-copyToStreaming true` 会删除并替换 `Assets/StreamingAssets/LiteQuark`，构建准备也可能修改项目设置，完成后检查差异。

## 通用路径与文件操作

资源身份使用 AssetPath；磁盘路径、URI 与普通文件操作分别使用以下入口。通用路径工具不检查资源是否已打包，也不承担目录范围限制。

| API | 使用约定 |
| --- | --- |
| `PathUtils.UnifyPath` | 仅将反斜杠转为 `/`，保留根、末尾分隔符和大小写 |
| `ConcatPath` | 按当前平台 Path.Combine 组合；后续根路径可替换前文，不限制 `..`，不用于 URI |
| `ConcatUri` | 把绝对基础 URI 当目录追加片段，逐段转义并保留查询和 fragment；传未编码名称，如 `My Icon.ab`，不要先编码成 `%20` |
| `GetParentPath` | 按宿主文件系统求父目录，输出 `/`；根目录无父级时返回空字符串，不用于 URL 或跨系统解析盘符 |
| `GetRelativePath` | 可能返回 `../`、`.` 或不同根的完整路径，不能用结果直接判断目录归属 |
| `GetAssetDataPath` / `GetPersistentDataPath` | 基于 Unity 对应磁盘目录组合路径 |
| `GetStreamingAssetsPath` | 随 Unity 返回的位置选择磁盘路径或 URI 组合；tag/path 传相对子路径，实际读取仍需对应平台支持 |
| `CopyDirectory` | 递归复制通过过滤的文件并覆盖同名目标，不生成空目录；源不存在或全部被过滤时不创建目标，不删除目标已有内容 |
| `RenameFile` | 创建目标父目录；源文件不存在返回 false，其他失败按文件系统异常处理 |

CopyDirectory 禁止目标等于源或位于源内部，不保证解析符号链接。普通文件／目录检查和创建使用 `System.IO.File`、`System.IO.Directory`；编辑器资产目录判断使用 `AssetDatabase.IsValidFolder()`。编辑器工作目录入口为 `LiteEditorUtils.GetLiteQuarkRootPath()`。

完整签名见 [PathUtils.cs](Runtime/Utils/PathUtils.cs)，旧 API 的替换方式见 [CHANGELOG](CHANGELOG.md)。

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
