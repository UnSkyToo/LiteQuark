# Changelog

## **[Unreleased]**
- 破坏：EffectSystem 关闭开始后 `PlayEffect` 抛 `ObjectDisposedException`，调用方不得继续使用旧实例；在途特效等待模板就绪后不再借出实例，共享模板和原生加载收尾仍由 ObjectPool／Asset 管理。
- 修复：EffectObject 归还前进入终态并清空实例／Binder 引用，避免释放重入导致重复归还，以及旧引用 Stop 后操作已复用实例；外部提前 Dispose 的特效也会在 Tick 中移出列表。正常完成回调和 RetainTime 时序不变。
- 修复：AudioSystem 独立持有承载对象池，不再复用 ObjectPoolSystem 的同名共享池，避免业务调用 `RemoveUnusedPools()` 后音频永久无法播放；音频关闭只释放自身的池。
- 破坏：AudioSystem 关闭后 `PlaySound`／`PlayMusic`／`PauseAllAudio`／`ResumeAllAudio` 抛 `ObjectDisposedException`，调用方不得继续使用旧实例；关闭同步取消 Clip Handle 并清理音频，原生加载收尾仍由 AssetSystem 承担。
- 修复：音频入口路径异常及加载结果处理异常时归还已取得的 Carrier、释放 Handle，不再因 IsLoaded 为 false 遗漏清理；关闭时恢复首次 Pause／Resume 前的全局 AudioListener.pause，从未修改则保持外部状态。
- 修复：UI 关闭只处理 Opened 状态，避免递归／重复执行 `OnClose`；最终释放时再次清理回调中新取得的框架资源，未交付请求在取消／错误时先进入 Closed／Error 终态。
- 变更：业务 `OnClose` 异常记录日志后继续关闭，不再中断 Runtime 收尾；框架资源释放异常仍向外传播，且异常时界面仍进入最终关闭队列。
- 修复：`OpenUI` 未成功交付界面时释放已取得的 Sprite Handle、Tag 监听及自动点击绑定，不调用业务 `OnClose`；正常关闭时序和失败／取消返回方式不变。
- 新增：`OpenUI<T>` 支持调用方 `CancellationToken`，取消尚未交付的请求并传播 `OperationCanceledException`，不影响其他共享加载或已交付 UI；原签名保留。LiteBattle 实体在模块释放前取消 HUD／飘字的在途打开请求。
- 破坏：UISystem 始终创建自己的 Canvas 根并跨场景保留，系统关闭时销毁，不再复用场景同名对象；依赖场景根配置或层级的接入需改用 `UISystemSetting` 和 `CanvasRoot`，跨场景使用需保证 UI Camera 常驻及 EventSystem 可用。根在创建后即纳入清理，支持部分初始化释放。
- 破坏：UISystem 关闭时同步取消在途 OpenUI，并传播 `OperationCanceledException`；关闭后的新请求抛 `ObjectDisposedException`。调用方需消费 UniTask 并单独处理关闭取消，HUD／飘字已迁移。普通加载失败和绑定／Open 的非关闭取消错误仍按原流程返回 null，现有列表与 Open／Close 顺序保持不变。
- 修复：UISystem 仅更新 Opened 界面，避免同一轮更新中已执行 Close 的界面继续 Update；实例仍在后续 Tick 通过原关闭队列释放。
- 修复：Effect 对象保存原池，模板失败或关闭取消后结束等待，避免收尾时重新创建池；失败不触发自然播放完成回调。
- 破坏：各扩展系统的 `Dispose()` 改为 `UniTask DisposeAsync()`，与核心 ISystem 接口保持一致；自定义扩展需迁移方法签名，同步清理返回 `UniTask.CompletedTask`。已注册系统仍由 Runtime 统一释放，本次不改变各扩展内部的资源清理流程，也不新增完整异步收尾保证。
- 新增：ConfigSystem 配置表模块及 Excel 配置工具，支持 JSON/C# 导出
- 优化：UI、音频与配置加载使用 Handle 管理资源生命周期
- 修复：关闭某个 UI 实例时误注销同类型其他实例的事件监听
- 文档：补充包级入口，将 Config 设计稿改为当前使用指南；模块指南说明注册、API、失败处理和生命周期

## **[0.4.3] - 2026-03-03**
- 修复：部分音效在WebGL平台因为state原因无法播放的BUG

## **[0.4.2] - 2026-02-28**
- 修复：正确的核心库依赖版本

## **[0.4.1] - 2026-02-26**
- 优化：AudioObject/EffectObject/BaseUI 回调统一使用 SafeInvoke 异常保护
- 优化：BaseUI 的 FindComponent 重命名为 GetComponent，迁移至 UnityUtils

## **[0.4.0] - 2026-02-25**
- 修复：音频加载失败时 Carrier 未释放的 BUG
- 修复：编辑器下播放Bundle音频报错的BUG
- 修复：HttpClient.SendBatch 在非主线程使用 UnityWebRequest 的报错
- 修复：EffectObject 加载中 Stop 后未回收 GameObject 的 BUG

## **[0.3.0] - 2026-02-09**
- 优化：System相关设置放到自己的Package内

## **[0.2.1] - 2026-01-07**
- 新增：音效系统增加音量设置接口
- 新增：音效系统增加音效获取接口

## **[0.2.0] - 2025-10-09**
- 更新：配合主包进行版本升级
- 新增：引入 UniTask

## **[0.1.0-preview] - 2025-02-25**
- 新增：从 LiteQuark 主体中拆分出的扩展库初版
