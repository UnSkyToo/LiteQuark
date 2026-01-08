# ObjectPoolSystem 对象池系统

ObjectPoolSystem 是 LiteQuark 框架的对象池管理模块，提供了高效的对象复用机制，减少GC压力。

## 功能特性

### ✅ 核心功能
- **GameObject对象池**：复用GameObject实例
- **集合对象池**：复用List、Dictionary、HashSet等集合
- **预加载机制**：支持预加载对象池，优化运行时性能
- **双接口设计**：同时支持Callback和UniTask两种调用方式
- **自动管理**：自动创建和清理对象池
- **WebGL兼容**：通过预加载完美支持WebGL平台

### ✅ 设计特点
- **向后兼容**：保留Callback方式，老代码无需修改
- **异步优化**：UniTask版本封装Callback，代码更清晰
- **性能优先**：预加载后分配对象几乎无开销
- **类型安全**：泛型支持，编译时类型检查

---

## 快速开始

### 1. 推荐用法：预加载 + UniTask

```csharp
// 场景加载时预加载对象池
await LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/explosion", 5);

// 使用时直接分配（接近同步性能）
var pool = LiteRuntime.ObjectPool.GetParticlePool("effects/explosion");
var effect = await pool.Alloc(transform);

// 使用完毕后回收
pool.Recycle(effect);
```

### 2. 传统用法：Callback方式

```csharp
// 获取对象池
var pool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/bullet");

// 使用Callback分配对象
pool.Alloc(transform, (bullet) =>
{
    bullet.transform.position = firePoint.position;
    // 使用子弹...
});

// 使用完毕后回收
pool.Recycle(bullet);
```

---

## GameObject对象池API

### 获取对象池

```csharp
// 获取ActiveGameObjectPool（使用SetActive管理）
var pool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/bullet");

// 获取ParticlePool（自动播放/停止粒子）
var particlePool = LiteRuntime.ObjectPool.GetParticlePool("effects/explosion");

// 获取PositionGameObjectPool（使用位置管理）
var posPool = LiteRuntime.ObjectPool.GetPositionGameObjectPool("prefabs/item");

// 获取EmptyGameObjectPool（空GameObject池）
var emptyPool = LiteRuntime.ObjectPool.GetEmptyGameObjectPool("temp");
```

### 预加载对象池

```csharp
// 预加载对象池（等待Template加载完成）
await LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/explosion");

// 预加载并预生成10个对象
await LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("prefabs/bullet", 10);

// 批量预加载
await UniTask.WhenAll(
    LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/explosion", 5),
    LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/hit", 10),
    LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("prefabs/bullet", 20)
);
```

### Callback方式分配对象

```csharp
// 基础用法
pool.Alloc((gameObject) =>
{
    // 获取到对象后的处理
    gameObject.transform.position = Vector3.zero;
});

// 指定父节点
pool.Alloc(parentTransform, (gameObject) =>
{
    // 对象会被设置到指定父节点下
});
```

### UniTask方式分配对象

```csharp
// 基础用法
var gameObject = await pool.Alloc();

// 指定父节点
var gameObject = await pool.Alloc(parentTransform);
```

### 回收对象

```csharp
pool.Recycle(gameObject);
```

### 检查就绪状态

```csharp
// 检查Template是否已加载
if (pool.IsReady)
{
    // Template已加载，可以立即使用
    var obj = await pool.Alloc(transform);
}

// 等待Pool就绪
await pool.WaitReadyAsync();
```

### 预生成对象

```csharp
// 预生成10个对象到池中
pool.Generate(10, (completedPool) =>
{
    Debug.Log($"预生成完成，池中对象数：{completedPool.CountAll}");
});
```

### 获取池状态

```csharp
// 总对象数
int total = pool.CountAll;

// 活跃对象数
int active = pool.CountActive;

// 空闲对象数
int inactive = pool.CountInactive;

// 是否已加载完成
bool ready = pool.IsReady;
```

---

## 集合对象池API

