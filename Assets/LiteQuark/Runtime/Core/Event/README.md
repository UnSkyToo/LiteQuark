# EventSystem 事件系统

EventSystem 是 LiteQuark 框架的核心事件通信模块，提供了类型安全、高性能的事件发布/订阅功能。

## 功能特性

### ✅ 核心功能
- **类型安全**：基于泛型的事件系统，编译时类型检查
- **全局事件模块**：开箱即用的全局事件通信
- **独立事件模块**：支持创建独立的事件空间
- **Tag标签管理**：基于Tag的作用域管理，自动批量注销
- **自动清理**：Editor模式下检测未注销的监听器

### ✅ 设计特点
- **零GC**：避免装箱，性能优异
- **解耦**：模块间通过事件通信，降低耦合
- **易用**：简洁的API，一行代码发送/接收事件

---

## 快速开始

### 1. 定义事件

```csharp
using LiteQuark.Runtime;

// 定义事件数据结构
public class PlayerLevelUpEvent : IEventData
{
    public int PlayerId;
    public int NewLevel;
    public int OldLevel;
}

public class CoinChangedEvent : IEventData
{
    public int Amount;
    public int TotalCoins;
}
```

### 2. 发送事件

```csharp
// 方式1：发送事件实例
var evt = new PlayerLevelUpEvent
{
    PlayerId = 1,
    NewLevel = 10,
    OldLevel = 9
};
LiteRuntime.Event.Send(evt);

// 方式2：发送无参数事件（自动创建实例）
LiteRuntime.Event.Send<GameStartEvent>();
```

### 3. 接收事件

```csharp
public class PlayerUI : MonoBehaviour
{
    void Start()
    {
        // 注册事件监听
        LiteRuntime.Event.Register<PlayerLevelUpEvent>(OnPlayerLevelUp);
    }

    void OnDestroy()
    {
        // 注销事件监听（重要！）
        LiteRuntime.Event.UnRegister<PlayerLevelUpEvent>(OnPlayerLevelUp);
    }

    private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
    {
        Debug.Log($"玩家升级到{evt.NewLevel}级");
        // 更新UI
    }
}
```

---

## API 文档

### 发送事件

#### Send<T>(T msg)
```csharp
public void Send<T>(T msg) where T : IEventData
```
发送事件实例。

**参数：**
- `msg` - 事件数据对象

**示例：**
```csharp
var evt = new CoinChangedEvent { Amount = 100, TotalCoins = 1000 };
LiteRuntime.Event.Send(evt);
```

#### Send<T>()
```csharp
public void Send<T>() where T : IEventData, new()
```
发送无参数事件（自动创建实例）。

**示例：**
```csharp
LiteRuntime.Event.Send<GameStartEvent>();
LiteRuntime.Event.Send<GamePausedEvent>();
```

---

### 注册监听

#### Register<T>(Action<T> callback)
```csharp
public void Register<T>(Action<T> callback) where T : IEventData
```
注册全局事件监听器。

**示例：**
```csharp
LiteRuntime.Event.Register<PlayerLevelUpEvent>(OnPlayerLevelUp);

private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
{
    Debug.Log($"等级：{evt.NewLevel}");
}
```

#### Register<T>(int tag, Action<T> callback)
```csharp
public void Register<T>(int tag, Action<T> callback) where T : IEventData
```
注册带Tag标签的事件监听器。

**用途：** 便于批量注销同一作用域的所有监听器。

**示例：**
```csharp
int uiTag = GetHashCode(); // 使用对象哈希作为Tag

// 注册多个事件，都使用相同的Tag
LiteRuntime.Event.Register(uiTag, OnPlayerLevelUp);
LiteRuntime.Event.Register(uiTag, OnCoinChanged);
LiteRuntime.Event.Register(uiTag, OnItemReceived);

// 销毁时一次性注销所有
void OnDestroy()
{
    LiteRuntime.Event.UnRegisterAll(uiTag);
}
```

---

### 注销监听

#### UnRegister<T>(Action<T> callback)
```csharp
public void UnRegister<T>(Action<T> callback) where T : IEventData
```
注销全局事件监听器。

