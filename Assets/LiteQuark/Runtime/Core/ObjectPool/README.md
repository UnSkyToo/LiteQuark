# ObjectPoolSystem 对象池系统

ObjectPoolSystem 是 LiteQuark 框架的对象池管理模块，提供了高效的对象复用机制，减少GC压力。

## 功能特性

### ✅ 核心功能
- **GameObject对象池**：复用GameObject实例
- **集合对象池**：复用List、Dictionary、HashSet等集合
- **自定义对象池**：支持任意类型的对象池
- **自动管理**：自动创建和清理对象池
- **性能优化**：减少Instantiate和Destroy的开销

### ✅ 设计特点
- **零配置**：首次使用自动创建对象池
- **类型安全**：泛型支持，编译时类型检查
- **灵活扩展**：可自定义对象池实现

---

## 快速开始

### 1. GameObject对象池

```csharp
// 从对象池获取GameObject
var bullet = LiteRuntime.ObjectPool.Get("Bullet", "Prefabs/Bullet");
bullet.transform.position = firePoint.position;
bullet.SetActive(true);

// 使用完毕后回收
LiteRuntime.ObjectPool.Release("Bullet", bullet);
```

### 2. 集合对象池

```csharp
// 获取List
var list = LiteRuntime.ObjectPool.GetList<int>();
list.Add(1);
list.Add(2);

// 使用完毕后回收
LiteRuntime.ObjectPool.ReleaseList(list); // 自动清空
```

---

## GameObject对象池API

### Get
```csharp
public GameObject Get(string key, string prefabPath, Transform parent = null)
```

从对象池获取GameObject。

**参数：**
- `key` - 对象池唯一键
- `prefabPath` - 预制体路径（首次创建时使用）
- `parent` - 父级Transform

**示例：**
```csharp
// 子弹对象池
var bullet = LiteRuntime.ObjectPool.Get("Bullet", "Prefabs/Bullet");
bullet.SetActive(true);

// 敌人对象池（指定父级）
var enemy = LiteRuntime.ObjectPool.Get("Enemy", "Prefabs/Enemy", enemyContainer);
```

### Release
```csharp
public void Release(string key, GameObject obj)
```

回收GameObject到对象池。

**示例：**
```csharp
// 回收对象
LiteRuntime.ObjectPool.Release("Bullet", bullet);

// 对象会被SetActive(false)并回到对象池
```

### Clear
```csharp
public void Clear(string key)
```

清空指定对象池。

**示例：**
```csharp
// 关卡结束时清空敌人池
LiteRuntime.ObjectPool.Clear("Enemy");
```

---

## 集合对象池API

### GetList<T> / ReleaseList<T>
```csharp
public List<T> GetList<T>()
public void ReleaseList<T>(List<T> list)
```

**示例：**
```csharp
// 获取List
var enemies = LiteRuntime.ObjectPool.GetList<Enemy>();
foreach (var enemy in GetNearbyEnemies())
{
    enemies.Add(enemy);
}

// 处理逻辑...

// 回收（自动Clear）
LiteRuntime.ObjectPool.ReleaseList(enemies);
```

### GetDictionary<TKey, TValue> / ReleaseDictionary<TKey, TValue>
```csharp
public Dictionary<TKey, TValue> GetDictionary<TKey, TValue>()
public void ReleaseDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict)
```

**示例：**
```csharp
var itemCounts = LiteRuntime.ObjectPool.GetDictionary<int, int>();
itemCounts[1] = 10;
itemCounts[2] = 5;

// 使用...

LiteRuntime.ObjectPool.ReleaseDictionary(itemCounts);
```

### GetHashSet<T> / ReleaseHashSet<T>
```csharp
public HashSet<T> GetHashSet<T>()
public void ReleaseHashSet<T>(HashSet<T> hashSet)
```

---

## 使用场景

### 1. 子弹对象池

