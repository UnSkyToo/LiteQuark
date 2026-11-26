# AssetSystem 资源管理

通过 `LiteRuntime.Asset` 加载资源、实例和场景。业务保存 Handle，使用 `await handle` 等待、`handle.Result` 读取结果，并在使用结束后调用 `Dispose()`。等待完成不会转移所有权；资源引用本身不能替代 Handle。

## 常用入口

| API | 结果与释放责任 |
| --- | --- |
| `LoadAssetHandle<T>(path, priority, ct)` | `AssetHandle<T>` 持有资源；Dispose 释放引用 |
| `InstantiateHandle(path, parent, ...)` | `GameObjectHandle` 持有实例；Dispose 销毁实例并释放 prefab 引用 |
| `LoadSceneHandle(path, priority, ct)` | 以 Additive 加载业务场景，`SceneHandle.Dispose()` 发起该实例的卸载 |
| `PreloadAssetHandle<T>(paths, priority, ct)` | `PreloadHandle<T>` 持有批量资源引用，Result 为成功标志 |
| `HasAsset(path)` / `GetVersion()` | 查询资源是否收录／获取版本 |
| `UnloadUnusedAssets(maxDepth: 5)` | 清理零引用缓存；不宜逐帧调用 |

Handle 在 Unity 主线程创建、取消和释放。`priority` 默认 `Normal`，`ct` 默认空令牌。示例在框架启动后的异步方法中使用 `LiteQuark.Runtime`、`Cysharp.Threading.Tasks`；完整重载见 [AssetSystem.cs](AssetSystem.cs)。

```csharp
using var handle = LiteRuntime.Asset.LoadAssetHandle<UnityEngine.TextAsset>("Config/game.json");
await handle;
if (handle.State == AssetHandleState.Failed)
{
    LLog.Error("Load failed: {0}, {1}", handle.Error.ResourceKey, handle.Error.Message);
    return;
}
var json = handle.Result.text;
// 在 Handle 有效期内解析或复制所需数据。
```

长期使用的资源把 Handle 保存在拥有者字段，在界面关闭、`OnDestroy()` 或业务释放时 Dispose。手动实例化也须保留 prefab Handle；`UIUtils.ReplaceSpriteHandle(...)` 返回的 Handle 同样由调用者释放。

## 请求状态与错误

Handle 从 `Pending` 单次进入终态；缓存命中可能同步完成。`IsDone` 表示请求结束，底层操作和清理可能仍在进行。主线程可读取 `Progress` 绘制加载条：成功为 1，失败或取消保留最近一次读取值；远端传输进度依赖 `Content-Length`。以 `State` 判断结果。

| 情况 | 状态、等待与所有权 |
| --- | --- |
| 成功 | `Succeeded`，Error 为 null，Handle 持有结果 |
| 普通加载失败 | `Failed`，Error 非空；await 正常结束，Result 为 null / false |
| 等待中在主线程取消 token 或 Dispose | 立即成为 `Cancelled`，Error 为 null；await 抛 `OperationCanceledException` |
| 成功或失败后 Dispose | State / Error 保留，IsDisposed 为 true；释放持有的资源 |
| 成功后 token 取消 | 保留成功及所有权，业务仍需 Dispose |

- `IsDisposed` 只表示显式释放，重复 Dispose 无额外效果。释放后对象和场景的 `Result` 为空，预加载仍保留批次结论。
- 取消当前 Handle 不保证停止共享加载或 Unity 原生操作。优先用 `using` 或 `try/finally` 确保释放。
- 非法路径等入口参数同步抛异常。实例化异常保存在 `Error`，同时由 `await` 抛出。

[AssetError](Base/AssetError.cs) 的 `ResourceKey` 指向实际失败的资源或依赖 Bundle；`Message` 用于诊断，`Exception` 可能为空。

## 场景与预加载

保留启动场景，用 `LoadSceneHandle("Scenes/Battle.unity")` 叠加加载关卡；请求已在场的启动场景会失败。加载保留当前活动场景；随关卡退场的动态对象应放在该关卡根节点下。`SceneHandle.Result` 是 `Scene?`，失败、释放或场景失效时为空。

同一轮 Runtime 内，同路径从请求受理到实际卸载一直被占用；重复请求得到 Failed Handle，原请求继续。加载失败且未留下场景可立即重试；已加载的场景 Dispose 后应等到实际卸载再重开。**需要同时加载的 Bundle 场景须使用不同文件名**：Unity 2022.3.62f3 验证中，同名场景同包构建冲突、分包也不能同时处于已加载状态；Simulate 可以按路径区分。

```csharp
using var preload = LiteRuntime.Asset.PreloadAssetHandle<UnityEngine.Sprite>(new[] { "UI/IconA.png", "UI/IconB.png" });
await preload;
// 保留 preload 以维持批量引用，使用具体资源时另取 AssetHandle。
```

预加载在发起前校验并去重路径。用 `TotalCount`、`LoadedCount` 和 `FailedCount` 查看结果，`FailedPaths` 与 `Errors` 按索引对应。部分失败仍保留成功项直到 Dispose，`Error` 为首个失败原因。

## 系统关闭

`Restart()` / `Shutdown()` 可在关卡还在加载或运行时调用。退场时停止新加载、释放业务持有的 Handle；可在游戏入口的 `DisposeAsync()` 中释放已交付的场景。重启后再次加载同一路径前，确认旧场景已卸载，关闭资源系统不会替业务卸载已交付场景。

关闭时拒绝新请求、取消排队工作，未交付的场景请求报告失败，底层迟到结果由框架清理。旧 Bundle 不会因尚存的 Handle 或保留期而继续有效。退出进程可能中断跨帧清理。

## 路径与模式

