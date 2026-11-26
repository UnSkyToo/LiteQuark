# Changelog

## **[Unreleased]**

计划版本：`0.5.0`。从 `0.4.3` 升级：最低 Unity 版本为 **2022.3.62f3**。自定义系统随核心包改用 `Initialize(CancellationToken ct = default)` 和 `UniTask DisposeAsync()`。

- **包分发**：依赖 Core `0.5.0` 和 uGUI `1.0.0`，包内附 [MIT 许可证](LICENSE.md)及[第三方许可声明](Third%20Party%20Notices.md)。
- **[配置表](Runtime/Config/README.md)**：新增 ConfigSystem 和 Excel 工具，支持从 Excel 导出 JSON 和 C#，运行时支持自定义主键。
- **[UI](Runtime/UI/README.md)**：系统创建跨场景 Canvas，原先依赖场景根节点的代码改用 `CanvasRoot`。`OpenUI` 支持取消，系统关闭时在途打开会抛 `OperationCanceledException`；界面支持 `ILateTick`。
- **[Data](Runtime/Data/README.md)**：关闭会等待已受理的异步读写和 Provider 切换。自定义 Provider 的 `Initialize` 增加 `CancellationToken ct = default`。
- **[Network](Runtime/Network/README.md)**：关闭发出取消后立即返回，必须取得结果的请求应在关闭前等待。被取消的普通请求和上传返回失败响应，下载抛取消异常。
- **[Audio](Runtime/Audio/README.md)**：音频使用独立的对象池，清理共享池后仍可播放。

## **[0.4.3] - 2026-03-03**
- 修复：WebGL 上部分音效因状态问题无法播放。

## **[0.4.2] - 2026-02-28**
- 修复：Core 依赖版本不正确。

## **[0.4.1] - 2026-02-26**
- 变更：`BaseUI.FindComponent` 更名为 `GetComponent`，组件查找移至 `UnityUtils`。
- 优化：AudioObject、EffectObject 和 BaseUI 使用 `SafeInvoke` 隔离回调异常。

## **[0.4.0] - 2026-02-25**
- 修复：音频加载失败未释放载体、编辑器 Bundle 音频播放错误，以及加载中的特效停止后未回收实例。
- 修复：`SendBatch` 在非主线程使用 UnityWebRequest 报错。

## **[0.3.0] - 2026-02-09**
- 变更：系统设置移至各自的包。

## **[0.2.1] - 2026-01-07**
- 新增：音效获取和音量设置接口。

## **[0.2.0] - 2025-10-09**
- 新增：引入 UniTask。
- 更新：随 Core 升级。

## **[0.1.0-preview] - 2025-02-25**
- 新增：Addons 首次从 LiteQuark 主体独立发布。
