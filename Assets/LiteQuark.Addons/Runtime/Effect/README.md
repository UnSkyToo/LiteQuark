# EffectSystem 特效

EffectSystem 属于 LiteQuark.Addons。在 Launcher 的 `SystemList` 注册后，使用 `LiteRuntime.Get<EffectSystem>()`。它借用 ActiveGameObjectPool 实例，管理粒子和 Animator，并对手动配置的 TrailRenderer 提供速度和停止处理。

## 播放与停止

```csharp
using LiteQuark.Runtime;
using UnityEngine;

public static class EffectExample
{
    public static ulong Play(Transform parent)
    {
        var info = new EffectCreateInfo(
            parent, "Effects/Hit.prefab", EffectSpace.Local,
            Vector3.zero, 1f, Quaternion.identity,
            speed: 1f, isLoop: false, lifeTime: 0.8f);
        info.SetCompleteCallback(() => LLog.Info("Effect naturally completed"));
        return LiteRuntime.Get<EffectSystem>().PlayEffect(info);
    }
}
```

`PlayEffect` 返回 `ulong` ID；空白路径返回 0，非零 ID 不代表异步资源加载已成功。业务保存循环特效的 ID，在不再需要时调用 `StopEffect(id)`。

## 创建参数

| 参数 | 含义 |
| --- | --- |
| `parent` / `path` | 父节点和相对 `Assets/StandaloneAssets` 的 prefab 路径，包含扩展名 |
| `space` | `Local` / `World` 或 LocalPosition、LocalRotation 等标记组合 |
| `position` / `rotation` / `scale` | 初始位置、旋转和统一缩放 |
| `speed` | 初始播放速度，默认 1 |
| `isLoop` | 是否循环；与 Binder 的循环标记取“或” |
| `lifeTime` | 大于 0 时覆盖 Binder 的时长；否则使用 Binder 值 |
| `order` | 大于 0 时对原排序值增加偏移；默认 -1，不修改 |
| `layerName` | Sorting Layer 名称，不是 GameObject Layer |

`EffectCreateInfo` 是 struct，使用 `SetParent`、`SetOrder`、`SetCompleteCallback` 在播放前修改；这些方法返回 void，不能链式调用。传给 `PlayEffect` 后再修改本地副本不会改变已创建的特效。

父节点存在时，LocalPosition / LocalRotation 决定是否采用局部值；父节点为 null 时使用世界位置和旋转。`Auto` 为 0，当前按未设置局部标记处理。

## Binder 与结束流程

在 prefab 上添加 `EffectBinder`，在 Inspector 点击 **Update** 收集激活子节点中的粒子和 Animator，并重新计算时长。`Trails` 数组需在 Inspector 手动配置，Update 不会收集 TrailRenderer。没有 Binder 或缓存为空时，运行时会尝试添加/扫描；建议在 prefab 中预先配置并保存。

- 非循环特效在 LifeTime 耗尽后调用完成回调，再进入停止流程。
- `StopEffect` 进入 Finishing，等待 Binder 的 RetainTime 后归还对象池；手动停止不调用自然完成回调。
- Binder 发现循环粒子、循环动画，或计算出的时长接近 0 时标记循环；拖尾不参与时长计算。即使创建参数 `isLoop: false`，也不能强制覆盖 Binder 的循环属性。
- 循环特效需要主动停止；不要将完成回调当作停止、加载失败等所有路径的统一通知。
- 父节点应保留到特效完成回收，或将特效挂到寿命更长的节点；`StopEffect` 不会立即归还对象，调用后立即销毁父节点仍可能破坏池对象。

## 控制与预热

`FindEffect(id)` 返回 `BaseObject`，需要转换为 `EffectObject` 后检查 `IsValid`。可用接口包括 `SetSpeed`、`ResetSpeed`、`SetTime`、`Play(speed)`、`PlayAnimation(stateName, layer, speed)` 和 `Stop()`；当前没有公开的 Pause/Resume 接口。

`SetTime` 调整 Binder 中组件的播放位置，不同步 EffectObject 自身的剩余生命周期。`SetSpeed(0)` 同时暂停剩余时长推进，可能使停止后的保留阶段也无法完成。

需要预热时，在异步方法中调用：

```csharp
await LiteRuntime.ObjectPool.PreloadPoolAsync<ActiveGameObjectPool>(
    "Effects/Hit.prefab", 8, LiteRuntime.ObjectPool.GetPoolRoot());
```

实现见 [EffectSystem.cs](EffectSystem.cs)、[EffectObject.cs](EffectObject.cs) 和 [EffectBinder.cs](EffectBinder.cs)。
