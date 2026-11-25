# UISystem 界面

UISystem 属于 LiteQuark.Addons。将它加入 Launcher 的 **System List**，并在该注册项内配置 `UISystemSetting`。使用 `LiteRuntime.Get<UISystem>()` 访问。

在注册项的 `UISystemSetting.UICamera` 配置专用 UI 相机时，系统使用 ScreenSpaceCamera Canvas 并将该相机设为正交。留空时，系统在初始化时取 `Camera.main` 作为渲染相机，保留其相机参数。场景没有主相机时，系统使用 ScreenSpaceOverlay Canvas，此时 `UISystem.UICamera` 为 null。参考分辨率与缩放参数保存在同一注册项。交互界面还需场景中的 EventSystem / 输入模块。

UISystem 创建并管理独立的 Canvas 根和子层，通过 `CanvasRoot` 访问。根节点跨场景保留，系统关闭时销毁整个 Canvas 层级。

跨场景使用时，需保证 Canvas 所用的相机常驻，并保持 EventSystem / 输入模块可用。

## 定义与打开

界面逻辑继承 `BaseUI`，不是挂载到 prefab 的 MonoBehaviour：

```csharp
using LiteQuark.Runtime;

public sealed class MenuUI : BaseUI
{
    protected override void OnOpen(params object[] paramList)
    {
        LLog.Info("Menu opened");
    }

    protected override void OnClose()
    {
    }

    protected override void OnUpdate(float deltaTime)
    {
    }
}
```

在异步方法中打开，实际路径替换为 UI prefab：

```csharp
var uiSystem = LiteRuntime.Get<UISystem>();
var config = new UIConfig("UI/Menu.prefab", UIDepthMode.Normal,
    isMutex: true, autoAdapt: true);
var menu = await uiSystem.OpenUI<MenuUI>(config);
if (menu == null)
{
    LLog.Error("Menu load failed");
    return;
}
// 需要关闭时：
uiSystem.CloseUI(menu);
```

`UIConfig` 通过构造函数设置。`OpenUI` 成功返回 UI，普通加载、绑定或 `OnOpen` 错误返回 null；请求因调用方取消或系统关闭而中断时抛 `OperationCanceledException`。`CloseUI` 仅关闭 Opened 状态的界面，重复关闭无效；关闭逻辑立即执行，实例在后续系统 Tick 中释放。只有 Opened 状态的 UI 执行 Update。

世界坐标跟随类界面可实现 `ILateTick.LateTick(float deltaTime)`。游戏入口完成晚更新后，UISystem 将同一帧的 deltaTime 传给已打开且实现该接口的窗口。本轮晚更新开始前已完成打开的窗口即可参与，开始关闭的窗口立即停止接收。普通 `OnUpdate` 和打开、关闭队列仍在原有的 Update 阶段处理。

调用方可能先于 UISystem 释放时，使用 `OpenUI<T>(config, ct, paramList)`，并在自身释放时取消对应的 `CancellationTokenSource`。系统在进入 `OnOpen` 前处理取消。如果调用方在回调中取消且 `OnOpen` 正常返回，`OpenUI` 仍交付该窗口，由调用方用 `CloseUI` 关闭。

## 层级与查找

| UIDepthMode | 基础排序值 |
| --- | ---: |
| Scene | 0 |
| Background | 2000 |
| Bottom | 5000 |
| Normal | 10000 |
| Top | 15000 |

`Scene` 层统一使用排序值 0。其他层按同层窗口的打开顺序从基础值起每次递增 100。打开窗口时分配排序，关闭队列在 Tick 释放窗口后重排同层，更新 Canvas 和 `BaseUI.SortingOrder`。

`OpenUI<T>()` 成功返回后，同帧即可通过 `FindUI<T>()` 查询、通过 `CloseUI<T>()` 关闭找到的实例，或通过 `CloseAllUI()` 关闭所有已打开的界面。

`isMutex: true` 复用已打开的同类型实例。同类型的互斥打开请求尚在进行时，后续互斥请求等待同一个结果，使用首个请求的配置和参数。首个请求因取消而终止时，共用该结果的等待均取消。后续调用方取消时，仅结束自己的等待。`isMutex: false` 每次独立打开。

## BaseUI 与生命周期

- 状态包括 Opening、Opened、Closing、Closed 和 Error。
- `RegisterEvent<T>(callback)` 使用每个 UI 实例自己的 Tag；关闭时自动注销。额外通过全局 EventSystem 注册的监听需自行清理。
- `LoadSprite(path, callback)`、`ReplaceSprite(childPath, resourcePath)` 持有 Sprite Handle，关闭时统一释放。
- `GetComponent<T>(path)`、`FindChild(path)`、`SetActive(path, value)` 操作 prefab 子节点。
- `autoAdapt` 在打开时按安全区调整锚点；不提供屏幕旋转后的持续监听。
- `OnClose()` 在正常关闭时调用一次，用于清理业务创建的计时器、动作和其他资源；回调异常会记录日志，不阻断关闭，框架资源释放异常仍向外传播。通过 UISystem 关闭界面，不要只销毁 `Go`。
- 打开失败或取消且未返回 UI 时，框架会释放已取得的资源，但不调用 `OnClose()`。

默认参考分辨率为 1920×1080；实际效果由 `UISystemSetting` 决定。实现见 [UISystem.cs](UISystem.cs)、[BaseUI.cs](BaseUI.cs) 和 [UIConfig.cs](UIConfig.cs)。

## 系统关闭

UISystem 由 Runtime 统一关闭，业务不要直接释放已注册系统或在关闭后使用旧实例。关闭时取消尚未完成的打开请求，并关闭已打开的界面。

调用方 await 时应处理 `OperationCanceledException`。无需等待结果时可用 `.Forget()` 消费任务，UniTask 默认不报告取消异常；仅处理成功结果可用 `.ContinueWith(ui => ...).Forget()`，取消时不会执行该回调。

`DisposeAsync` 完成不代表 Unity 延迟销毁或底层原生加载已经结束；原生资源收尾由 AssetSystem 负责。
