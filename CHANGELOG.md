# Changelog

## [0.0.19-preview] - 2024-09-10
- Project Builder添加AAB对Split Binary支持
- Project Builder关联CleanModel和DebugBuild选项
- 增加宏管理接口
- 修复出包命名错误的问题

## [0.0.18-preview] - 2024-08-29
- Project Builder增加Callback回调支持（继承IBuildCallback）
- 优化打包结束后的弹窗逻辑，只有成功才弹窗
- 使用SynchronizationContext代替TaskSystem中执行主线程的方法
- 修复ListEx、DictionaryEx中同一帧Foreach中Add后Clear导致Add的数据被清掉的BUG

## [0.0.17-preview] - 2024-08-26
- Project Builder增加CreateSymbols选项
- Task新增AsyncOperation和AsyncResult类别的Task
- ObjectSystem增加UnityEngine.Pool.CollectionPool的部分封装

## [0.0.16-preview] - 2024-07-31
- Timer.NextFrame默认采用0延迟
- TaskSystem增加PipelineTask
- AudioSystem增加延迟播放参数
- 缓存池增加异步实例化接口

## [0.0.15-preview] - 2024-07-25
- 修复Window平台打包脚本报错
- 修复打包命令行参数与unity重复的BUG
- 更新出包命名，避免空格

## [0.0.14-preview] - 2024-07-19
- 修复打包Debug参数未生效的BUG

## [0.0.13-preview] - 2024-07-19
- 增加AAB支持
- 修复Action循环中无法停止其它新增Action的BUG
- 统一Timer风格和其它系统一致用ID

## [0.0.12-preview] - 2024-07-17
- 增加App Clean Build选项
- 修复Mac无法打开目录的BUG
- 完善命令行编译模式
- 修复DebugMode报空的BUG

## [0.0.11-preview] - 2024-07-17
- 增加Asset Viewer资源查看器
- 增加若干新的打包参数
- 现在运行时可增加Logic
- 增加DebugSetting

## [0.0.10-preview] - 2024-07-12
- 修复Setting Inspector界面报错BUG

## [0.0.9-preview] - 2024-07-12
- 修复还在使用Text的项目字体丢失BUG
- 增加异常抛出选项，可以关闭框架异常处理，由项目组上报
- 增加Simple开关，开启后直接使用UnityLog

## [0.0.8-preview] - 2024-07-09
- Bundle自动收集规则排除Package目录
- AssetSystem增加Retain开关
- 修复UI同一帧关闭再打开生命周期错误的问题
- 增加RectTransformMoveAction，操作anchorPosition
- BaseUI增加ReplaceSprite接口，缓存所有Load的Asset，统一释放
- 部分界面细节优化

## [0.0.7-preview] - 2024-07-02
- Bundle打包增加根目录，避免冲突
- ActionSystem新增SetParent，完成部分接口
- 打包工具界面优化，修复打包结束后的报错
- 修复GetPool部分情况下的报错

## [0.0.6-preview] - 2024-06-19
- 完善ActionSystem
- 修复MovePathAction耗时比设定多的BUG
- EventSystem拆分Tag储存，UI系统可以独立注册和反注册

## [0.0.5-preview] - 2024-05-31
- 删除NewtonJson库，内部Asset改用LitJson

## [0.0.4-preview] - 2024-05-31
- 修复资源管理器自动释放资源导致无法再次加载的BUG
- 修复音效资源泄露的问题

## [0.0.3-preview] - 2024-05-29
- 增加AudioSystem
- 增加EmptyObjectPool
- 同步最新Third/IngameDebugConsole库
- BUG修复

## [0.0.2-preview] - 2024-05-20
- 增加ActionSystem
- 完善已有逻辑
- 修改外部访问System的方式
- BUG修复

## [0.0.1-preview] - 2024-04-23
- LiteQuark初版，主要包含Asset