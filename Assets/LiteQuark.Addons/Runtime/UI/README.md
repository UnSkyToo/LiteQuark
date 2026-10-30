# UISystem 界面

UISystem 属于 LiteQuark.Addons。将它加入 Launcher 的 `SystemList`，并在 `SystemSettings` 配置 `UISystemSetting`。使用 `LiteRuntime.Get<UISystem>()` 访问。

需要可用的 UI Camera（配置为空时回退到 `Camera.main`）。系统使用 ScreenSpaceCamera Canvas 并设置相机为正交；需要独立相机行为时应明确配置 UI Camera。交互界面还需场景中的 EventSystem / 输入模块。

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

`UIConfig` 通过构造函数设置，只读属性不能使用对象初始化器赋值。`OpenUI` 成功返回 UI，普通加载失败或现有绑定／Open 错误处理返回 null；系统关闭中断在途打开时抛 `OperationCanceledException`。`CloseUI` 调用关闭逻辑，实例的销毁与列表移除在后续系统 Tick 中处理。只有 Opened 状态的 UI 执行 Update；同一轮更新中被其他 UI 关闭的界面也会跳过后续 Update。

## 层级与查找

| UIDepthMode | 基础排序值 |
| --- | ---: |
| Scene | 0 |
| Background | 2000 |
| Bottom | 5000 |
| Normal | 10000 |
| Top | 15000 |

非 Scene 层同层实例按创建时的子节点数量递增排序。

`FindUI<T>()` 返回已纳入活动列表的对应类型界面；`CloseUI<T>()` 关闭找到的一个实例，`CloseAllUI()` 关闭活动列表内界面。`isMutex` 复用找到的同类型界面，不会关闭其他类型；它不是并发打开锁，连续/同时打开仍需调用者协调。

## BaseUI 与生命周期

- 状态包括 Opening、Opened、Closing、Closed 和 Error。
- `RegisterEvent<T>(callback)` 使用每个 UI 实例自己的 Tag；关闭时自动注销。额外通过全局 EventSystem 注册的监听需自行清理。
- `LoadSprite(path, callback)`、`ReplaceSprite(childPath, resourcePath)` 持有 Sprite Handle，关闭时统一释放。
- `GetComponent<T>(path)`、`FindChild(path)`、`SetActive(path, value)` 操作 prefab 子节点。
- `autoAdapt` 在打开时按安全区调整锚点；不提供屏幕旋转后的持续监听。
- `OnClose()` 清理业务创建的计时器、动作和其他资源。通过 UISystem 关闭界面，不要只销毁 `Go`。

默认参考分辨率为 1920×1080；实际效果由 `UISystemSetting` 和场景中保存的值决定。实现见 [UISystem.cs](UISystem.cs)、[BaseUI.cs](BaseUI.cs) 和 [UIConfig.cs](UIConfig.cs)。

## 系统关闭

UISystem 由 Runtime 统一关闭。进入 `DisposeAsync` 后拒绝新 OpenUI（抛 `ObjectDisposedException`），同步取消已有在途 Handle，再按原打开队列、活动列表和关闭队列完成已打开界面的关闭。取消请求不会执行 Setup、Open 或正常 Close，也不会作为加载错误记录。调用方 await 时应处理 `OperationCanceledException`；无需等待的入口可用 `.Forget()` 消费任务，UniTask 默认不报告取消异常。接收成功结果可用 `.ContinueWith(ui => ...).Forget()`，取消时不会执行该回调。

`DisposeAsync` 返回 `UniTask.CompletedTask` 时，在途打开已结束、已打开界面已清理，UI 实例已调用 Destroy；Runtime 随即移除该系统，不再调度其 Tick。它不保证 Unity 延迟销毁或底层原生加载已经结束，原生加载与迟到资源由 AssetSystem 后续收尾。业务不要直接释放已注册系统或在关闭后使用旧实例。调用方自身早于 UISystem 结束的生命周期仍需由业务处理。
