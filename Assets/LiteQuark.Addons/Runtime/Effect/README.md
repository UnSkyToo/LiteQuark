# EffectSystem 特效系统

EffectSystem 是 LiteQuark 框架的特效管理模块，提供了统一的特效播放、管理和回收机制，支持粒子系统、动画和拖尾等多种特效类型。

## 功能特性

### ✅ 核心功能
- **统一管理**：集中管理所有特效的播放和生命周期
- **对象池集成**：自动使用对象池复用特效对象，减少GC
- **多类型支持**：支持 ParticleSystem、Animator、TrailRenderer
- **精确控制**：支持播放、停止、速度控制、时间控制
- **自动回收**：特效播放完成后自动回收到对象池
- **坐标空间**：支持本地坐标和世界坐标

### ✅ 设计特点
- **简单易用**：一行代码播放特效
- **自动缓存**：通过 EffectBinder 自动缓存特效信息
- **性能优化**：基于对象池，避免频繁 Instantiate/Destroy
- **灵活配置**：支持循环播放、生命周期、完成回调

---

## 快速开始

### 1. 播放特效

```csharp
// 最简单的用法：在指定位置播放特效
var info = new EffectCreateInfo(
    parent: transform,
    path: "effects/explosion",
    space: EffectSpace.Local,
    position: Vector3.zero,
    scale: 1f,
    rotation: Quaternion.identity
);

ulong effectId = LiteRuntime.Effect.PlayEffect(info);
```

### 2. 停止特效

```csharp
// 停止特效（会自动回收）
LiteRuntime.Effect.StopEffect(effectId);
```

### 3. 查找特效

```csharp
// 根据 ID 查找特效对象
var effect = LiteRuntime.Effect.FindEffect(effectId) as EffectObject;
if (effect != null && effect.IsValid)
{
    // 操作特效...
}
```

---

## API 文档

### PlayEffect

```csharp
public ulong PlayEffect(EffectCreateInfo info)
```

播放特效，返回特效唯一 ID。

**参数：**
- `info` - 特效创建信息（EffectCreateInfo）

**返回值：**
- 特效唯一 ID，如果播放失败返回 0

**示例：**
```csharp
var info = new EffectCreateInfo(
    parent: transform,
    path: "effects/explosion",
    space: EffectSpace.Local,
    position: Vector3.zero,
    scale: 1f,
    rotation: Quaternion.identity,
    speed: 1f,
    isLoop: false,
    lifeTime: 2f
);

ulong effectId = LiteRuntime.Effect.PlayEffect(info);
```

### StopEffect

```csharp
public void StopEffect(ulong id)
```

停止特效播放。特效会进入 Finishing 状态，等待 RetainTime 后自动回收。

**参数：**
- `id` - 特效唯一 ID

**示例：**
```csharp
LiteRuntime.Effect.StopEffect(effectId);
```

### FindEffect

```csharp
public BaseObject FindEffect(ulong id)
```

根据 ID 查找特效对象。

**参数：**
- `id` - 特效唯一 ID

**返回值：**
- 特效对象（需转换为 EffectObject），如果未找到返回 null

**示例：**
```csharp
var effect = LiteRuntime.Effect.FindEffect(effectId) as EffectObject;
if (effect != null && effect.IsValid)
{
    effect.SetSpeed(2f);
}
```

---

## EffectCreateInfo 参数说明

### 构造函数

```csharp
public EffectCreateInfo(
    Transform parent,
    string path,
    EffectSpace space,
    Vector3 position,
    float scale,
    Quaternion rotation,
    float speed = 1f,
    bool isLoop = false,
    float lifeTime = 0f,
    int order = -1,
    string layerName = ""
)
```

**参数说明：**

| 参数 | 类型 | 说明 |
|-----|------|-----|
| parent | Transform | 父节点（null 表示世界坐标） |
| path | string | 特效预制体路径 |
| space | EffectSpace | 坐标空间（Local/World） |
| position | Vector3 | 位置 |
| scale | float | 缩放 |
| rotation | Quaternion | 旋转 |
| speed | float | 播放速度（默认 1.0） |
| isLoop | bool | 是否循环播放（默认 false） |
| lifeTime | float | 生命周期（0 表示自动检测） |
| order | int | 渲染排序（-1 表示不设置） |
| layerName | string | 渲染层名称 |

