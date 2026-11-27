# ActionSystem 动作与补间

`LiteRuntime.Action` 由框架 Tick 驱动，支持序列、并行、Transform 补间、数值补间和等待。通过 `ActionBuilder` 组织动作，然后用 `AddBuilder` 启动。

## 最小示例

以下方法在框架初始化后调用，目标由业务传入：

```csharp
using LiteQuark.Runtime;
using UnityEngine;

public static class MotionExample
{
    public static IAction Start(Transform target)
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

`AddBuilder` 和 `AddAction` 返回 `IAction`，可在 Unity 主线程读取 `IsDone` 并调用 `Cancel()`。自定义动作继承 `BaseAction`，通过 `AddAction` 提交或用 `ActionBuilder.Add` 加入组合。已有 ID 时可用 `FindAction(id)` 查找，或用 `StopAction(id)` 按 ID 取消。

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
| 设置 | `SetUnscaled`、`TransformSet*` 扩展 |

具体参数和 `EaseKind` 见同目录的 Builder 扩展接口。

## 生命周期

- Builder 默认执行一次，组合动作的 `repeatCount < 0` 表示循环。循环体需要有等待或耗时动作，避免纯即时回调序列无限同步执行。
- `SetUnscaled()` 应设置在提交的最外层 Builder 上，使整个组合使用未缩放时间。框架整体暂停时仍停止更新。
- `AddBuilder(..., isSafety: true)` 或 `LiteRuntime.Setting.Action.SafetyMode` 会向子动作传播安全标记；Transform 动作借此检查目标是否仍存在。它不代替业务资源清理。
- `Cancel()` 立即标记动作结束，重复取消安全。取消组合会停止其子动作和后续循环。系统处理结束状态时释放并移除动作，`IsIdle()` 在移除后反映结果。
- `Callback` 是序列中的一步，适合在序列走到指定位置时执行逻辑。循环序列每轮走到该步骤时都会调用，主动取消会跳过尚未执行的步骤。
- 每次提交使用独立的动作实例。业务在拥有者结束时取消仍在运行的动作，系统关闭时取消并释放剩余动作。

实现见 [ActionSystem.cs](ActionSystem.cs)、[ActionBuilder.cs](ActionBuilder.cs) 及同目录扩展类。动作创建包含对象和集合分配，没有零 GC 保证。
