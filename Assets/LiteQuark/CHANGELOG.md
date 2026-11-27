# Changelog

## **[Unreleased]**

计划版本：`0.5.0`。从 `0.4.3` 升级：最低 Unity 版本为 **2022.3.62f3**。

- **包分发**：声明 uGUI `1.0.0` 依赖，包内附 [MIT 许可证](LICENSE.md)及[第三方许可声明](Third%20Party%20Notices.md)。直接复制源码时，目标项目也需启用 uGUI。
- **Launcher**：升级前备份场景和 Prefab，旧配置须手工转填到 **Release** 默认值和 **Development** 覆盖。将旧 `ILogic` 合并为 `IGameEntry`，把扩展系统及其参数填入 **System List**，恢复 UI 相机等场景引用。见 [Launcher 指南](README.md#release-默认值与-development-覆盖)。
- **资源构建**：将旧 Resource 资源纳入 Bundle，从源资源重建 Bundle 和版本清单并配套部署；旧相对路径清单不能通过读入再导出迁移。旧 `AssetMode`／`BundleLocater` 改用 `BundleMode`：Editor 模式改用 `Simulate=0`，旧 `BuiltIn=0` 和 `Remote=1` 分别迁为 `1` 和 `2`。
- **资源加载**：统一使用 Handle，`await handle` 后读取 `Result`，用完后 `Dispose()`。业务场景固定 Additive，换关时释放旧 `SceneHandle`，保留启动场景。见 [Asset 指南](Runtime/Asset/README.md)。
- **生命周期与日志**：自定义系统改用 `Initialize(CancellationToken ct = default)` 和 `UniTask DisposeAsync()`，集成层等待 `Shutdown()`，需要的数据由业务显式保存。新增 `ILateTick`；旧命名 Logger 和自定义日志后端改用 `LLog`。
- **任务调度**：支持分组并发和优先级，提交使用 `BaseTask`。将 `await task.Task` 改为 `await task.WaitForCompletionAsync()`，等待后读取 `State`、`Result` 或 `Error`。协程在提交任务后用 `yield return task.WaitForCompletionAsync().ToCoroutine()` 等待终态。WebGet 成功结果为 `byte[]`。见 [Task 指南](Runtime/Core/Async/Task/README.md)。
- **定时器**：`AddTimer`、`AddUnscaledTimer`、`AddTimerWithFrame` 和 `NextFrame` 返回 `ITimer`。需要停止时保存返回的对象并调用 `Cancel()`，仍使用 ID 的调用可读取 `timer.ID`。见 [Timer 指南](Runtime/Core/Async/Timer/README.md)。
- **动作**：`AddAction` / `AddBuilder` 返回 `IAction`，将保存的 ID 改为动作引用并调用 `Cancel()`，按 ID 操作可继续使用 `action.ID`。`SetFinalCallback` 已移除。单次序列的自然结束逻辑放在末尾的 `Callback`。有限循环如果需要在所有轮次完成后只回调一次，把循环动作放进外层单次序列，并在外层末尾添加 `Callback`。取消时的清理由拥有者执行。自定义动作通过继承 `BaseAction` 参与提交与组合，跨程序集的驱动覆写使用 `protected override`。见 [Action 指南](Runtime/Core/Async/Action/README.md)。
- **对象池**：预生成改用 `GenerateAsync(count)`，`WaitReadyAsync` 和 `PreloadPoolAsync` 返回 `UniTask<bool>`。见 [ObjectPool 指南](Runtime/Core/ObjectPool/README.md)。
- **路径工具**：资源路径转换改用 `AssetPath.ToProjectPath`／`ToRootRelativePath`；URI 拼接使用 `ConcatUri`，普通文件和目录操作使用 `System.IO`。见[路径与文件操作](README.md#通用路径与文件操作)。
- **编辑器命名**：将 `ProduceName`、`IncrementBuildModel`、`GotoBeginingContent` 的调用分别改为 `ProductName`、`IncrementalBuild`、`GoToBeginningContent`。

## **[0.4.3] - 2026-03-04**
- 修复：版本文件 URI 大小写导致的读取失败。

## **[0.4.2] - 2026-02-28**
- 新增：Action 支持未缩放时间，Debugger 补充 Timer、Event 和 Task 并发信息，`ResCollectorSetting` 支持清理无效目录。
- 变更：连续异常达到上限才进入错误状态，`EnterErrorState` 支持 `FrameworkErrorCode`。
- 优化：统一 `AssetPath` 处理。
- 修复：Release 下日志全部关闭的问题，保留 Error 和 Fatal 级别。

## **[0.4.1] - 2026-02-26**
- 新增：Timer 支持未缩放时间和 `CancelAll`；`LiteUtils.SafeInvoke` 增加多参数重载。
- 变更：任务提交统一 `Add` 前缀，`Abort` 改为 `Cancel`；`IsEnd`、`MarkSafety` 分别改为 `IsDone`、`MarkAsSafe`，状态机改用 `CanChangeTo`。通用方法从 `UIUtils` 移至 `UnityUtils`。
- 优化：Timer 支持单帧补发并改进总时长换算；关闭可取消在途初始化。
- 优化：完善 `SafeList` / `SafeDictionary` 的遍历保护和延后修改，统一用 `SafeInvoke` 隔离回调异常，减少 Info 日志的栈追踪开销。
- 移除：`CallbackFuncAction`、四参数 `Callback` / `DelayCallback` 重载及 `JsonUtils`。
- 修复：Timer 延迟累积、系统更新未应用 `Setting.TimeScale`、已加载场景的后端结果不一致及 `PositionGameObjectPool` 模板加载问题。
- 修复：类型判断、反射默认参数、Appender 清理和日志内部错误输出问题。

## **[0.4.0] - 2026-02-25**
- 新增：`HasAsset` 和 Handle 加载入口，UniTask 加载支持取消。
- 新增：下载重试参数 `RetryParam`、音频暂停与恢复、`DelayCallback`。
- 变更：废弃 `ListEx`，改用 `SafeList`。
- 优化：无网络时快速失败，捕获异步启动异常。
- 修复：资源加载失败后无法重试，以及依赖、回调重入、引用计数和释放问题。
- 修复：下载重试清理、取消和 `RunningTaskCount` 统计问题；修复事件回调异常或注销中断分发，以及遍历中 `Clear` 失败。
- 修复：组合动作提前释放、对象池模板回调和条件属性受 Attribute 顺序影响的问题。

## **[0.3.0] - 2026-02-09**
- 新增：NetworkSystem、DataSystem、Load Profiler 和对象池的 UniTask 接口。
- 变更：Setting 支持自定义系统配置，System 和 Logic 共用 EntryData 并支持自定义展示。
- 优化：前置不依赖 App 构建的必要设置，补充任务诊断。
- 修复：编辑器后台切换、资源加载中的重启和 Bundle Viewer 大小计算问题。

## **[0.2.6] - 2025-12-31**
- 修复：引擎未初始化时抛出FrameworkError，逻辑层收不到

## **[0.2.5] - 2025-12-31**
- 新增：System初始化失败的引擎事件
- 修复：VersionPack加载失败没有对外抛出事件的BUG

## **[0.2.4] - 2025-12-31**
- 新增：版本文件解密和比较、命令行构建参数 `copyToStreaming` 和 `FrameworkErrorEvent`。
- 变更：收窄 TaskSystem 暴露的接口。

## **[0.2.3] - 2025-12-01**
- 新增：网络相关逻辑与远程 Bundle 下载的重试机制
- 修复：AssetSystem 在 Remote 模式下依赖加载顺序错误的问题

## **[0.2.2] - 2025-11-20**
- 修复：重构导致的 Scene 加载失败问题

## **[0.2.1] - 2025-11-20**
- 新增：AssetSystem 增加 Resource 模式
- 变更：统一平台字符串（如 WebGL 与 WebGLPlayer），Remote 模式改用主版本号。
- 修复：版本号不足三位导致的错误

## **[0.2.0] - 2025-10-09**
- 新增：引入 UniTask，扩展 AssetSystem 打包选项，`VersionPackInfo` 支持加密。
- 优化：统一资源加载方式。

## **[0.1.0-preview] - 2025-02-25**
- 新增：远端 Bundle、通用对象 Inspector 绘制和 Codec 序列化。
- 重构：Task 模块。
- 优化：Logic 类型记录与 UI 显示。
- 修复：动态数组创建和 `showunitylogo` 构建选项问题。

## **[0.0.20-preview] - 2024-09-20**
- 新增：Project Builder 增加 SplitApplicationBinary 命令行参数支持
- 修复：命令行 Bool 参数解析错误

## **[0.0.19-preview] - 2024-09-10**
- 新增：Project Builder 增加 AAB 的 Split Binary 支持
- 新增：宏管理接口
- 优化：Project Builder 关联 CleanModel 与 DebugBuild 选项
- 修复：输出包命名错误

## **[0.0.18-preview] - 2024-08-29**
- 新增：Project Builder 增加 Callback（继承 IBuildCallback）
- 优化：构建完成弹窗逻辑，仅在成功时显示
- 优化：以 SynchronizationContext 替代 TaskSystem 中的主线程执行逻辑
- 修复：ListEx/DictionaryEx 在同一帧 Foreach→Add→Clear 的数据丢失问题

## **[0.0.17-preview] - 2024-08-26**
- 新增：Project Builder 增加 CreateSymbols 选项
- 新增：TaskSystem 增加 AsyncOperation 与 AsyncResult 类型的 Task
- 新增：ObjectSystem 封装 UnityEngine.Pool.CollectionPool 的部分功能

## **[0.0.16-preview] - 2024-07-31**
- 新增：TaskSystem 增加 PipelineTask
- 新增：AudioSystem 增加延迟播放参数
- 变更：`Timer.NextFrame` 默认使用 0 延迟。
- 新增：ObjectPoolSystem 增加异步实例化接口

## **[0.0.15-preview] - 2024-07-25**
- 优化：构建输出名称自动将空格替换为下划线
- 修复：Windows 平台构建脚本错误
- 修复：命令行参数与 Unity 冲突的问题

## **[0.0.14-preview] - 2024-07-19**
- 修复：Debug 构建参数未生效的问题

## **[0.0.13-preview] - 2024-07-19**
- 新增：AAB 构建支持
- 优化：Timer 风格与其他系统统一（使用 ID）
- 修复：Action 循环中无法停止其他新增 Action 的问题

## **[0.0.12-preview] - 2024-07-17**
- 新增：构建系统增加 App Clean Build 选项
- 优化：命令行构建模式
- 修复：Mac 无法打开目录的问题
- 修复：DebugMode 空引用问题

## **[0.0.11-preview] - 2024-07-17**
- 新增：Asset Viewer 资源浏览器
- 新增：多项新的构建参数
- 新增：DebugSetting 设置项
- 优化：运行时可动态添加 Logic

## **[0.0.10-preview] - 2024-07-12**
- 修复：Setting Inspector 界面错误

## **[0.0.9-preview] - 2024-07-12**
- 新增：异常抛出开关，可关闭框架异常捕获，由项目方统一上报
- 新增：日志增加 Simple 模式，启用后使用 UnityLog
- 修复：使用 Text 组件的项目字体丢失问题

## **[0.0.8-preview] - 2024-07-09**
- 新增：AssetSystem 的 Retain 开关、`RectTransformMoveAction`（操作 `anchoredPosition`）和 `BaseUI.ReplaceSprite`。
- 优化：Bundle 自动收集排除 Package 目录；统一释放 UI 加载的资源。
- 修复：UI 同帧关闭后重开的生命周期问题。

## **[0.0.7-preview] - 2024-07-02**
- 新增：ActionSystem 的 `SetParent` 等接口。
- 优化：Bundle 打包增加根目录以避免冲突，改进构建界面。
- 修复：构建完成和 `GetPool` 的异常。

## **[0.0.6-preview] - 2024-06-19**
- 新增：EventSystem 支持独立模块注册与注销。
- 修复：`MovePathAction` 耗时超过设定值。

## **[0.0.5-preview] - 2024-05-31**
- 变更：内部系统改用 LitJson。
- 移除：Newtonsoft.Json 依赖。

## **[0.0.4-preview] - 2024-05-31**
- 修复：资源管理器自动释放资源导致无法再次加载的问题
- 修复：音效资源泄露问题

## **[0.0.3-preview] - 2024-05-29**
- 新增：AudioSystem 和 EmptyObjectPool。
- 更新：IngameDebugConsole。

## **[0.0.2-preview] - 2024-05-20**
- 新增：ActionSystem。
- 变更：调整外部访问系统的方式。

## **[0.0.1-preview] - 2024-04-23**
- 新增：LiteQuark 初版（包含 Asset 相关功能）