**示例：**
```csharp
// 基础特效
var info = new EffectCreateInfo(
    parent: transform,
    path: "effects/hit",
    space: EffectSpace.Local,
    position: Vector3.up,
    scale: 1f,
    rotation: Quaternion.identity
);

// 循环特效
var loopInfo = new EffectCreateInfo(
    parent: transform,
    path: "effects/aura",
    space: EffectSpace.Local,
    position: Vector3.zero,
    scale: 1.5f,
    rotation: Quaternion.identity,
    speed: 1f,
    isLoop: true
);

// 带完成回调
info.SetCompleteCallback(() =>
{
    Debug.Log("特效播放完成！");
});

// 设置渲染排序
info.SetOrder(100);
```

---

## EffectSpace 坐标空间

```csharp
[Flags]
public enum EffectSpace
{
    Auto = 0,                    // 自动
    LocalPosition = 1 << 0,      // 本地位置
    LocalRotation = 1 << 1,      // 本地旋转
    WorldPosition = 1 << 2,      // 世界位置
    WorldRotation = 1 << 3,      // 世界旋转
    Local = LocalPosition | LocalRotation,   // 本地坐标
    World = WorldPosition | WorldRotation,   // 世界坐标
}
```

**示例：**
```csharp
// 使用本地坐标（跟随父节点）
var info = new EffectCreateInfo(
    parent: transform,
    path: "effects/trail",
    space: EffectSpace.Local,
    position: Vector3.zero,
    scale: 1f,
    rotation: Quaternion.identity
);

// 使用世界坐标（不跟随父节点）
var worldInfo = new EffectCreateInfo(
    parent: transform,
    path: "effects/explosion",
    space: EffectSpace.World,
    position: worldPosition,
    scale: 1f,
    rotation: Quaternion.identity
);
```

---

## EffectObject 控制接口

### 基础属性

```csharp
// 是否循环
bool IsLoop { get; }

// 生命周期
float LifeTime { get; }

// 是否有效（正在播放或暂停）
bool IsValid { get; }

// 是否结束（可以回收）
bool IsEnd { get; }
```

### 播放控制

```csharp
// 播放特效（指定速度）
void Play(float speed)

// 播放指定动画
void PlayAnimation(string stateName, int layer, float speed)

// 停止特效
void Stop()
```

### 速度控制

```csharp
// 设置播放速度
void SetSpeed(float speed)

// 恢复上一次的速度
void ResetSpeed()
```

### 时间控制

```csharp
// 设置播放时间
void SetTime(float time)
```

**示例：**
```csharp
var effect = LiteRuntime.Effect.FindEffect(effectId) as EffectObject;

// 加速播放
effect.SetSpeed(2f);

// 跳转到指定时间
effect.SetTime(0.5f);

// 播放指定动画
effect.PlayAnimation("Attack", 0, 1f);
```

---

## EffectBinder 绑定器

EffectBinder 是挂载在特效预制体上的组件，用于缓存特效信息和控制特效播放。

### 自动缓存

系统会自动为特效对象添加 EffectBinder 并缓存以下信息：
- ParticleSystem[] - 粒子系统数组
- Animator[] - 动画控制器数组
- TrailRenderer[] - 拖尾渲染器数组
- IsLoop - 是否循环播放
- LifeTime - 生命周期
- RetainTime - 停止后保留时间

### 手动配置

也可以手动在预制体上添加 EffectBinder 组件并配置参数：

```csharp
// 在 Editor 中配置 EffectBinder
public class EffectBinder : MonoBehaviour
{
    public bool IsLoop;         // 是否循环
    public float LifeTime;      // 生命周期
    public float RetainTime;    // 停止后保留时间
}
```

---

## 使用场景

### 1. 基础特效播放

