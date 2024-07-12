# Changelog

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