**示例：**
```csharp
LiteRuntime.Event.UnRegister<PlayerLevelUpEvent>(OnPlayerLevelUp);
```

#### UnRegister<T>(int tag, Action<T> callback)
```csharp
public void UnRegister<T>(int tag, Action<T> callback) where T : IEventData
```
注销指定Tag的事件监听器。

#### UnRegisterAll(int tag)
```csharp
public void UnRegisterAll(int tag)
```
注销指定Tag的所有事件监听器。

**示例：**
```csharp
int uiTag = GetHashCode();
LiteRuntime.Event.UnRegisterAll(uiTag); // 清理所有UI相关监听器
```

---

### 独立事件模块

#### CreateIndependentModule(string name)
```csharp
public EventModule CreateIndependentModule(string name)
```
创建独立的事件模块。

**用途：**
- 隔离不同系统的事件空间
- 避免事件命名冲突
- 精确控制事件生命周期

**示例：**
```csharp
public class BattleSystem
{
    private EventModule battleEvents;

    public void Initialize()
    {
        // 创建战斗系统专用事件模块
        battleEvents = LiteRuntime.Event.CreateIndependentModule("Battle");
    }

    public void StartBattle()
    {
        // 在独立模块中发送事件
        battleEvents.Send<BattleStartEvent>();
    }

    public void Dispose()
    {
        // 清理模块（自动注销所有监听器）
        battleEvents.Dispose();
    }
}
```

---

## 使用场景

### 1. UI与逻辑解耦

```csharp
// GameLogic.cs - 游戏逻辑
public class GameLogic
{
    public void PlayerCollectCoin(int amount)
    {
        playerCoins += amount;

        // 通知UI更新（无需持有UI引用）
        LiteRuntime.Event.Send(new CoinChangedEvent
        {
            Amount = amount,
            TotalCoins = playerCoins
        });
    }
}

// CoinUI.cs - UI显示
public class CoinUI : MonoBehaviour
{
    void Start()
    {
        LiteRuntime.Event.Register<CoinChangedEvent>(OnCoinChanged);
    }

    void OnDestroy()
    {
        LiteRuntime.Event.UnRegister<CoinChangedEvent>(OnCoinChanged);
    }

    private void OnCoinChanged(CoinChangedEvent evt)
    {
        coinText.text = evt.TotalCoins.ToString();
        PlayCoinAnimation();
    }
}
```

### 2. 系统间通信

```csharp
// 定义事件
public class EnemyKilledEvent : IEventData
{
    public int EnemyId;
    public Vector3 Position;
    public int RewardExp;
}

// BattleSystem发送事件
public class BattleSystem
{
    void OnEnemyKilled(Enemy enemy)
    {
        LiteRuntime.Event.Send(new EnemyKilledEvent
        {
            EnemyId = enemy.Id,
            Position = enemy.transform.position,
            RewardExp = enemy.ExpReward
        });
    }
}

// PlayerSystem接收事件
public class PlayerSystem
{
    void Initialize()
    {
        LiteRuntime.Event.Register<EnemyKilledEvent>(OnEnemyKilled);
    }

    private void OnEnemyKilled(EnemyKilledEvent evt)
    {
        AddExp(evt.RewardExp);
        CheckLevelUp();
    }
}

// EffectSystem接收事件
public class EffectSystem
{
    void Initialize()
    {
        LiteRuntime.Event.Register<EnemyKilledEvent>(OnEnemyKilled);
    }

    private void OnEnemyKilled(EnemyKilledEvent evt)
    {
        // 在敌人位置播放死亡特效
        PlayDeathEffect(evt.Position);
    }
}
```

### 3. Tag标签管理