```csharp
public class WeaponEffect : MonoBehaviour
{
    public void PlayHitEffect(Vector3 position)
    {
        var info = new EffectCreateInfo(
            parent: null,  // 世界坐标
            path: "effects/hit",
            space: EffectSpace.World,
            position: position,
            scale: 1f,
            rotation: Quaternion.identity
        );

        LiteRuntime.Effect.PlayEffect(info);
        // 特效会自动播放完成后回收，无需手动管理
    }
}
```

### 2. 跟随角色的特效

```csharp
public class CharacterEffect : MonoBehaviour
{
    private ulong auraEffectId;

    void Start()
    {
        // 播放循环光环特效（跟随角色）
        var info = new EffectCreateInfo(
            parent: transform,
            path: "effects/aura",
            space: EffectSpace.Local,
            position: Vector3.zero,
            scale: 1f,
            rotation: Quaternion.identity,
            isLoop: true
        );

        auraEffectId = LiteRuntime.Effect.PlayEffect(info);
    }

    void OnDestroy()
    {
        // 角色销毁时停止特效
        if (auraEffectId != 0)
        {
            LiteRuntime.Effect.StopEffect(auraEffectId);
        }
    }
}
```

### 3. 技能特效系统

```csharp
public class SkillEffect : MonoBehaviour
{
    private Dictionary<string, ulong> activeEffects = new();

    // 播放技能特效
    public void PlaySkillEffect(string skillName, Vector3 position)
    {
        // 停止旧特效
        if (activeEffects.TryGetValue(skillName, out var oldId))
        {
            LiteRuntime.Effect.StopEffect(oldId);
        }

        // 播放新特效
        var info = new EffectCreateInfo(
            parent: transform,
            path: $"effects/skill/{skillName}",
            space: EffectSpace.Local,
            position: position,
            scale: 1f,
            rotation: Quaternion.identity,
            speed: 1f
        );

        // 设置完成回调
        info.SetCompleteCallback(() =>
        {
            activeEffects.Remove(skillName);
            OnSkillEffectComplete(skillName);
        });

        ulong effectId = LiteRuntime.Effect.PlayEffect(info);
        activeEffects[skillName] = effectId;
    }

    private void OnSkillEffectComplete(string skillName)
    {
        Debug.Log($"技能特效完成: {skillName}");
    }
}
```

### 4. 特效速度控制

```csharp
public class TimeController : MonoBehaviour
{
    private ulong currentEffectId;

    public void PlaySlowMotionEffect()
    {
        var info = new EffectCreateInfo(
            parent: transform,
            path: "effects/time_slow",
            space: EffectSpace.Local,
            position: Vector3.zero,
            scale: 1f,
            rotation: Quaternion.identity
        );

        currentEffectId = LiteRuntime.Effect.PlayEffect(info);

        // 等待一帧让特效加载完成
        LiteRuntime.Task.DelayTask(0.1f, () =>
        {
            var effect = LiteRuntime.Effect.FindEffect(currentEffectId) as EffectObject;
            if (effect != null && effect.IsValid)
            {
                // 慢动作播放
                effect.SetSpeed(0.5f);
            }
        });
    }

    public void ResumeNormalSpeed()
    {
        var effect = LiteRuntime.Effect.FindEffect(currentEffectId) as EffectObject;
        if (effect != null && effect.IsValid)
        {
            // 恢复正常速度
            effect.ResetSpeed();
        }
    }
}
```

### 5. UI 特效（渲染排序）

```csharp
public class UIEffect : MonoBehaviour
{
    public void PlayUIEffect(Transform parent, int sortingOrder)
    {
        var info = new EffectCreateInfo(
            parent: parent,
            path: "effects/ui_glow",
            space: EffectSpace.Local,
            position: Vector3.zero,
            scale: 1f,
            rotation: Quaternion.identity
        );

        // 设置渲染排序（显示在 UI 前面）
        info.SetOrder(sortingOrder);
        info.SetParent(parent);

        LiteRuntime.Effect.PlayEffect(info);
    }
}
```

### 6. 批量特效管理