```csharp
public class Gun : MonoBehaviour
{
    private const string BULLET_KEY = "Bullet";
    private const string BULLET_PATH = "Prefabs/Bullet";

    public void Fire()
    {
        // 从对象池获取子弹
        var bullet = LiteRuntime.ObjectPool.Get(BULLET_KEY, BULLET_PATH, transform);
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;
        bullet.SetActive(true);

        // 配置子弹
        var bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.Initialize(damage, speed);
    }
}

public class Bullet : MonoBehaviour
{
    private const string BULLET_KEY = "Bullet";

    void OnTriggerEnter(Collider other)
    {
        // 击中目标
        DealDamage(other);

        // 回收到对象池
        LiteRuntime.ObjectPool.Release(BULLET_KEY, gameObject);
    }
}
```

### 2. 特效对象池

```csharp
public class EffectManager
{
    public void PlayEffect(string effectName, Vector3 position)
    {
        var effect = LiteRuntime.ObjectPool.Get(effectName, $"Effects/{effectName}");
        effect.transform.position = position;
        effect.SetActive(true);

        // 自动回收
        StartCoroutine(AutoRelease(effectName, effect, 2.0f));
    }

    IEnumerator AutoRelease(string key, GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        LiteRuntime.ObjectPool.Release(key, effect);
    }
}
```

### 3. 集合复用

```csharp
public class EnemyAI
{
    public void FindTargets()
    {
        // 从对象池获取List（避免频繁分配）
        var targets = LiteRuntime.ObjectPool.GetList<Transform>();

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

        // 回收List
        LiteRuntime.ObjectPool.ReleaseList(targets);
    }
}
```

---

## 最佳实践

### 1. 使用常量管理Key

```csharp
public static class PoolKeys
{
    public const string BULLET = "Bullet";
    public const string ENEMY = "Enemy";
    public const string COIN = "Coin";
}

// 使用
var bullet = LiteRuntime.ObjectPool.Get(PoolKeys.BULLET, "Prefabs/Bullet");
```

### 2. 封装对象池管理

```csharp
public class BulletPool
{
    private const string KEY = "Bullet";
    private const string PATH = "Prefabs/Bullet";

    public static GameObject Get()
    {
        return LiteRuntime.ObjectPool.Get(KEY, PATH);
    }

    public static void Release(GameObject bullet)
    {
        LiteRuntime.ObjectPool.Release(KEY, bullet);
    }

    public static void Clear()
    {
        LiteRuntime.ObjectPool.Clear(KEY);
    }
}

// 使用
var bullet = BulletPool.Get();
// ...
BulletPool.Release(bullet);
```

### 3. 关卡结束时清理

```csharp
public class LevelManager
{
    void OnLevelEnd()
    {
        // 清空所有关卡相关对象池
        LiteRuntime.ObjectPool.Clear("Enemy");
        LiteRuntime.ObjectPool.Clear("Bullet");
        LiteRuntime.ObjectPool.Clear("Coin");
    }
}
```

---

## 性能建议

### 1. 避免频繁创建销毁

```csharp
// ❌ 不好：频繁Instantiate/Destroy
void Update()
{
    var bullet = Instantiate(bulletPrefab);
    // ...
    Destroy(bullet);
}

// ✅ 好：使用对象池
void Update()
{
    var bullet = LiteRuntime.ObjectPool.Get("Bullet", "Prefabs/Bullet");
    // ...
    LiteRuntime.ObjectPool.Release("Bullet", bullet);
}
```

### 2. 预热对象池

```csharp
void Start()
{
    // 预先创建对象（避免游戏中首次创建卡顿）
    for (int i = 0; i < 20; i++)
    {
        var bullet = LiteRuntime.ObjectPool.Get("Bullet", "Prefabs/Bullet");
        LiteRuntime.ObjectPool.Release("Bullet", bullet);
    }
}
```

---

## 许可证

MIT License - 与 LiteQuark 框架保持一致