### AllocList<T> / RecycleList<T>
```csharp
// 获取List
var list = LiteRuntime.ObjectPool.AllocList<int>();
list.Add(1);
list.Add(2);

// 回收（自动Clear）
LiteRuntime.ObjectPool.RecycleList(list);
```

### AllocDictionary<TKey, TValue> / RecycleDictionary<TKey, TValue>
```csharp
// 获取Dictionary
var dict = LiteRuntime.ObjectPool.AllocDictionary<int, string>();
dict[1] = "one";
dict[2] = "two";

// 回收
LiteRuntime.ObjectPool.RecycleDictionary(dict);
```

### AllocHashSet<T> / RecycleHashSet<T>
```csharp
// 获取HashSet
var hashSet = LiteRuntime.ObjectPool.AllocHashSet<int>();
hashSet.Add(1);
hashSet.Add(2);

// 回收
LiteRuntime.ObjectPool.RecycleHashSet(hashSet);
```

---

## 使用场景

### 1. 战斗场景完整流程

```csharp
public class BattleManager : MonoBehaviour
{
    private ParticlePool explosionPool;
    private ParticlePool hitPool;
    private ActiveGameObjectPool bulletPool;

    private async UniTaskVoid Start()
    {
        // 1. 进入战斗前预加载所有对象池
        Debug.Log("加载战斗资源...");

        await UniTask.WhenAll(
            LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/explosion", 5),
            LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/hit", 10),
            LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("prefabs/bullet", 20)
        );

        Debug.Log("战斗资源加载完成！");

        // 2. 获取对象池引用
        explosionPool = LiteRuntime.ObjectPool.GetParticlePool("effects/explosion");
        hitPool = LiteRuntime.ObjectPool.GetParticlePool("effects/hit");
        bulletPool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/bullet");

        // 3. 战斗中使用（此时都是接近同步的性能）
        for (int i = 0; i < 10; i++)
        {
            await FireBullet();
            await UniTask.Delay(100);
        }
    }

    private async UniTask FireBullet()
    {
        // 发射子弹（已预加载，性能最优）
        var bullet = await bulletPool.Alloc(transform);
        bullet.transform.position = transform.position;

        // 模拟子弹飞行
        await UniTask.Delay(500);

        // 生成击中特效
        var hitEffect = await hitPool.Alloc(null);
        hitEffect.transform.position = bullet.transform.position + Vector3.forward * 10;

        // 回收子弹
        bulletPool.Recycle(bullet);

        // 特效播放完后回收
        await UniTask.Delay(1000);
        hitPool.Recycle(hitEffect);
    }
}
```

### 2. 子弹系统（Callback方式）

```csharp
public class Gun : MonoBehaviour
{
    private ActiveGameObjectPool bulletPool;

    void Start()
    {
        // 获取子弹池
        bulletPool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/bullet");
    }

    public void Fire()
    {
        // 使用Callback方式分配子弹
        bulletPool.Alloc(transform, (bullet) =>
        {
            // 配置子弹
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = firePoint.rotation;

            var bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.Initialize(damage, speed);
        });
    }
}

public class Bullet : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 击中目标
        DealDamage(other);

        // 回收到对象池
        var bulletPool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/bullet");
        bulletPool.Recycle(gameObject);
    }
}
```

### 3. 特效管理器（UniTask方式）

```csharp
public class EffectManager : MonoBehaviour
{
    private Dictionary<string, ParticlePool> effectPools = new();

    // 场景加载时预加载特效
    public async UniTask PreloadEffects()
    {
        await UniTask.WhenAll(
            PreloadEffect("explosion"),
            PreloadEffect("hit"),
            PreloadEffect("smoke")
        );
    }

    private async UniTask PreloadEffect(string effectName)
    {
        await LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>($"effects/{effectName}", 5);
        effectPools[effectName] = LiteRuntime.ObjectPool.GetParticlePool($"effects/{effectName}");
    }

    // 播放特效（接近同步性能）
    public async UniTask PlayEffect(string effectName, Vector3 position, float duration = 2f)
    {
        if (!effectPools.TryGetValue(effectName, out var pool))
        {
            Debug.LogError($"特效池未预加载: {effectName}");
            return;
        }

        var effect = await pool.Alloc(null);
        effect.transform.position = position;

        // 自动回收
        await UniTask.Delay((int)(duration * 1000));
        pool.Recycle(effect);
    }
}
```