```csharp
public class EffectManager : MonoBehaviour
{
    private List<ulong> activeEffects = new();

    // 播放多个特效
    public void PlayMultipleEffects(Vector3[] positions, string effectPath)
    {
        foreach (var position in positions)
        {
            var info = new EffectCreateInfo(
                parent: null,
                path: effectPath,
                space: EffectSpace.World,
                position: position,
                scale: 1f,
                rotation: Quaternion.identity
            );

            ulong id = LiteRuntime.Effect.PlayEffect(info);
            activeEffects.Add(id);
        }
    }

    // 停止所有特效
    public void StopAllEffects()
    {
        foreach (var id in activeEffects)
        {
            LiteRuntime.Effect.StopEffect(id);
        }
        activeEffects.Clear();
    }

    // 清理已结束的特效
    public void CleanupFinishedEffects()
    {
        activeEffects.RemoveAll(id =>
        {
            var effect = LiteRuntime.Effect.FindEffect(id) as EffectObject;
            return effect == null || effect.IsEnd;
        });
    }
}
```

---

## 最佳实践

### 1. 使用对象池预加载

```csharp
public class EffectPreloader : MonoBehaviour
{
    async UniTask Start()
    {
        // 预加载常用特效到对象池
        await UniTask.WhenAll(
            LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("effects/explosion", 5),
            LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("effects/hit", 10),
            LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("effects/smoke", 3)
        );

        Debug.Log("特效预加载完成！");
    }
}
```

### 2. 封装特效管理类

```csharp
public static class EffectHelper
{
    // 在指定位置播放一次性特效
    public static ulong PlayOneShotEffect(string path, Vector3 position, float scale = 1f)
    {
        var info = new EffectCreateInfo(
            parent: null,
            path: path,
            space: EffectSpace.World,
            position: position,
            scale: scale,
            rotation: Quaternion.identity
        );

        return LiteRuntime.Effect.PlayEffect(info);
    }

    // 在对象上播放跟随特效
    public static ulong PlayFollowEffect(string path, Transform parent, Vector3 localPos, bool isLoop = false)
    {
        var info = new EffectCreateInfo(
            parent: parent,
            path: path,
            space: EffectSpace.Local,
            position: localPos,
            scale: 1f,
            rotation: Quaternion.identity,
            isLoop: isLoop
        );

        return LiteRuntime.Effect.PlayEffect(info);
    }

    // 播放带回调的特效
    public static ulong PlayEffectWithCallback(string path, Vector3 position, System.Action onComplete)
    {
        var info = new EffectCreateInfo(
            parent: null,
            path: path,
            space: EffectSpace.World,
            position: position,
            scale: 1f,
            rotation: Quaternion.identity
        );

        info.SetCompleteCallback(onComplete);
        return LiteRuntime.Effect.PlayEffect(info);
    }
}

// 使用
EffectHelper.PlayOneShotEffect("effects/explosion", hitPosition);
```

### 3. 预制体添加 EffectBinder

为了提高性能，建议在特效预制体上预先添加 EffectBinder 组件：

1. 在 Unity Editor 中打开特效预制体
2. 添加 EffectBinder 组件
3. 点击"Update Info"按钮自动扫描并缓存信息
4. 保存预制体

这样可以避免运行时自动扫描的开销。

### 4. 管理循环特效的生命周期

```csharp
public class BuffEffect : MonoBehaviour
{
    private ulong buffEffectId;

    public void ApplyBuff(float duration)
    {
        // 播放循环特效
        var info = new EffectCreateInfo(
            parent: transform,
            path: "effects/buff",
            space: EffectSpace.Local,
            position: Vector3.up,
            scale: 1f,
            rotation: Quaternion.identity,
            isLoop: true
        );

        buffEffectId = LiteRuntime.Effect.PlayEffect(info);

        // duration 秒后停止
        LiteRuntime.Task.DelayTask(duration, () =>
        {
            RemoveBuff();
        });
    }

    public void RemoveBuff()
    {
        if (buffEffectId != 0)
        {
            LiteRuntime.Effect.StopEffect(buffEffectId);
            buffEffectId = 0;
        }
    }
}
```

### 5. 检查特效有效性

