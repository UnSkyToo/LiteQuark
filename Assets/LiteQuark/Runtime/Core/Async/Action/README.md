# ActionSystem 动画补间系统

ActionSystem 是 LiteQuark 框架的动画和补间（Tween）模块，提供了丰富的动画效果和灵活的组合方式。

## 功能特性

### ✅ 核心功能
- **Transform动画**：位置、旋转、缩放补间
- **RectTransform动画**：UI专用动画（锚点、尺寸等）
- **组合动画**：序列（Sequence）和并行（Parallel）
- **缓动函数**：多种easing函数（Linear, EaseIn, EaseOut等）
- **回调支持**：开始、更新、完成回调

### ✅ 设计特点
- **零GC**：对象池管理，无GC压力
- **链式调用**：流畅的API设计
- **高性能**：基于Unity Update驱动
- **安全模式**：防止对已销毁对象执行动画

---

## 快速开始

### 1. 基础位移动画

```csharp
using LiteQuark.Runtime;

// 移动到目标位置（1秒，EaseOut缓动）
LiteRuntime.Action.Execute(
    LiteAction.MoveTo(transform, new Vector3(10, 0, 0), 1.0f, EasingFunction.EasingType.EaseOut)
);
```

### 2. 组合动画

```csharp
// 序列动画：先移动，再旋转，最后缩放
var sequence = LiteAction.Sequence(
    LiteAction.MoveTo(transform, Vector3.up * 5, 1.0f),
    LiteAction.RotateTo(transform, Quaternion.Euler(0, 180, 0), 0.5f),
    LiteAction.ScaleTo(transform, Vector3.one * 2, 0.5f)
);

LiteRuntime.Action.Execute(sequence);
```

---

## Transform动画API

### MoveTo
```csharp
public static IAction MoveTo(Transform transform, Vector3 targetPos, float duration, EasingType easing = EasingType.Linear)
```

移动到目标位置。

**示例：**
```csharp
// 平滑移动到(10, 0, 0)
LiteRuntime.Action.Execute(
    LiteAction.MoveTo(transform, new Vector3(10, 0, 0), 2.0f, EasingType.EaseInOut)
);
```

### MoveBy
```csharp
public static IAction MoveBy(Transform transform, Vector3 deltaPos, float duration, EasingType easing = EasingType.Linear)
```

相对移动。

**示例：**
```csharp
// 向上移动5个单位
LiteRuntime.Action.Execute(
    LiteAction.MoveBy(transform, Vector3.up * 5, 1.0f)
);
```

### RotateTo / RotateBy
```csharp
// 旋转到目标角度
LiteAction.RotateTo(transform, Quaternion.Euler(0, 180, 0), 1.0f);

// 相对旋转
LiteAction.RotateBy(transform, Vector3.up * 90, 1.0f);
```

### ScaleTo / ScaleBy
```csharp
// 缩放到目标大小
LiteAction.ScaleTo(transform, Vector3.one * 2, 1.0f);

// 相对缩放
LiteAction.ScaleBy(transform, Vector3.one * 0.5f, 1.0f);
```

---

## UI动画API（RectTransform）

### AnchorTo
```csharp
public static IAction AnchorTo(RectTransform rectTransform, Vector2 targetAnchor, float duration)
```

UI锚点动画。

**示例：**
```csharp
// UI从左侧滑入
var rectTransform = GetComponent<RectTransform>();
LiteRuntime.Action.Execute(
    LiteAction.AnchorTo(rectTransform, new Vector2(0, 0.5f), 0.5f)
);
```

### SizeDeltaTo
```csharp
public static IAction SizeDeltaTo(RectTransform rectTransform, Vector2 targetSize, float duration)
```

UI尺寸动画。

---

## 组合动画

### Sequence（序列）
```csharp
public static IAction Sequence(params IAction[] actions)
```

按顺序执行动画。

**示例：**
```csharp
// 跳跃动画：上升 -> 停留 -> 下落
var jumpSequence = LiteAction.Sequence(
    LiteAction.MoveBy(transform, Vector3.up * 3, 0.5f, EasingType.EaseOut),
    LiteAction.Wait(0.2f),
    LiteAction.MoveBy(transform, Vector3.down * 3, 0.5f, EasingType.EaseIn)
);

LiteRuntime.Action.Execute(jumpSequence);
```

### Parallel（并行）
```csharp
public static IAction Parallel(params IAction[] actions)
```

同时执行多个动画。

**示例：**
```csharp
// 同时移动和缩放
var parallelAnim = LiteAction.Parallel(
    LiteAction.MoveTo(transform, new Vector3(10, 0, 0), 1.0f),
    LiteAction.ScaleTo(transform, Vector3.one * 2, 1.0f)
);

LiteRuntime.Action.Execute(parallelAnim);
```

---

## 工具动画

### Wait
```csharp
public static IAction Wait(float duration)
```

等待指定时间。

