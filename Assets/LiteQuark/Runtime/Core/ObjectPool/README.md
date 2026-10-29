# ObjectPoolSystem 对象池

`LiteRuntime.ObjectPool` 管理 GameObject 池和集合池。模板型池自行持有 prefab 的 AssetHandle，归还实例使用池的 `Recycle()`。

## 获取与归还

以下片段在异步方法中、框架初始化后执行：

```csharp
var pool = LiteRuntime.ObjectPool.GetActiveGameObjectPool("Prefabs/Bullet.prefab");
if (!await pool.WaitReadyAsync())
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

也可以使用 `pool.Alloc(parent, callback)`，与异步入口共用分配逻辑。模板加载失败时异步分配返回 null；关闭取消时 await 抛 `OperationCanceledException`。回调在失败或取消时均收到 null，必须先检查结果。池键同时作为模板资源路径，应包含扩展名。同一 key 只对应一个池，避免用不同池类型复用同一 key。

## 预加载与池类型

```csharp
var succeeded = await LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>(
    "Prefabs/Bullet.prefab", 20, LiteRuntime.ObjectPool.GetPoolRoot());
```

不传 `generateCount` 的重载只等待模板加载；传入正数还会预生成实例。`WaitReadyAsync()`、`GenerateAsync(count)` 和 `PreloadPoolAsync` 成功返回 true，普通失败返回 false，关闭取消时抛 `OperationCanceledException`。失败的模板不会自动重试；再次等待会立即得到失败结果。

使用 `await pool.GenerateAsync(count)` 补充实例。预生成成功表示实例已经由池接管并完成入池处理；不保证之后没有实例化开销。`GenerateAsync(0)` 只等待模板，负数是参数错误。多个预生成请求独立结束。

预生成入池时，自定义 `OnRelease` 等执行逻辑抛出的异常会向调用方传播，尚未入池的实例会被清理；已经入池的部分保留在池中。这类执行异常不转换为普通加载失败的 false。

| 获取入口 | 行为 |
| --- | --- |
| `GetActiveGameObjectPool(path)` | 分配时启用，归还时禁用 |
| `GetParticlePool(path)` | 在 Active 池基础上播放/停止粒子 |
| `GetPositionGameObjectPool(path)` | 归还时移到远处，保留激活状态 |
| `GetEmptyGameObjectPool(key)` | 创建空 GameObject 模板，无外部 prefab |

`CountAll`、`CountActive`、`CountInactive` 可用于查看池状态。无空闲实例时按需创建，预加载并不保证后续没有实例化开销。

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
- `RemoveUnusedPools()` 移除没有活动实例的池，也会取消这些池尚未完成的加载等待和预生成。场景退出时先归还实例，再调用它。
- `RemovePool(pool)` 会销毁池并释放模板引用，使用前应确认没有仍在使用的实例。
- 业务操作在主线程执行，不要让拥有活动对象的父节点先销毁而遗忘池的借出记录。

系统由 Runtime 关闭，关闭开始后拒绝获取或创建池。池的同步 Dispose 取消未完成请求、停止并同步等待自身原生实例化，再销毁闲置对象和池节点、释放模板。返回时 Destroy 可能尚未实际执行；挂到外部父节点的借出实例仍由业务负责，业务应在池关闭前归还。关闭不等待借出实例归还，也不保留旧池的可用性。

完整接口见 [ObjectPoolSystem.cs](ObjectPoolSystem.cs) 和 [BaseGameObjectPool.cs](BaseGameObjectPool.cs)。
