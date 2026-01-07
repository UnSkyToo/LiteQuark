# UISystem UI管理系统 (Addon)

UISystem 是 LiteQuark.Addons 的UI管理模块，提供了完整的UI生命周期管理和层级管理功能。

## 功能特性

### ✅ 核心功能
- **UI生命周期管理**：OnOpen / OnClose / OnUpdate
- **Canvas层级管理**：自动管理UI层级深度
- **Mutex UI**：独占模式，同时只能打开一个
- **安全区适配**：自动适配刘海屏
- **事件自动清理**：销毁时自动注销事件监听器
- **Sprite资源管理**：统一加载和卸载

### ✅ 设计特点
- **状态管理**：Opening → Opened → Closing → Closed
- **资源自动释放**：关闭UI时自动卸载资源
- **类型安全**：泛型支持，编译时类型检查

---

## 快速开始

### 1. 创建UI类

```csharp
using LiteQuark.Runtime;

public class ShopUI : BaseUI
{
    public override string DebugName => "ShopUI";

    protected override void OnOpen(params object[] args)
    {
        // UI打开时调用
        Debug.Log("商店打开");

        // 接收传入参数
        if (args.Length > 0)
        {
            int coinCount = (int)args[0];
            UpdateCoinDisplay(coinCount);
        }

        // 注册事件（自动清理）
        RegisterEvent<CoinChangedEvent>(OnCoinChanged);
    }

    protected override void OnClose()
    {
        // UI关闭时调用
        Debug.Log("商店关闭");
        // 事件会自动注销，无需手动处理
    }

    protected override void OnUpdate(float deltaTime)
    {
        // 每帧更新（可选）
    }

    private void OnCoinChanged(CoinChangedEvent evt)
    {
        UpdateCoinDisplay(evt.TotalCoins);
    }
}
```

### 2. 打开/关闭UI

```csharp
// 打开UI
var config = new UIConfig("UI/ShopPanel", UIDepthMode.Normal, isMutex: false);
var shopUI = await LiteRuntime.Get<UISystem>().OpenUI<ShopUI>(config, coinCount);

// 关闭UI
shopUI.Close();
```

---

## API 文档

### OpenUI<T>
```csharp
public async UniTask<T> OpenUI<T>(UIConfig config, params object[] args) where T : BaseUI
```

打开UI。

**参数：**
- `config` - UI配置
- `args` - 传递给OnOpen的参数

**示例：**
```csharp
var config = new UIConfig("UI/SettingsPanel", UIDepthMode.Top);
var settingsUI = await LiteRuntime.Get<UISystem>().OpenUI<SettingsUI>(config);
```

### UIConfig
```csharp
public class UIConfig
{
    public string Path;              // UI预制体路径
    public UIDepthMode DepthMode;    // 层级模式
    public bool IsMutex;             // 是否独占
    public bool EnableSafeArea;      // 是否启用安全区
}
```

**UIDepthMode：**
- `Bottom` - 底层（背景）
- `Normal` - 普通层（主UI）
- `Top` - 顶层（弹窗）
- `System` - 系统层（加载界面、提示）

---

## 使用场景

### 1. 主菜单UI

```csharp
public class MainMenuUI : BaseUI
{
    public override string DebugName => "MainMenu";

    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;

    protected override void OnOpen(params object[] args)
    {
        playButton.onClick.AddListener(OnPlayClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
    }

    protected override void OnClose()
    {
        playButton.onClick.RemoveListener(OnPlayClicked);
        settingsButton.onClick.RemoveListener(OnSettingsClicked);
    }

    void OnPlayClicked()
    {
        // 开始游戏
        StartGame();
    }

    async void OnSettingsClicked()
    {
        // 打开设置UI
        var config = new UIConfig("UI/SettingsPanel", UIDepthMode.Top);
        await LiteRuntime.Get<UISystem>().OpenUI<SettingsUI>(config);
    }
}
```

### 2. 独占弹窗（Mutex UI）

```csharp
public class DialogUI : BaseUI
{
    public override string DebugName => "Dialog";

    protected override void OnOpen(params object[] args)
    {
        string title = (string)args[0];
        string message = (string)args[1];

        titleText.text = title;
        messageText.text = message;
    }
}

// 使用（同时只能打开一个）
public static async void ShowDialog(string title, string message)
{
    var config = new UIConfig("UI/DialogPanel", UIDepthMode.Top, isMutex: true);
    await LiteRuntime.Get<UISystem>().OpenUI<DialogUI>(config, title, message);
}
```

### 3. 安全区适配

```csharp
// 启用安全区适配（自动适配刘海屏）
var config = new UIConfig("UI/GameHUD", UIDepthMode.Normal)
{
    EnableSafeArea = true
};
await LiteRuntime.Get<UISystem>().OpenUI<GameHUD>(config);
```

---

## BaseUI方法

### RegisterEvent<T>
```csharp
protected void RegisterEvent<T>(Action<T> callback) where T : IEventData
```

注册事件监听器（自动清理）。

**示例：**
```csharp
protected override void OnOpen(params object[] args)
{
    RegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp);
    RegisterEvent<CoinChangedEvent>(OnCoinChanged);
    // OnClose时自动注销
}
```

### LoadSpriteAsync
```csharp
protected async UniTask<Sprite> LoadSpriteAsync(string path)
```

加载Sprite资源（统一管理）。

**示例：**
```csharp
var iconSprite = await LoadSpriteAsync("UI/Icons/sword_icon");
iconImage.sprite = iconSprite;
// UI关闭时自动卸载
```

---

## 最佳实践

### 1. UI层级规划

```csharp
// 背景UI
var bg = new UIConfig("UI/Background", UIDepthMode.Bottom);

// 主UI
var main = new UIConfig("UI/MainPanel", UIDepthMode.Normal);

// 弹窗
var popup = new UIConfig("UI/RewardPopup", UIDepthMode.Top);

// 加载界面
var loading = new UIConfig("UI/LoadingScreen", UIDepthMode.System);
```

### 2. 使用Mutex避免重复打开

```csharp
// 设置界面（独占）
var config = new UIConfig("UI/Settings", UIDepthMode.Top, isMutex: true);
// 多次调用只会打开一个实例
```

### 3. 参数传递

```csharp
protected override void OnOpen(params object[] args)
{
    if (args.Length > 0)
    {
        var itemId = (int)args[0];
        var count = (int)args[1];
        ShowItem(itemId, count);
    }
}
```

---

## 许可证

MIT License - 与 LiteQuark 框架保持一致
