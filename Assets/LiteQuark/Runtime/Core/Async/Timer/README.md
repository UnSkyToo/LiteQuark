# TimerSystem 定时器

通过 `LiteRuntime.Timer` 创建时间回调，返回 `ulong` ID。定时器由框架 Tick 驱动，默认使用缩放后的时间。

## 常用操作

以下片段在框架初始化后执行：

```csharp
ulong timerId = LiteRuntime.Timer.AddTimer(
    interval: 1f,
    onTick: () => LLog.Info("Tick"),
    repeatCount: TimerSystem.RepeatCountForever,
    delayTime: 0.5f);

var timer = LiteRuntime.Timer.FindTimer(timerId);
timer?.Pause();
timer?.Resume();
// 拥有者退出或不再需要定时器时：
LiteRuntime.Timer.StopTimer(timerId);
```

| API | 行为 |
| --- | --- |
| `AddTimer(interval, onTick, repeatCount = 1, delayTime = 0f)` | 按秒触发；`-1` 为无限重复 |
| `AddTimer(interval, onTick, totalTime)` | 由 `Round(totalTime / interval)` 计算次数 |
| `AddTimer(interval, onTick, onComplete, totalTime)` | 有限次数自然完成后执行完成回调 |
| `AddUnscaledTimer(...)` | 使用未缩放时间，提供次数或总时长加完成回调的重载 |
| `AddTimerWithFrame(frameCount, onTick, repeatCount = 1, delayTime = 0f)` | 根据目标帧率换算时间间隔 |
| `NextFrame(onTick)` | 创建零间隔、执行一次的定时器 |
| `FindTimer(id)` | 返回 `ITimer` 或 null；可 Pause / Resume / Cancel |
| `StopTimer(id)` / `CancelAll()` | 取消单个 / 全部定时器 |

## 时间与完成语义

- `delayTime` 是首次计时间隔前的额外等待，不替代 `interval`。
- 第三个实参为整数时选择次数重载；总时长使用浮点值或命名参数 `totalTime:`，避免混淆。
- `AddTimerWithFrame` 使用创建 TimerSystem 时的 `max(0.01, 1 / TargetFrameRate)` 换算秒数，不是实际渲染帧计数。需要按 Tick 数等待时可使用 Action 的 `WaitFrame`。
- `NextFrame` 在该定时器下一次获得 Tick 时执行；同帧是否发生取决于调用相对框架更新的位置，不是 Unity 帧边界保证。
- 单个定时器每次 Tick 最多补发 10 次回调，达到上限后丢弃剩余累积时间。
- `StopTimer`、`Cancel` 不触发 `onComplete`。保存 ID，并在拥有者释放时停止仍在运行的定时器。
- Unscaled 模式不受时间缩放影响，但框架整体暂停时仍不会更新。

完整重载见 [TimerSystem.cs](TimerSystem.cs)，时间推进实现见 [BaseTimer.cs](BaseTimer.cs)。
