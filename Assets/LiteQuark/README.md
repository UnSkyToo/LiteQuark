# LiteQuark Framework

LiteQuark 是轻量级 Unity 框架，提供应用启动、生命周期、资源加载和常用运行时服务。升级与发布历史见 [CHANGELOG](CHANGELOG.md)。

## 安装

当前源码使用 Unity **2022.3.62f3**，最低版本见 [package.json](package.json)。将本目录连同 `.meta` 文件复制到项目，或在 Package Manager 使用 `https://github.com/UnSkyToo/LiteQuark.git#LiteQuark`。可复现安装应固定已核对的 tag 或 commit。

## 接入与启动

1. 在常驻启动场景的 GameObject 上挂载 `LiteLauncher`。独立关卡用 `LoadSceneHandle` 叠加加载，离开时释放对应 Handle。
2. 在 Launcher 编辑 **Common / Task / Asset / Action / Log**。通过 **System List > +** 注册扩展系统并编辑该条目的设置。
3. 按需选择 **Game Entry**。入口是实现 `IGameEntry`、具有公共无参构造函数的 C# 类。仅由 Inspector 引用的入口使用 `[Preserve]` 或 `link.xml` 保留构造函数。

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;

[UnityEngine.Scripting.Preserve]
public sealed class GameEntry : IGameEntry
{
    public UniTask<bool> Initialize(CancellationToken ct = default) => UniTask.FromResult(true);
    public UniTask DisposeAsync() => UniTask.CompletedTask;
}
```

### Release 默认值与 Development 覆盖

**Environment** 选择 Inspector 的编辑页和下次 Editor 启动或重启的预览环境。普通 Player 使用 Release；Development Build 使用 Development。

- **Release**：编辑默认值、共享字段、游戏入口及 **System List**。每个条目保存自己的设置；禁用保留设置，重排时设置随条目移动。新增或更换配置类型时使用 **Create Settings** / **Replace Settings**。替换会使用新类型的默认值，可用 Undo 恢复。
- **Development**：显示生效值；普通字段和列表结构只读。展开条目或数组元素后，可对 `BuildValue<T>` 勾选 **Override**；取消即恢复继承，已填的覆盖值会保留。`false`、`0`、空字符串和 `LogLevel.None` 都可以是有效覆盖值。

资源模式、CDN、日志与异常抛出策略支持覆盖；自定义设置的 `BuildValue<T>` 字段用 `Resolve(LiteRuntime.Setting.Environment)` 取值。每次启动或重启从 Launcher 解析独立的 `LiteRuntime.Setting`，场景对象引用保持原对象；显式传给 `Startup(launcher, setting)` 的配置由重启沿用。

## 生命周期与注册

- **启动**：`LiteLauncher` 自动驱动；自定义集成可在 Idle 调用 `LiteRuntime.Instance.Startup(launcher)`。内置系统按 `Log → Event → Timer → Task → Asset → ObjectPool → Action` 初始化，随后按 **System List** 顺序初始化启用的扩展系统（同类型一次），最后初始化可选的游戏入口。业务在 `IGameEntry.Initialize` 或之后获取系统。
- **失败与取消**：初始化共享启动 token，应传给实际异步操作。启动中关闭会取消 token 并等待初始化退出。失败或取消会释放部分初始化的对象，再逆序释放已初始化的系统；重试须等清理回到 Idle。
- **关闭**：停止提交新工作并 `await LiteRuntime.Instance.Shutdown()`，运行期进入 Error 后也可调用。框架先释放游戏入口，再逆序释放系统；单个释放错误会记录，清理继续。`DisposeAsync()` 须支持部分初始化，可等待尚未关闭的 Timer、Task 或 PlayerLoop，不能等待本系统的 Tick。关闭中不要重复调用；退出可能中断异步清理，必要数据应提前保存。
- **重启**：`Restart()` 在 Running 受理，下一帧执行，重复请求合并；ShuttingDown 忽略请求，其余状态抛 `InvalidOperationException`。运行中途发生错误或开始关闭会放弃待执行的请求；重启清理失败或遇到退出会停在 Idle。重启后重新获取系统。业务场景的退场责任见 [Asset 系统关闭](Runtime/Asset/README.md#系统关闭)。

| 扩展点 | 约定 |
| --- | --- |
| `ISystem`、`IGameEntry` | `UniTask<bool> Initialize(CancellationToken ct = default)`、`UniTask DisposeAsync()`；同步清理可返回 `UniTask.CompletedTask` |
| `ITick` | Unity Update 中调用 `Tick(float deltaTime)`。先按注册顺序更新系统，再更新 Running 状态的游戏入口 |
| `ILateTick` | Unity LateUpdate 中调用 `LateTick(float deltaTime)`。先更新游戏入口，再按注册顺序更新系统，可独立于 `ITick` 实现 |
| `ISystemSettingProvider<TSetting>` | 框架在初始化前注入可写 `Setting`；配置须可序列化、有无参构造，并实现 `ISystemSetting` |

程序化注册在启动前填写 `LiteSystemEntry` 的程序集限定类型名及可选的 `Setting`。缺少所需配置时使用默认实例并记录警告。修改列表不会动态注册系统。

晚更新复用本帧的 deltaTime，只在 Running 且未暂停时分发（`CanLateTick`）。自定义驱动应在同一帧先 `Tick(deltaTime)` 再 `LateTick()`。`Common.TimeScale` 只在 Editor 缩放正常更新的 deltaTime，不修改 `Time.timeScale`。

停止 Play Mode 时，异步释放可能晚于 Unity 对象销毁；访问前请用 `obj != null` 检查原生对象是否仍在。

## 服务与模块

通过 `LiteRuntime.Asset`、`LiteRuntime.Event` 等入口访问内置服务。`LiteRuntime.Get<T>()` 也可查询已初始化的内置或扩展系统，未找到时返回 `null`。

| 核心模块 | 说明 |
| --- | --- |
| [Asset](Runtime/Asset/README.md) | Simulate / BuiltIn / Remote 加载、Handle 所有权、缓存与预加载 |
| [Event](Runtime/Core/Event/README.md) | 类型化事件、Tag 注销、独立事件模块 |
| [Task](Runtime/Core/Async/Task/README.md) | 协程、回调任务、并发限制、主线程投递 |
| [Timer](Runtime/Core/Async/Timer/README.md) | 时间回调、重复执行、暂停与取消 |
| [Action](Runtime/Core/Async/Action/README.md) | Transform 补间、序列/并行、条件等待 |
| [ObjectPool](Runtime/Core/ObjectPool/README.md) | GameObject、集合复用及预生成 |
| Log | `LLog` 输出到 Unity，受 `Log.Levels` 过滤。`Fatal` 控制 `LLog.Exception` 的输出，`None` 关闭全部级别；`ShowLogViewer` 单独控制游戏内窗口 |

UI、Audio、Effect、Network、Data、Config 属于 `LiteQuark.Addons`；安装并注册后通过 `LiteRuntime.Get<T>()` 使用。

## 资源与构建

默认资源根目录为 `Assets/StandaloneAssets`。保留 Handle 直到用完资源、实例或场景，再调用 `Dispose()`。加载模式、寻址和部署规则见 [Asset 指南](Runtime/Asset/README.md)。

安装目标平台的 Unity 构建支持后，通过 `Lite > Builder` 或 [命令行入口](Editor/Builder/ProjectBuilderCommandLine.cs) 构建。`-copyToStreaming true` 会替换 `Assets/StreamingAssets/LiteQuark`；构建准备还可能修改项目设置。

## 通用路径与文件操作

资源身份用 `AssetPath`。普通文件与目录操作用 `System.IO`；[PathUtils](Runtime/Utils/PathUtils.cs) 提供以下常用操作：

| API | 使用约定 |
| --- | --- |
| `UnifyPath` / `ConcatPath` | 前者仅将反斜杠转为 `/`；后者按宿主平台 `Path.Combine` 组合，不限制 `..` 或根路径替换 |
| `ConcatUri` | 把绝对基础 URI 当目录，追加并转义未编码的名称，保留查询和片段 |
| `GetParentPath` / `GetRelativePath` | 按宿主文件系统解析；根无父级时返回空字符串，相对结果可能是 `../`、`.` 或另一根下的绝对路径，不能据此判断目录归属 |
| `GetAssetDataPath` / `GetPersistentDataPath` / `GetStreamingAssetsPath` | 基于 Unity 目录组合；StreamingAssets 按 Unity 返回的地址选择磁盘路径或 URI，传相对子路径 |
| `CopyDirectory` | 递归复制匹配文件，只为复制的文件建目录并覆盖同名目标；禁止目标等于源或位于源内，不保证解析符号链接 |
| `RenameFile` | 创建目标父目录；源文件不存在返回 `false`，其他文件系统异常向外传播 |

## 依赖与版本

核心依赖 uGUI（`com.unity.ugui`）；直接复制源码时，请在目标项目启用该包。核心随包包含 UniTask、LitJson 和 IngameDebugConsole，Runtime 与 Editor 分属独立程序集。

## 许可证

LiteQuark 使用 [MIT 许可证](LICENSE.md)。随包组件的许可见[第三方许可声明](Third%20Party%20Notices.md)。
