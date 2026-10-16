# ObjectPoolSystem 对象池

`LiteRuntime.ObjectPool` 管理 GameObject 池和集合池。模板型池自行持有 prefab 的 AssetHandle，归还实例使用池的 `Recycle()`。

## 获取与归还

以下片段在异步方法中、框架初始化后执行：

```csharp
var pool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("Prefabs/Bullet.prefab");
await pool.WaitReadyAsync();
if (!pool.IsReady)
{
    return; // 模板尚未成功加载
}

var bullet = await pool.Alloc();
if (bullet == null)
{
    return;
}
// 使用 bullet；业务应保存它及所属池，直到回收。
pool.Recycle(bullet);
```

也可以使用 `pool.Alloc(parent, callback)`；UniTask 重载是对回调接口的封装。池键同时作为模板资源路径，应包含扩展名。同一 key 只对应一个池，避免用不同池类型复用同一 key。

## 预加载与池类型

```csharp
await LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>(
    "Prefabs/Bullet.prefab", 20, LiteRuntime.ObjectPool.GetPoolRoot());
```

不传 `generateCount` 的重载只等待模板加载；传入正数还会预生成实例。`pool.Generate(count, callback)` 可在之后补充实例。`WaitReadyAsync()` 等待模板加载回调，不提供独立的成功结果；使用前仍需检查 `IsReady` 和分配结果。

| 获取入口 | 行为 |
| --- | --- |
| `GetActiveGameObjectPool(path)` | 分配时启用，归还时禁用 |
| `GetParticlePool(path)` | 在 Active 池基础上播放/停止粒子 |
| `GetPositionGameObjectPool(path)` | 归还时移到远处，保留激活状态 |
| `GetEmptyGameObjectPool(key)` | 创建空 GameObject 模板，无外部 prefab |

`CountAll`、`CountActive`、`CountInactive` 可用于查看池状态。无空闲实例时按需创建，预加载并不保证后续没有实例化开销，也没有单独的 WebGL 必须预加载规则。

## 集合池

```csharp
var values = LiteRuntime.ObjectPool.AllocList<int>();
try
{
    values.Add(1);
}
finally
{
    LiteRuntime.ObjectPool.RecycleList(values);
}
```

同样提供 `AllocDictionary` / `RecycleDictionary`、`AllocHashSet` / `RecycleHashSet`，以及通用 `AllocCollection` / `RecycleCollection`。回收会清空集合，之后不能继续使用旧引用。

## 清理责任

- 每个实例只归还一次，并归还到原池；归还前重置业务状态、事件、计时器和外部引用。
- 回收不会完整重置组件业务状态；不要依赖它自动清理脚本字段或动画。
- `RemoveUnusedPools()` 移除没有活动实例的池。场景退出时先归还实例，再调用它。
- `RemovePool(pool)` 会销毁池并释放模板引用，使用前应确认没有仍在使用的实例。
- 业务操作在主线程执行，不要让拥有活动对象的父节点先销毁而遗忘池的借出记录。

完整接口见 [ObjectPoolSystem.cs](ObjectPoolSystem.cs) 和 [BaseGameObjectPool.cs](BaseGameObjectPool.cs)。
