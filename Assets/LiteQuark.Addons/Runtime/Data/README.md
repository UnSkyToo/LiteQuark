# DataSystem 数据持久化

DataSystem 属于 LiteQuark.Addons。将它加入 Launcher 的 `SystemList`，并在 `SystemSettings` 配置 `DataSystemSetting`。初始化后使用 `LiteRuntime.Get<DataSystem>()`。

## 配置与格式

| 配置 | 默认值 | 说明 |
| --- | --- | --- |
| `ProviderMode` | PlayerPrefs | PlayerPrefs / JsonFile / BinaryFile / Custom |
| `EnableEncryption` | true | 内置 Provider 的加密开关 |
| `EncryptionKey` | LiteQuark | 内置 Provider 使用的密钥字符串 |
| `CustomProviderType` | 未指定 | Custom 模式下实现 `IDataProvider` 的类型 |

内置 Provider 均使用 LitJson 序列化。配置在初始化时用于创建 Provider，运行中仅修改设置不会重建它。

| 模式 | 实际存储 |
| --- | --- |
| PlayerPrefs | JSON 或加密后的 Base64 字符串；Save 后调用 `PlayerPrefs.Save()` |
| JsonFile | `Application.persistentDataPath/SaveData/<key>.json`；加密启用时内容是 Base64 密文 |
| BinaryFile | 同目录 `<key>.dat`；内容是 JSON 的 UTF-8 字节，或加密后的字节 |
| Custom | 自定义 `IDataProvider` 决定格式和存储位置 |

BinaryFile 使用上表中的 JSON 字节格式，不做压缩。

## 使用

以下片段在异步方法中执行：

```csharp
var data = LiteRuntime.Get<DataSystem>();
data.Save("playerLevel", 10);
int level = data.Load("playerLevel", defaultValue: 1);

await data.SaveAsync("playerName", "Alice");
string playerName = await data.LoadAsync("playerName", defaultValue: "Guest");
bool hasSave = data.Has("playerLevel");
```

- `Delete(key)` 删除单项；`DeleteAll()` 删除 Provider 覆盖的全部存储。
- PlayerPrefs 的 `DeleteAll()` 会删除该应用所有 PlayerPrefs；两个文件 Provider 的 `DeleteAll()` 都会删除整个 SaveData 目录，包括另一文件模式的数据。
- 内置 Provider 在键缺失或读取/反序列化失败时返回默认值，后者记录错误。保存异常仍可能传播，业务需决定重试、提示或备份策略。
- PlayerPrefs 的 Async 接口仍同步执行；文件 Async 接口使用异步文件 I/O，但序列化和加密仍在调用线程执行。

## 加密与迁移

当前 `SecurityUtils` 使用 AES-256-CBC、随机 IV 和 SHA-256 派生密钥。它未提供认证标签，且加密失败时会记录错误并返回输入；不要把开关当作完整的防篡改或防作弊保证。更换密钥或加密模式前需要迁移旧数据。

`SwitchProvider(newProvider)` 先初始化候选，成功后才替换并释放旧 Provider；初始化 false／异常时释放候选、保留旧 Provider，并向调用方报告异常。null 候选被拒绝，切换到当前实例不做操作。候选初始化期间，系统仍提供旧 Provider 的同步 API，不新增“切换期间不可用”状态。

调用方需在整个切换期间协调异步读写和其他切换：开始前等待旧异步 I/O，切换完成前不发起新的异步 I/O 或重叠切换；系统不替业务等待或串行调度这些工作。切换**不会迁移已有数据**，存档迁移需由业务安排。

文件模式的 key 直接参与文件路径拼接，应由业务生成固定、有效的文件名，避免传入目录分隔符或未经检查的外部输入。按业务时机保存，不要每帧写盘。

## 生命周期

系统调用从 Unity 主线程发起。关闭开始后拒绝全部存取、切换及 Provider 查询，抛 `ObjectDisposedException`。Provider 是借用引用，不应自行 Dispose；直接调用 Provider 的操作不经系统追踪，须在切换或关闭前自行等待。

Runtime 等待 `DisposeAsync()` 完成：不取消已接受的异步读写或切换，等待它们退出后才释放当前 Provider。已接受的切换若与关闭重叠，关闭会等待初始化退出再释放当前 Provider。Provider 不应反向等待本系统的关闭，否则会互相等待。

候选初始化可在后台完成，替换和释放会回到主线程。初始化失败时保留旧 Provider；发布成功后若旧 Provider 的 Dispose 抛异常，切换仍报告错误，但已发布的新 Provider 继续由系统持有，不撤销发布或自动重试旧 Provider 的释放。

关闭等待表示操作已结束，不保证写入成功；读写结果／异常仍由各调用返回的 UniTask 报告，业务应等待 SaveAsync 确认保存结果。Provider 的 Dispose 异常使关闭失败，不自动重试。自定义 Provider 的 UniTask 必须覆盖实际 I/O 寿命，Dispose 需支持部分初始化。这里不新增同 key 写入串行化、原子文件替换或断电持久化保证；必要保存应在应用退出前完成。

实现见 [DataSystem.cs](DataSystem.cs)、[Provider](Provider) 和 [DataSystemSetting.cs](DataSystemSetting.cs)。