**示例：**
```csharp
var sequence = LiteAction.Sequence(
    LiteAction.MoveTo(transform, Vector3.forward * 10, 1.0f),
    LiteAction.Wait(1.0f), // 等待1秒
    LiteAction.MoveTo(transform, Vector3.zero, 1.0f)
);
```

### CallFunc
```csharp
public static IAction CallFunc(Action callback)
```

执行回调函数。

**示例：**
```csharp
var sequence = LiteAction.Sequence(
    LiteAction.MoveTo(transform, targetPos, 1.0f),
    LiteAction.CallFunc(() => Debug.Log("到达目标！")),
    LiteAction.ScaleTo(transform, Vector3.zero, 0.5f)
);
```

---

## 使用场景

### 1. UI动画

```csharp
public class UIPanel : MonoBehaviour
{
    void Start()
    {
        var rectTransform = GetComponent<RectTransform>();

        // 淡入 + 缩放动画
        var openAnim = LiteAction.Parallel(
            LiteAction.ScaleTo(transform, Vector3.one, 0.3f, EasingType.EaseOut),
            LiteAction.AlphaTo(canvasGroup, 1.0f, 0.3f)
        );

        LiteRuntime.Action.Execute(openAnim);
    }

    public void Close()
    {
        // 淡出动画
        var closeAnim = LiteAction.Sequence(
            LiteAction.ScaleTo(transform, Vector3.zero, 0.2f, EasingType.EaseIn),
            LiteAction.CallFunc(() => Destroy(gameObject))
        );

        LiteRuntime.Action.Execute(closeAnim);
    }
}
```

### 2. 角色动画

```csharp
public class Character : MonoBehaviour
{
    public void Jump()
    {
        var jumpAnim = LiteAction.Sequence(
            // 上升
            LiteAction.MoveBy(transform, Vector3.up * 3, 0.4f, EasingType.EaseOut),
            // 下落
            LiteAction.MoveBy(transform, Vector3.down * 3, 0.4f, EasingType.EaseIn),
            // 着地回调
            LiteAction.CallFunc(OnLanded)
        );

        LiteRuntime.Action.Execute(jumpAnim);
    }

    void OnLanded()
    {
        Debug.Log("着地！");
    }
}
```

### 3. 道具收集动画

```csharp
public class Coin : MonoBehaviour
{
    public void Collect(Transform target)
    {
        var collectAnim = LiteAction.Parallel(
            // 飞向目标
            LiteAction.MoveTo(transform, target.position, 0.5f, EasingType.EaseIn),
            // 旋转
            LiteAction.RotateBy(transform, Vector3.up * 360, 0.5f),
            // 缩小
            LiteAction.ScaleTo(transform, Vector3.zero, 0.5f)
        );

        var sequence = LiteAction.Sequence(
            collectAnim,
            LiteAction.CallFunc(() =>
            {
                OnCollected();
                Destroy(gameObject);
            })
        );

        LiteRuntime.Action.Execute(sequence);
    }
}
```

---

## 缓动函数

支持多种缓动类型：

```csharp
public enum EasingType
{
    Linear,          // 线性
    EaseIn,          // 缓慢开始
    EaseOut,         // 缓慢结束
    EaseInOut,       // 两端缓慢
    EaseInQuad,      // 二次方缓入
    EaseOutQuad,     // 二次方缓出
    EaseInOutQuad,   // 二次方缓入缓出
    // ... 更多缓动类型
}
```

**选择建议：**
- **Linear**: 匀速运动
- **EaseOut**: UI出现（快速开始，缓慢停止）
- **EaseIn**: UI消失（缓慢开始，快速结束）
- **EaseInOut**: 平滑动画（两端缓慢）

---

## 最佳实践

### 1. 保存动画引用以便停止

```csharp
private ulong currentActionId;

void PlayAnimation()
{
    var anim = LiteAction.MoveTo(transform, targetPos, 2.0f);
    currentActionId = LiteRuntime.Action.Execute(anim);
}

void StopAnimation()
{
    LiteRuntime.Action.Stop(currentActionId);
}
```

### 2. 使用序列简化逻辑

```csharp
// ❌ 不好：嵌套回调
LiteRuntime.Action.Execute(
    LiteAction.MoveTo(transform, pos1, 1.0f)
).OnComplete(() => {
    LiteRuntime.Action.Execute(
        LiteAction.MoveTo(transform, pos2, 1.0f)
    ).OnComplete(() => {
        // 深层嵌套...
    });
});

// ✅ 好：使用Sequence
var sequence = LiteAction.Sequence(
    LiteAction.MoveTo(transform, pos1, 1.0f),
    LiteAction.MoveTo(transform, pos2, 1.0f),
    LiteAction.CallFunc(OnComplete)
);
LiteRuntime.Action.Execute(sequence);
```

### 3. 启用安全模式

```csharp
// 在LiteSetting中启用SafetyMode
// 防止对已销毁的对象执行动画
LiteSetting.Action.SafetyMode = true;
```

---

