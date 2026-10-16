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

BinaryFile 没有专用二进制对象编码或压缩，不能据此保证体积更小、性能最好。

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

`SwitchProvider(newProvider)` 释放旧 Provider 并初始化新 Provider，**不会迁移已有数据**。业务应先读取旧数据，再切换并保存；存档结构升级也需自行维护版本和迁移逻辑。

文件模式的 key 直接参与文件路径拼接，应由业务生成固定、有效的文件名，避免传入目录分隔符或未经检查的外部输入。按业务时机保存，不要每帧写盘。

实现见 [DataSystem.cs](DataSystem.cs)、[Provider](Provider) 和 [DataSystemSetting.cs](DataSystemSetting.cs)。