```csharp
public class BaseUI : MonoBehaviour
{
    private int _eventTag;

    protected virtual void Awake()
    {
        _eventTag = GetHashCode();
    }

    // 子类使用Tag注册事件
    protected void RegisterEvent<T>(Action<T> callback) where T : IEventData
    {
        LiteRuntime.Event.Register(_eventTag, callback);
    }

    protected virtual void OnDestroy()
    {
        // 自动清理所有事件监听器
        LiteRuntime.Event.UnRegisterAll(_eventTag);
    }
}

// 使用示例
public class ShopUI : BaseUI
{
    protected override void Awake()
    {
        base.Awake();

        // 注册多个事件（都会在OnDestroy时自动清理）
        RegisterEvent<CoinChangedEvent>(OnCoinChanged);
        RegisterEvent<ItemPurchasedEvent>(OnItemPurchased);
        RegisterEvent<ShopRefreshedEvent>(OnShopRefreshed);
    }

    private void OnCoinChanged(CoinChangedEvent evt) { /* ... */ }
    private void OnItemPurchased(ItemPurchasedEvent evt) { /* ... */ }
    private void OnShopRefreshed(ShopRefreshedEvent evt) { /* ... */ }
}
```

### 4. 独立事件模块

```csharp
public class MiniGameManager
{
    private EventModule miniGameEvents;

    public void Initialize()
    {
        // 创建小游戏独立事件空间
        miniGameEvents = LiteRuntime.Event.CreateIndependentModule("MiniGame");

        // 在独立模块中注册事件
        miniGameEvents.Register<MiniGameStartEvent>(OnMiniGameStart);
        miniGameEvents.Register<MiniGameEndEvent>(OnMiniGameEnd);
    }

    public void StartMiniGame()
    {
        // 只在小游戏模块中广播
        miniGameEvents.Send<MiniGameStartEvent>();
    }

    public void Dispose()
    {
        // 清理模块（不影响全局事件）
        miniGameEvents.Dispose();
    }
}
```

---

## 内置事件

框架提供了一些内置事件：

### EnterForegroundEvent
```csharp
public class EnterForegroundEvent : IEventData { }
```
应用进入前台时触发。

**示例：**
```csharp
LiteRuntime.Event.Register<EnterForegroundEvent>(evt =>
{
    Debug.Log("应用进入前台");
    // 恢复游戏、同步数据等
});
```

### EnterBackgroundEvent
```csharp
public class EnterBackgroundEvent : IEventData { }
```
应用进入后台时触发。

**示例：**
```csharp
LiteRuntime.Event.Register<EnterBackgroundEvent>(evt =>
{
    Debug.Log("应用进入后台");
    // 保存数据、暂停游戏等
});
```

---

## 最佳实践

### 1. 事件命名规范

```csharp
// 好的命名：动词 + 名词 + Event
public class PlayerLevelUpEvent : IEventData { }
public class ItemPurchasedEvent : IEventData { }
public class EnemySpawnedEvent : IEventData { }

// 避免：太模糊
public class DataEvent : IEventData { }    // ❌ 太模糊
public class UpdateEvent : IEventData { }  // ❌ 不具体
```

### 2. 事件数据设计

```csharp
// 好的设计：包含足够的上下文信息
public class ItemDroppedEvent : IEventData
{
    public int ItemId;
    public string ItemName;
    public Vector3 Position;
    public int Quantity;
    public int SourceEnemyId; // 从哪个敌人掉落
}

// 避免：信息不足
public class ItemDroppedEvent : IEventData
{
    public int ItemId; // ❌ 信息太少
}
```

### 3. 始终注销监听器

```csharp
// ❌ 错误：忘记注销
public class BadExample : MonoBehaviour
{
    void Start()
    {
        LiteRuntime.Event.Register<PlayerLevelUpEvent>(OnLevelUp);
    }
    // 忘记在OnDestroy中注销 -> 内存泄漏！
}

// ✅ 正确：使用Tag自动管理
public class GoodExample : MonoBehaviour
{
    private int _tag;

    void Awake()
    {
        _tag = GetHashCode();
    }

    void Start()
    {
        LiteRuntime.Event.Register(_tag, OnLevelUp);
    }

    void OnDestroy()
    {
        LiteRuntime.Event.UnRegisterAll(_tag);
    }

    private void OnLevelUp(PlayerLevelUpEvent evt) { }
}
```

### 4. 避免事件链

```csharp
// ❌ 避免：事件触发事件（可能导致死循环）
private void OnEventA(EventA evt)
{
    LiteRuntime.Event.Send<EventB>(); // EventB处理器又发送EventA -> 死循环
}

// ✅ 建议：使用直接调用或状态管理
```

