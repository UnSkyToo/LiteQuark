# Changelog

## **[0.4.0] - 2026-02-14**
- 新增：AudioSystem 增加暂停和恢复接口
- 新增：ActionSystem增加DelayCallback
- 新增：增加 RetryParam，提供重试间隔的参数
- 优化：简化 AssetCacheStage 状态，移除 Unloading 瞬态
- 优化：废弃 ListEx，统一使用 SafeList
- 优化：无网络情况下的请求直接失败，避免等待超时
- 优化：更安全的框架启动流程，捕获异步异常
- 优化：InstantiateAsync 不再重复注册 prefab 到 IDToPathMap
- 修复：Bundle 加载失败后状态未重置，导致无法再次加载的 BUG
- 修复：多播委托异常导致后续 Bundle 回调丢失的 BUG
- 修复：回调中重新加载资源导致的迭代器修改异常
- 修复：加载失败时 Bundle 引用计数异常增加的 BUG
- 修复：依赖 Bundle 加载成功但主 Bundle 失败时，引用悬空的 BUG
- 修复：依赖信息缺失时 Bundle 加载卡死的 BUG
- 修复：未加载的 Bundle 执行 Unload 破坏依赖引用计数的 BUG
- 修复：Asset 加载失败后 AssetInfoCache 不过期导致的内存泄漏
- 修复：依赖加载失败时主 Bundle 的 AssetBundle 对象未释放的内存泄漏
- 修复：DecRef 在非法状态下仍执行导致引用计数下溢的 BUG
- 修复：Dispose 时未触发 pending 回调导致 UniTask 无法 resolve 的 BUG
- 修复：场景未加载时卸载导致引用计数错误的 BUG
- 修复：事件响应中抛出异常导致后续事件中断的 BUG
- 修复：事件回调中反注册导致集合修改异常的 BUG
- 修复：DownloadTask 重试未释放 Request 的 BUG
- 修复：取消任务时未取消下载重试计时器的 BUG
- 修复：ObjectPool 模板加载回调的多播问题
- 修复：ParallelAction/SequenceAction 中错误的提前 Dispose 行为
- 修复：SafeList 和 SafeDictionary 在 Foreach 中 Clear 导致的迭代器异常
- 修复：Attribute 顺序错误导致 Condition 属性未生效的 BUG
- 修复：TaskSystem 异常路径下 RunningTaskCount 不一致的 BUG

## **[0.3.0] - 2026-02-09**
- 新增：NetworkSystem 网络模块
- 新增：DataSystem 数据持久化模块
- 新增：Load Profiler模块，资源加载分析器
- 新增：ObjectPool增加UniTask接口版本
- 优化：Setting配置方式，支持自定义System配置Setting
- 优化：重构EntryData，System和Logic共享，也支持自定义的数据展示
- 优化：打包脚本部分设置前置，放置没有勾选App阶段跳过了必要设置
- 优化：Task增加更详细的任务名称和打印
- 修复：编辑器模式重启切后台报错
- 修复：资源加载中重启框架报错
- 修复：Bundle Viewer大小计算异常

## **[0.2.6] - 2025-12-31**
- 修复：引擎未初始化时抛出FrameworkError，逻辑层收不到

## **[0.2.5] - 2025-12-31**
- 新增：System初始化失败的引擎事件
- 修复：VersionPack加载失败没有对外抛出事件的BUG

## **[0.2.4] - 2025-12-31**
- 新增：AssetViewer增加解密版本文件功能
- 新增：资源版本比较工具
- 新增：命令行打包增加新参数copyToStreaming
- 新增：引擎报错事件FrameworkErrorEvent，方面业务层处理如网络请求失败的业务逻辑
- 优化：TaskSystem权限优化，避免过多不必要的函数暴露

## **[0.2.3] - 2025-12-01**
- 新增：网络相关逻辑与远程 Bundle 下载的重试机制
- 修复：AssetSystem 在 Remote 模式下依赖加载顺序错误的问题

## **[0.2.2] - 2025-11-20**
- 修复：重构导致的 Scene 加载失败问题

## **[0.2.1] - 2025-11-20**
- 新增：AssetSystem 增加 Resource 模式
- 优化：统一平台字符串（如 WebGL 与 WebGLPlayer）
- 优化：Remote 模式下使用主版本号
- 修复：版本号不足三位导致的错误

## **[0.2.0] - 2025-10-09**
- 新增：AssetSystem 增加新的打包模式与编译选项；统一加载方式
- 新增：引入 UniTask
- 重构：VersionPackInfo 支持加密并优化加载逻辑
- 修复：若干稳定性问题

## **[0.1.0-preview] - 2025-02-25**
- 重构：Task 模块
- 新增：Asset 模块支持远程资源，可将所有 Bundle 放置于远端
- 新增：编辑器扩展模块，可直接绘制对象，支持大部分类型
- 新增：Codec 模块，可序列化复杂类型
- 优化：Logic Type 记录方式更加稳健
- 优化：多项 UI 显示细节
- 修复：TypeUtils 数组动态创建的特定问题
- 修复：showunitylogo 构建选项被错误关闭的问题

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
- 优化：Timer.NextFrame 默认使用 0 延迟
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
- 新增：AssetSystem 增加 Retain 开关
- 新增：RectTransformMoveAction（操作 anchorPosition）
- 新增：BaseUI 增加 ReplaceSprite 接口；缓存所有加载的 Asset 做统一释放
- 优化：Bundle 自动收集规则排除 Package 目录
- 优化：多项 UI 细节
- 修复：UI 同一帧关闭再打开导致生命周期错误的问题

## **[0.0.7-preview] - 2024-07-02**
- 优化：Bundle 打包增加根目录，避免冲突
- 优化：ActionSystem 增加 SetParent 并补全部分接口
- 优化：构建工具界面；修复构建完成后的报错
- 修复：GetPool 在部分情况下的异常

## **[0.0.6-preview] - 2024-06-19**
- 新增：EventSystem 拆分模块，系统可独立注册/反注册
- 优化：ActionSystem
- 修复：MovePathAction 耗时超出设定的问题

## **[0.0.5-preview] - 2024-05-31**
- 新增：内部系统改用 LitJson，移除 Newtonsoft.Json

## **[0.0.4-preview] - 2024-05-31**
- 修复：资源管理器自动释放资源导致无法再次加载的问题
- 修复：音效资源泄露问题

## **[0.0.3-preview] - 2024-05-29**
- 新增：AudioSystem
- 新增：EmptyObjectPool
- 更新：IngameDebugConsole 至最新版本
- 修复：若干稳定性问题

## **[0.0.2-preview] - 2024-05-20**
- 新增：ActionSystem
- 优化：优化已有逻辑
- 优化：调整外部访问 System 的方式
- 修复：若干稳定性问题

## **[0.0.1-preview] - 2024-04-23**
- 新增：LiteQuark 初版（包含 Asset 相关功能）
