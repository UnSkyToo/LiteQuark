# TimerSystem 定时器系统

TimerSystem 是 LiteQuark 框架的定时器管理模块，提供了简单易用的定时回调功能。

## 功能特性

### ✅ 核心功能
- **时间定时器**：按秒/毫秒间隔触发
- **帧定时器**：按帧数间隔触发
- **重复执行**：支持单次或多次重复
- **延迟启动**：可设置延迟时间
- **永久定时器**：无限循环执行

### ✅ 设计特点
- **轻量高效**：基于Update驱动
- **易于管理**：返回唯一ID，方便停止
- **灵活配置**：支持多种定时模式

---

## 快速开始

### 1. 基础定时器

```csharp
// 1秒后执行一次
LiteRuntime.Timer.AddTimer(1.0f, () =>
{
    Debug.Log("1秒到了");
});

// 每0.5秒执行一次，共执行10次
LiteRuntime.Timer.AddTimer(0.5f, () =>
{
    Debug.Log("Tick");
}, repeatCount: 10);

// 永久定时器（无限循环）
LiteRuntime.Timer.AddTimer(1.0f, () =>
{
    Debug.Log("每秒执行");
}, TimerSystem.RepeatCountForever);
```

### 2. 帧定时器

```csharp
// 下一帧执行
LiteRuntime.Timer.NextFrame(() =>
{
    Debug.Log("下一帧");
});

// 每10帧执行一次
LiteRuntime.Timer.AddTimerWithFrame(10, () =>
{
    Debug.Log("每10帧");
}, repeatCount: 5);
```

---

## API 文档

### AddTimer
```csharp
public ulong AddTimer(float interval, Action onTick, int repeatCount = 1, float delayTime = 0f)
```

创建定时器。

**参数：**
- `interval` - 间隔时间（秒）
- `onTick` - 回调函数
- `repeatCount` - 重复次数（-1为无限）
- `delayTime` - 延迟启动时间（秒）

**返回值：** 定时器ID

**示例：**
```csharp
// 每2秒执行一次，共5次
var timerId = LiteRuntime.Timer.AddTimer(2.0f, () =>
{
    Debug.Log("Timer Tick");
}, repeatCount: 5);

// 停止定时器
LiteRuntime.Timer.StopTimer(timerId);
```

### AddTimer (带完成回调)
```csharp
public ulong AddTimer(float interval, Action onTick, Action onComplete, float totalTime)
```

**示例：**
```csharp
// 倒计时10秒
LiteRuntime.Timer.AddTimer(1.0f, () =>
{
    countdown--;
    UpdateUI(countdown);
}, () =>
{
    Debug.Log("倒计时结束！");
}, totalTime: 10.0f);
```

### AddTimerWithFrame
```csharp
public ulong AddTimerWithFrame(int frameCount, Action onTick, int repeatCount = 1, float delayTime = 0f)
```

按帧数创建定时器。

**示例：**
```csharp
// 每30帧执行一次（约0.5秒 @ 60FPS）
LiteRuntime.Timer.AddTimerWithFrame(30, () =>
{
    Debug.Log("每30帧");
}, repeatCount: TimerSystem.RepeatCountForever);
```

### NextFrame
```csharp
public ulong NextFrame(Action onTick)
```

下一帧执行。

**示例：**
```csharp
// 延迟到下一帧再初始化
LiteRuntime.Timer.NextFrame(() =>
{
    InitializeComponents();
});
```

### StopTimer
```csharp
public void StopTimer(ulong id)
```

停止定时器。

**示例：**
```csharp
ulong timerId = LiteRuntime.Timer.AddTimer(1.0f, OnTick, TimerSystem.RepeatCountForever);

// 5秒后停止
LiteRuntime.Timer.AddTimer(5.0f, () =>
{
    LiteRuntime.Timer.StopTimer(timerId);
});
```

---

## 使用场景

### 1. 倒计时