```csharp
public class EffectController : MonoBehaviour
{
    private ulong effectId;

    public void SetEffectSpeed(float speed)
    {
        var effect = LiteRuntime.Effect.FindEffect(effectId) as EffectObject;

        // 检查特效是否有效
        if (effect != null && effect.IsValid)
        {
            effect.SetSpeed(speed);
        }
        else
        {
            Debug.LogWarning("特效无效或已结束");
        }
    }
}
```

---

## 性能建议

### 1. 预加载特效到对象池

```csharp
// ❌ 不好：运行时首次加载会有延迟
void Fire()
{
    var info = new EffectCreateInfo(transform, "effects/muzzle", EffectSpace.Local, Vector3.zero, 1f, Quaternion.identity);
    LiteRuntime.Effect.PlayEffect(info);  // 首次播放需要加载资源
}

// ✅ 好：提前预加载
async UniTask Start()
{
    await LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("effects/muzzle", 5);
}

void Fire()
{
    var info = new EffectCreateInfo(transform, "effects/muzzle", EffectSpace.Local, Vector3.zero, 1f, Quaternion.identity);
    LiteRuntime.Effect.PlayEffect(info);  // 立即播放
}
```

### 2. 复用特效 ID

```csharp
// ✅ 好：保存特效 ID 用于后续控制
private ulong trailEffectId;

void Start()
{
    var info = new EffectCreateInfo(transform, "effects/trail", EffectSpace.Local, Vector3.zero, 1f, Quaternion.identity, isLoop: true);
    trailEffectId = LiteRuntime.Effect.PlayEffect(info);
}

void SetSpeed(float speed)
{
    var effect = LiteRuntime.Effect.FindEffect(trailEffectId) as EffectObject;
    effect?.SetSpeed(speed);
}
```

### 3. 避免频繁查找

```csharp
// ❌ 不好：每帧查找
void Update()
{
    var effect = LiteRuntime.Effect.FindEffect(effectId) as EffectObject;
    effect?.SetSpeed(Time.timeScale);
}

// ✅ 好：缓存引用
private EffectObject cachedEffect;

void Start()
{
    var info = new EffectCreateInfo(transform, "effects/aura", EffectSpace.Local, Vector3.zero, 1f, Quaternion.identity, isLoop: true);
    ulong id = LiteRuntime.Effect.PlayEffect(info);

    LiteRuntime.Task.DelayTask(0.1f, () =>
    {
        cachedEffect = LiteRuntime.Effect.FindEffect(id) as EffectObject;
    });
}

void Update()
{
    if (cachedEffect != null && cachedEffect.IsValid)
    {
        cachedEffect.SetSpeed(Time.timeScale);
    }
}
```

### 4. 及时停止循环特效

```csharp
// ✅ 好：不再需要时立即停止循环特效
void OnDisable()
{
    if (loopEffectId != 0)
    {
        LiteRuntime.Effect.StopEffect(loopEffectId);
        loopEffectId = 0;
    }
}
```

---

## 常见问题

### Q: 特效播放完后需要手动回收吗？
A:
- 不需要，系统会自动回收
- 非循环特效播放完成后会自动回收
- 循环特效需要调用 StopEffect 停止，然后自动回收

### Q: 如何让特效跟随角色移动？
A:
- 将 parent 设置为角色 Transform
- space 设置为 EffectSpace.Local
- 特效会自动跟随父节点移动

### Q: 特效的生命周期是如何确定的？
A:
- 如果 EffectCreateInfo.lifeTime > 0，使用指定的生命周期
- 否则使用 EffectBinder.LifeTime（自动从粒子系统/动画检测）
- 循环特效的生命周期为 0，需要手动停止

### Q: 如何控制特效的渲染排序？
A:
- 使用 info.SetOrder(sortingOrder) 设置渲染排序
- 使用 info.SetParent 和 layerName 参数设置渲染层
- 适用于 UI 特效等需要精确控制渲染顺序的场景

### Q: 特效突然消失或闪烁怎么办？
A:
- 检查 EffectBinder 是否正确缓存了组件
- 确保特效预制体上的组件是激活状态
- 可以手动调用 EffectBinder.UpdateInfo() 重新扫描

---