默认根目录为 `Assets/StandaloneAssets`。传入带扩展名的相对路径 `Prefabs/Player.prefab` 或项目路径 `Assets/StandaloneAssets/Prefabs/Player.prefab`。场景须以 `.unity` 结尾，实际加载保留源路径大小写。

- 清单按小写完整项目路径查询，反斜杠转为 `/`。路径为空、含 `..` 目录段、无扩展名或场景扩展名有误时，加载入口同步抛 `ArgumentException`；未收录时 `HasAsset` 返回 `false`，加载返回 Failed Handle。
- `Assets/` 开头按项目路径解释，忽略大小写；根外资源也须收录在清单中。访问根下的同名子目录请传完整路径，例如 `Assets/StandaloneAssets/Assets/UI/Hud.prefab`。`Packages/...` 按资源根相对路径解释；真正的 UPM 资源可由 Unity 作为引用依赖隐式打包，框架不支持显式收集或直接加载。
- 编辑器保存与回读路径可用 [`AssetPath.ToRootRelativePath` / `ToProjectPath`](Base/AssetPath.cs)，两者保留输入大小写。

在 Launcher 的 **Asset** 模块配置；模式与 CDN 支持 Development 覆盖，运行时从 `LiteRuntime.Setting.Asset` 读取。

| 配置 | 含义 |
| --- | --- |
| `BundleMode` | Release 默认 BuiltIn，Development 默认 Simulate；Remote 须显式选择。非 Editor 的 Simulate（包括 Development Build）自动按 BuiltIn 加载，仍须部署本地资源 |
| `BundleRemoteUri` | 绝对基础 URI，按目录追加小写平台名和版本号前两段（如 0.4）；传未编码路径名称，已有查询和 fragment 保留，勿配置框架保留参数 t |
| `ConcurrencyLimit` | Bundle Loader 同时提交加载任务的上限，默认 5 |
| `TaskConcurrencyLimit` | 独立 Asset 任务组额度，默认 20，运行时至少为 1；覆盖版本包、Bundle、Asset 和 Scene 加载任务 |
| `EnableRetain` / `BundleRetainTime` | Bundle 零引用保留缓存开关／时间，默认 true／300 秒 |

- **Simulate** 按当前构建目标收集清单，不需要预构建 Bundle。改动收录、分包或依赖后须重新初始化。物理文件名、平台构建、压缩、下载、原生卸载及同名场景限制用 BuiltIn 或 Remote 验证。
- **BuiltIn** 优先使用 `persistentDataPath/LiteQuark` 中存在的文件，缺失时使用 `StreamingAssets/LiteQuark`。编辑器默认 `EditorForceStreamingAssets = true`，直接使用 StreamingAssets。persistent 文件存在但损坏时不会回退。
- **Remote** 从 CDN 获取清单和 Bundle。清单请求使用随机参数 `t`，重试沿用同一值；CDN 应将它计入清单缓存键，或对清单路径启用绕过缓存或回源验证。Bundle 仍可按地址和 Hash 缓存。启动中关闭会中止在途清单和重试。

## 加载优先级

- `AssetLoadPriority` 的 Low / Normal / High 决定尚未提交的 Bundle 加载顺序，同级 FIFO，提权保留入队顺序。主包、依赖及预加载子请求均使用传入优先级；同一共享加载周期只升不降，取消高优先级请求也不降级。
- 已提交的任务不抢占，优先级不保证完成顺序，持续高优先级请求可能推迟低优先级请求。Bundle 提交额度和 Asset 任务组额度同时生效，排队等待任务组准入的 Bundle 也占提交名额，不占业务 Default 组。
- [TaskPriority](../Core/Async/Task/README.md#优先级与执行时序) 独立生效：版本和 Bundle 任务使用 Urgent，资源和场景任务使用 Normal。Profiler 的 Bundle Begin 不代表实际提交顺序，事件订阅者不要在回调里修改资源生命周期。

## 缓存与构建工具

保留期只作用于零引用 Bundle，主包卸载后才释放依赖引用。`UnloadUnusedAssets(maxDepth)` 最多清理指定轮数，并调用 Unity 未使用资源清理和 GC；释放最后引用后可能立即回收。

收集优先用 `Assets/Editor/ResSetting.asset`，其次查找项目中的 `ResCollectorSetting`，缺失则用默认规则。修改文件夹 Inspector 的 **Individual Bundle** 或使用 Create 菜单可创建设置；浏览、模拟或构建不会自动创建设置。收集发现依赖环会报错。

用 `Lite > Builder` 生成匹配的清单与 Bundle，一起部署到 StreamingAssets 或 CDN。清单读取或解析失败会阻止资源系统启动。旧清单升级见 [CHANGELOG](../../CHANGELOG.md)。`VersionPackInfo` 按以下规则计算物理文件名：

| 配置（逻辑名为 ui/icons.ab） | 构建文件名 | 加载文件名 |
| --- | --- | --- |
| FlatMode、HashMode 关闭 | `ui/icons.ab` | `ui/icons.ab` |
| 仅 FlatMode | `ui_icons.ab` | `ui_icons.ab` |
| 仅 HashMode | `ui/icons.ab` | `ui/icons_<hash>.ab` |
| FlatMode、HashMode 开启，SecurityMode 关闭 | `ui_icons.ab` | `ui_icons_<hash>.ab` |
| FlatMode、HashMode、SecurityMode 开启 | `ui_icons.ab` | `<hash>.ab` |

FlatMode 可能产生同名文件（如 `a/b_c.ab` 和 `a_b/c.ab`）；调整命名或关闭 FlatMode，HashMode 无法避免构建阶段碰撞。用 `Lite > Asset > Bundle Viewer` 检查收集结果，Alt 选择条目可定位源资源或 Bundle 首个资源所在目录。