### 4. 集合复用（减少GC）

```csharp
public class EnemyAI : MonoBehaviour
{
    public void FindTargets()
    {
        // 从对象池获取List（避免频繁分配）
        var targets = LiteRuntime.ObjectPool.AllocList<Transform>();

        // 查找目标
        var colliders = Physics.OverlapSphere(transform.position, detectRadius);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                targets.Add(col.transform);
            }
        }

        // 处理目标
        ProcessTargets(targets);

        // 回收List（自动Clear）
        LiteRuntime.ObjectPool.RecycleList(targets);
    }

    private void ProcessTargets(List<Transform> targets)
    {
        foreach (var target in targets)
        {
            // 处理逻辑...
        }
    }
}
```

---

## 最佳实践

### 1. 场景加载时预加载对象池

```csharp
public class SceneLoader : MonoBehaviour
{
    async UniTask LoadBattleScene()
    {
        // 显示加载界面
        ShowLoadingUI();

        // 预加载所有对象池
        await PreloadObjectPools();

        // 加载场景
        await LoadSceneAsync();

        // 隐藏加载界面
        HideLoadingUI();
    }

    async UniTask PreloadObjectPools()
    {
        await UniTask.WhenAll(
            // 特效池
            LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/explosion", 5),
            LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/hit", 10),

            // GameObject池
            LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("prefabs/bullet", 20),
            LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("prefabs/enemy", 10)
        );
    }
}
```

### 2. 封装对象池管理类

```csharp
public static class BulletPool
{
    private const string KEY = "prefabs/bullet";
    private static ActiveGameObjectPool pool;

    // 初始化时调用
    public static async UniTask Initialize(int preloadCount = 20)
    {
        await LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>(KEY, preloadCount);
        pool = LiteRuntime.ObjectPool.GetActiveGameObjectPool(KEY);
    }

    // Callback方式
    public static void Alloc(Transform parent, System.Action<GameObject> callback)
    {
        pool.Alloc(parent, callback);
    }

    // UniTask方式
    public static UniTask<GameObject> Alloc(Transform parent = null)
    {
        return pool.Alloc(parent);
    }

    public static void Recycle(GameObject bullet)
    {
        pool.Recycle(bullet);
    }

    public static bool IsReady => pool?.IsReady ?? false;
}

// 使用
await BulletPool.Initialize(20);
var bullet = await BulletPool.Alloc(transform);
BulletPool.Recycle(bullet);
```

### 3. 混合使用Callback和UniTask

```csharp
public class WeaponSystem : MonoBehaviour
{
    private ActiveGameObjectPool bulletPool;

    async UniTask Start()
    {
        // 预加载
        await LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("prefabs/bullet", 20);
        bulletPool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/bullet");
    }

    // 老代码可以继续用Callback方式
    public void FireWithCallback()
    {
        bulletPool.Alloc(transform, (bullet) =>
        {
            SetupBullet(bullet);
        });
    }

    // 新代码推荐用UniTask方式
    public async UniTask FireWithUniTask()
    {
        var bullet = await bulletPool.Alloc(transform);
        SetupBullet(bullet);
    }

    private void SetupBullet(GameObject bullet)
    {
        bullet.transform.position = firePoint.position;
        // ...
    }
}
```

### 4. 检查就绪状态

