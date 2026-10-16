# ActionSystem 动作与补间

`LiteRuntime.Action` 每帧驱动 `IAction`，支持序列、并行、Transform 补间、数值补间和等待。通过 `ActionBuilder` 组织动作，然后用 `AddBuilder` 启动。

## 最小示例

以下方法在框架初始化后调用，目标由业务传入：

```csharp
using LiteQuark.Runtime;
using UnityEngine;

public static class MotionExample
{
    public static ulong Start(Transform target)
    {
        var builder = ActionBuilder.Sequence("MoveAndScale")
            .TransformLocalMove(target, new Vector3(2f, 0f, 0f), 0.5f)
            .BeginParallel("Finish")
                .TransformScale(target, Vector3.one * 1.2f, 0.2f)
                .WaitTime(0.2f)
            .EndParallel()
            .Callback(() => LLog.Info("Sequence reached its end"));

        return LiteRuntime.Action.AddBuilder(builder, isSafety: true);
    }
}
```

保存返回的 ID，结束拥有者生命周期时调用 `StopAction(id)`。已有 `IAction` 可通过 `AddAction(action)` 启动；`FindAction(id)`、`IsEnd(id)` 和 `IsIdle()` 可查询状态。

## Builder 接口

| 类别 | 接口 |
| --- | --- |
| 组合 | `Sequence`、`Parallel`；嵌套使用 `BeginSequence` / `EndSequence`、`BeginParallel` / `EndParallel` |
| 位置 | `TransformLocalMove`、`TransformWorldMove`，以及对应的 `MovePath` |
| 旋转/缩放 | `TransformRotate`、`TransformLocalRotateAround`、`TransformWorldRotateAround`、`TransformScale` |
| 透明度/震动 | `TransformFade`、`TransformFadeIn`、`TransformFadeOut`、`TransformShaking` |
| UI 位置 | `RectTransformMove`、`RectTransformSetPosition` |
| 数值 | `ValueFromTo`，支持 float、int、Color |
| 等待/回调 | `WaitTime`、`WaitFrame`、`WaitUntil`、`Callback`、`DelayCallback` |
| 设置 | `SetFinalCallback`、`SetUnscaled`，以及 `TransformSet*` 扩展 |

具体参数和 `EaseKind` 以对应源码为准。当前没有 `MoveTo`、`AnchorTo`、`SizeDeltaTo` 或 `ActionSystem.Execute` 这组接口。

## 生命周期

- Builder 默认执行一次，组合动作的 `repeatCount < 0` 表示循环。循环体需要有等待或耗时动作，避免纯即时回调序列无限同步执行。
- `SetUnscaled()` 应设置在提交的最外层 Builder 上，使整个组合使用未缩放时间。框架整体暂停时仍停止更新。
- `AddBuilder(..., isSafety: true)` 或 `LiteRuntime.Setting.Action.SafetyMode` 会向子动作传播安全标记；Transform 动作借此检查目标是否仍存在。它不代替业务资源清理。
- `SetFinalCallback` 应设置在提交给 ActionSystem 的最外层动作上：自然结束或手动 `StopAction` 时调用，系统关闭时直接 Dispose 不调用。嵌套子动作的 FinalCallback 不会被组合动作自动触发；需要“自然走到末尾”的逻辑时，将 `Callback` 放在对应序列尾部。
- ActionSystem 负责结束后的 `Dispose()`；不要把同一动作实例重复提交为多个独立任务。

实现见 [ActionSystem.cs](ActionSystem.cs)、[ActionBuilder.cs](ActionBuilder.cs) 及同目录扩展类。动作创建包含对象和集合分配，没有零 GC 保证。