```csharp
public class CountdownTimer
{
    private int countdown = 10;
    private ulong timerId;

    public void Start()
    {
        countdown = 10;
        timerId = LiteRuntime.Timer.AddTimer(1.0f, () =>
        {
            countdown--;
            Debug.Log($"倒计时：{countdown}");

            if (countdown <= 0)
            {
                OnTimeUp();
            }
        }, repeatCount: 10);
    }

    void OnTimeUp()
    {
        Debug.Log("时间到！");
    }

    public void Stop()
    {
        LiteRuntime.Timer.StopTimer(timerId);
    }
}
```

### 2. 技能冷却

```csharp
public class Skill
{
    private bool isOnCooldown = false;
    private float cooldownTime = 5.0f;

    public void Use()
    {
        if (isOnCooldown) return;

        // 使用技能
        Debug.Log("技能释放！");

        // 进入冷却
        isOnCooldown = true;
        LiteRuntime.Timer.AddTimer(cooldownTime, () =>
        {
            isOnCooldown = false;
            Debug.Log("技能冷却完成");
        });
    }
}
```

### 3. 定时刷新

```csharp
public class EnemySpawner
{
    private ulong spawnTimerId;

    void Start()
    {
        // 每3秒刷新一个敌人
        spawnTimerId = LiteRuntime.Timer.AddTimer(3.0f, () =>
        {
            SpawnEnemy();
        }, TimerSystem.RepeatCountForever);
    }

    void OnDestroy()
    {
        LiteRuntime.Timer.StopTimer(spawnTimerId);
    }

    void SpawnEnemy()
    {
        Debug.Log("刷新敌人");
    }
}
```

### 4. 延迟执行

```csharp
public class DelayedAction
{
    public void ShowRewardAfterDelay()
    {
        // 延迟2秒后显示奖励
        LiteRuntime.Timer.AddTimer(0f, () =>
        {
            ShowRewardPanel();
        }, repeatCount: 1, delayTime: 2.0f);
    }
}
```

---

## 最佳实践

### 1. 保存定时器ID以便停止

```csharp
public class TimerManager : MonoBehaviour
{
    private ulong attackTimerId;

    void StartAttack()
    {
        attackTimerId = LiteRuntime.Timer.AddTimer(0.5f, OnAttack, TimerSystem.RepeatCountForever);
    }

    void StopAttack()
    {
        LiteRuntime.Timer.StopTimer(attackTimerId);
    }
}
```

### 2. 在OnDestroy中清理定时器

```csharp
public class Enemy : MonoBehaviour
{
    private ulong aiTimerId;

    void Start()
    {
        aiTimerId = LiteRuntime.Timer.AddTimer(1.0f, UpdateAI, TimerSystem.RepeatCountForever);
    }

    void OnDestroy()
    {
        LiteRuntime.Timer.StopTimer(aiTimerId); // 重要！
    }
}
```

### 3. 使用NextFrame避免初始化问题

```csharp
void Start()
{
    // ❌ 可能有问题：其他组件可能还未初始化
    InitializeDependencies();

    // ✅ 更安全：下一帧再初始化
    LiteRuntime.Timer.NextFrame(() =>
    {
        InitializeDependencies();
    });
}
```

---

## 性能建议

### 1. 避免创建过多定时器

```csharp
// ❌ 不好：为每个敌人创建定时器
foreach (var enemy in enemies)
{
    LiteRuntime.Timer.AddTimer(1.0f, enemy.UpdateAI, TimerSystem.RepeatCountForever);
}

// ✅ 更好：使用一个定时器管理所有敌人
LiteRuntime.Timer.AddTimer(1.0f, () =>
{
    foreach (var enemy in enemies)
    {
        enemy.UpdateAI();
    }
}, TimerSystem.RepeatCountForever);
```

### 2. 及时停止不用的定时器

```csharp
// 游戏暂停时停止定时器
void OnGamePause()
{
    LiteRuntime.Timer.StopTimer(gameTimerId);
}

// 恢复时重新创建
void OnGameResume()
{
    gameTimerId = LiteRuntime.Timer.AddTimer(1.0f, OnTick, TimerSystem.RepeatCountForever);
}
```

---

## 许可证

MIT License - 与 LiteQuark 框架保持一致