```csharp
public class SmartSpawner : MonoBehaviour
{
    private ActiveGameObjectPool enemyPool;

    async UniTask Start()
    {
        enemyPool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/enemy");

        // 等待对象池就绪
        if (!enemyPool.IsReady)
        {
            Debug.Log("等待敌人池加载...");
            await enemyPool.WaitReadyAsync();
            Debug.Log("敌人池已就绪！");
        }

        // 开始生成敌人
        StartSpawning();
    }

    async void StartSpawning()
    {
        while (true)
        {
            // 对象池已就绪，分配对象几乎无开销
            var enemy = await enemyPool.Alloc(transform);
            enemy.transform.position = GetRandomSpawnPoint();

            await UniTask.Delay(1000);
        }
    }
}
```

### 5. 场景切换时清理对象池

```csharp
public class LevelManager : MonoBehaviour
{
    void OnDestroy()
    {
        // 清空未使用的对象池（释放内存）
        LiteRuntime.ObjectPool.RemoveUnusedPools();
    }
}
```

---

## 性能建议

### 1. 预加载是关键

```csharp
// ❌ 不好：运行时首次加载会有延迟
void Fire()
{
    var pool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/bullet");
    pool.Alloc(transform, (bullet) => { /* ... */ });  // 首次调用需要等待加载
}

// ✅ 好：提前预加载
async UniTask Start()
{
    // 场景开始时预加载
    await LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("prefabs/bullet", 20);
}

void Fire()
{
    var pool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("prefabs/bullet");
    pool.Alloc(transform, (bullet) => { /* ... */ });  // 立即返回
}
```

### 2. 预生成对象避免卡顿

```csharp
// 预加载并预生成对象
await LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/explosion", 10);

// 此时池中已有10个对象，使用时不需要Instantiate
```

### 3. 使用UniTask优化代码可读性

```csharp
// ❌ Callback嵌套（回调地狱）
pool1.Alloc(transform, (obj1) =>
{
    pool2.Alloc(transform, (obj2) =>
    {
        pool3.Alloc(transform, (obj3) =>
        {
            // 嵌套太深，难以维护
        });
    });
});

// ✅ UniTask扁平化
var obj1 = await pool1.Alloc(transform);
var obj2 = await pool2.Alloc(transform);
var obj3 = await pool3.Alloc(transform);
// 代码清晰，易于维护
```

### 4. 批量操作使用UniTask.WhenAll

```csharp
// 并行预加载多个对象池
await UniTask.WhenAll(
    LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/explosion", 5),
    LiteRuntime.ObjectPool.PreloadPoolAsync<ParticlePool>("effects/hit", 10),
    LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>("prefabs/bullet", 20)
);
```

---

## 对象池类型说明

### ActiveGameObjectPool
使用 `SetActive(true/false)` 管理对象显示/隐藏。

**适用场景**：大部分GameObject对象池

### ParticlePool
继承自 `ActiveGameObjectPool`，自动播放/停止粒子系统。

**适用场景**：特效、粒子系统

### PositionGameObjectPool
使用位置偏移（移到屏幕外）管理对象。

**适用场景**：需要保持对象激活状态的场景

### EmptyGameObjectPool
空GameObject池，不加载资源。

**适用场景**：动态创建的临时对象

---

## 常见问题

### Q: Callback和UniTask方式有什么区别？
A:
- Callback方式是主体实现，性能最优
- UniTask方式封装了Callback，代码更清晰
- 两种方式可以混合使用
- 推荐新代码使用UniTask方式

### Q: 预加载是必须的吗？
A:
- 不是必须的，但强烈推荐
- 不预加载的话，首次使用会有异步加载延迟
- 预加载后，分配对象几乎没有性能开销
- WebGL平台必须预加载才能获得良好性能

### Q: IsReady和WaitReadyAsync的区别？
A:
- `IsReady` 是属性，立即返回当前状态
- `WaitReadyAsync()` 是异步方法，会等待直到就绪
- 预加载后IsReady会立即返回true

### Q: 如何处理对象池中对象不足的情况？
A:
- 对象池会自动创建新对象，不会出现"不足"
- 可以通过预生成避免运行时创建卡顿
- CountActive、CountInactive可以监控对象池状态

---