### 5. 合理使用独立模块

```csharp
// 适合使用独立模块的场景：
// 1. 功能模块化（战斗系统、UI系统等）
// 2. 需要独立生命周期管理
// 3. 避免事件命名冲突

// 战斗系统
var battleEvents = LiteRuntime.Event.CreateIndependentModule("Battle");
battleEvents.Register<BattleStartEvent>(OnBattleStart);

// UI系统
var uiEvents = LiteRuntime.Event.CreateIndependentModule("UI");
uiEvents.Register<UIOpenEvent>(OnUIOpen);
```

---

## 性能建议

### 1. 避免频繁发送事件

```csharp
// ❌ 不好：每帧发送事件
void Update()
{
    LiteRuntime.Event.Send(new PlayerMoveEvent { Position = transform.position });
}

// ✅ 更好：只在必要时发送
void OnPlayerReachedWaypoint(Vector3 waypoint)
{
    LiteRuntime.Event.Send(new PlayerReachedWaypointEvent { Position = waypoint });
}
```

### 2. 复用事件对象（可选优化）

```csharp
public class CoinSystem
{
    private CoinChangedEvent _cachedEvent = new CoinChangedEvent();

    public void AddCoins(int amount)
    {
        totalCoins += amount;

        // 复用事件对象减少GC
        _cachedEvent.Amount = amount;
        _cachedEvent.TotalCoins = totalCoins;
        LiteRuntime.Event.Send(_cachedEvent);
    }
}
```

### 3. 使用Tag批量管理

```csharp
// ✅ 推荐：使用Tag，OnDestroy时一次性清理
int tag = GetHashCode();
LiteRuntime.Event.Register(tag, OnEventA);
LiteRuntime.Event.Register(tag, OnEventB);
LiteRuntime.Event.Register(tag, OnEventC);
// ...
LiteRuntime.Event.UnRegisterAll(tag); // 一次清理所有
```

---

## 调试技巧

### 1. Editor模式检测

在Unity Editor中，Dispose时会自动检测未注销的监听器：

```csharp
// Console输出示例：
// Warning: 1234-PlayerUI : OnPlayerLevelUp UnRegister
// 说明PlayerUI的OnPlayerLevelUp方法忘记注销
```

### 2. 自定义日志

```csharp
#if UNITY_EDITOR
public class DebugEvents : MonoBehaviour
{
    void Start()
    {
        // 监听所有玩家相关事件（调试用）
        LiteRuntime.Event.Register<PlayerLevelUpEvent>(evt =>
            Debug.Log($"[Event] PlayerLevelUp: {evt.NewLevel}"));

        LiteRuntime.Event.Register<CoinChangedEvent>(evt =>
            Debug.Log($"[Event] CoinChanged: {evt.Amount}"));
    }
}
#endif
```

---

## 常见问题

### Q: 事件监听器的执行顺序是什么？
**A:** 按照注册顺序执行，但不应依赖执行顺序。如果需要顺序，考虑使用直接调用或状态机。

### Q: 如何在Lambda中注册和注销事件？
**A:**
```csharp
// ❌ 错误：无法注销Lambda
LiteRuntime.Event.Register<MyEvent>(evt => Debug.Log("Hello"));

// ✅ 正确：使用命名方法
LiteRuntime.Event.Register<MyEvent>(OnMyEvent);

private void OnMyEvent(MyEvent evt)
{
    Debug.Log("Hello");
}

// 或者保存Lambda引用
Action<MyEvent> handler = evt => Debug.Log("Hello");
LiteRuntime.Event.Register(handler);
// ...
LiteRuntime.Event.UnRegister(handler);
```

### Q: 事件可以跨场景传递吗？
**A:** 可以，EventSystem是单例，在整个游戏生命周期中存在。但要注意在场景销毁时注销监听器。

### Q: 如何避免内存泄漏？
**A:**
1. 始终在OnDestroy中注销监听器
2. 使用Tag批量管理
3. 使用独立模块，销毁时自动清理
4. 在Editor模式下检查Console警告

---
