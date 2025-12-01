# Changelog

## [0.2.3] - 2025-12-01
- 新增：所有网络相关逻辑和远程Bundle下载增加重试机制
- 修复：AssetSystem在Remote模式下依赖加载顺序错误的BUG

## [0.2.2] - 2025-11-20
- 修复：重构导致的Scene加载失败

## [0.2.1] - 2025-11-20
- 新增：优化AssetSystem，增加Resource模式
- 优化：统一了平台字符串，例如WebGL和WebGLPlayer
- 优化：AssetSystem在Remote模式采用主版本号
- 修复：版本号不足3位导致的BUG

## [0.2.0] - 2025-10-09
- 新增：AssetSystem增加新打包模式和编译选项，统一加载方式
- 新增：引入UniTask库
- 重构：VersionPackInfo支持加密，优化加载
- 修复：稳定性修复

## [0.1.0-preview] - 2025-02-25
- 重构：Task模块重构
- 新增：Asset模块支持远程资源，可把所有Bundle放到远端
- 新增：编辑器扩展模块，可直接绘制object，支持绝大部分类型
- 新增：Codec模块，可以序列化较为复杂的类型
- 优化：优化Logic Type记录方式，使用系统自带的名称更健壮
- 优化：若干显示优化
- 修复：TypeUtils中数组动态创建的特定bug
- 修复：打包showunitylogo选项会被错误关闭的bug

## [0.0.20-preview] - 2024-09-20
- 新增：Project Builder增加SplitApplicationBinary命令行打包参数支持
- 修复：命令行打包Bool参数解析错误的BUG

## [0.0.19-preview] - 2024-09-10
- 新增：Project Builder添加AAB对Split Binary支持
- 新增：宏管理接口
- 优化：Project Builder关联CleanModel和DebugBuild选项
- 修复：出包命名错误的问题

## [0.0.18-preview] - 2024-08-29
- 新增：Project Builder增加Callback回调支持（继承IBuildCallback）
- 优化：打包结束后的弹窗逻辑，只有成功才弹窗
- 优化：使用SynchronizationContext代替TaskSystem中执行主线程的方法
- 修复：ListEx、DictionaryEx中同一帧Foreach中Add后Clear导致Add的数据被清掉的BUG

## [0.0.17-preview] - 2024-08-26
- 新增：Project Builder增加CreateSymbols选项
- 新增：TaskSystem增加AsyncOperation和AsyncResult类别的Task
- 新增：ObjectSystem增加UnityEngine.Pool.CollectionPool的部分封装

## [0.0.16-preview] - 2024-07-31
- 新增：TaskSystem增加PipelineTask
- 新增：AudioSystem增加延迟播放参数
- 优化：Timer.NextFrame默认采用0延迟
- 新增：ObjectPoolSystem增加异步实例化接口

## [0.0.15-preview] - 2024-07-25
- 优化：出包名称会用_替代空格
- 修复：Window平台打包脚本报错
- 修复：打包命令行参数与unity重复的BUG

## [0.0.14-preview] - 2024-07-19
- 修复：打包Debug参数未生效的BUG

## [0.0.13-preview] - 2024-07-19
- 新增：增加AAB支持
- 优化：统一Timer风格和其它系统一致用ID
- 修复：Action循环中无法停止其它新增Action的BUG

## [0.0.12-preview] - 2024-07-17
- 新增：打包系统增加App Clean Build选项
- 优化：命令行编译模式
- 修复：Mac无法打开目录的BUG
- 修复：DebugMode报空的BUG

## [0.0.11-preview] - 2024-07-17
- 新增：Asset Viewer资源查看器
- 新增：若干新的打包参数
- 新增：增加DebugSetting设置
- 优化：现在运行时可增加Logic

## [0.0.10-preview] - 2024-07-12
- 修复：Setting Inspector界面报错BUG

## [0.0.9-preview] - 2024-07-12
- 新增：异常抛出选项，可以关闭框架异常处理，由项目组上报
- 新增：日志新增Simple开关，开启后直接使用UnityLog
- 修复：还在使用Text的项目字体丢失BUG

## [0.0.8-preview] - 2024-07-09
- 新增：AssetSystem增加Retain开关
- 新增：RectTransformMoveAction，操作anchorPosition
- 新增：BaseUI增加ReplaceSprite接口，缓存所有Load的Asset，统一释放
- 优化：Bundle自动收集规则排除Package目录
- 优化：部分界面细节优化
- 修复：UI同一帧关闭再打开生命周期错误的问题

## [0.0.7-preview] - 2024-07-02
- 优化：Bundle打包增加根目录，避免冲突
- 优化：ActionSystem新增SetParent，完成部分接口
- 优化：打包工具界面优化，修复打包结束后的报错
- 修复：GetPool部分情况下报错修复

## [0.0.6-preview] - 2024-06-19
- 新增：EventSystem拆分模块，所有系统可以独立注册和反注册
- 优化：ActionSystem
- 修复：MovePathAction耗时比设定多的BUG

## [0.0.5-preview] - 2024-05-31
- 新增：内部系统使用LitJson，移除NewtonJson库

## [0.0.4-preview] - 2024-05-31
- 修复：资源管理器自动释放资源导致无法再次加载的BUG
- 修复：音效资源泄露的问题

## [0.0.3-preview] - 2024-05-29
- 新增：AudioSystem
- 新增：EmptyObjectPool
- 更新：最新Third/IngameDebugConsole库
- 修复：稳定性修复

## [0.0.2-preview] - 2024-05-20
- 新增：ActionSystem
- 优化：完善已有逻辑
- 优化：修改外部访问System的方式
- 修复：稳定性修复

## [0.0.1-preview] - 2024-04-23
- 新增：LiteQuark初版，主要包含Asset