# ConfigSystem 配置表

ConfigSystem 属于 LiteQuark.Addons。`Lite > Config Tool` 将 Excel 导出为 JSON 和 C#。将 ConfigSystem 加入 Launcher 的 **System List**，然后通过 `LiteRuntime.Get<ConfigSystem>()` 注册、加载和查询配置表。

## 制作与导出

1. 设置 Excel Folder；工具递归读取 `.xlsx` / `.xls`，忽略 `~$` 临时文件。以 `#` 开头的 Sheet 不导出。
2. 设置 JSON Output、Code Output、Code Namespace 和 JSON Base Path。默认分别为 `Assets/StandaloneAssets/Configs`、`Assets/GamePlay/Config`、`GamePlay`、`Configs`。JSON Base Path 是生成代码使用的资源路径，须与 JSON 实际输出位置对应。
3. Refresh 后选择 Excel 文件并导出。变更检测以 Excel 文件为单位；普通导出跳过未变化的文件，强制导出重新输出所选文件。检查工具日志和 Unity 编译结果。
4. 每张 Sheet 输出一个 JSON；`Data/<表名PascalCase>Data.cs` 同时包含数据类及带 `ParseList` 的 Table 类，`ConfigSystemGenerated.cs` 提供统一注册入口。注册文件也包含未勾选文件中的表，使用前应确保相应数据和代码已导出。

设置保存在本机 EditorPrefs。Sheet 名及生成的类型名应全局唯一，字段名应能转换为有效且不冲突的 C# 属性名；不要依赖工具检查所有命名冲突。Sheet 删除或改名后需要处理旧输出。

**Clean 的删除范围：**JSON Output 目录直接包含的全部 `.json` 文件、Code Output 下整个 `Data` 目录，以及 `ConfigSystemGenerated.cs`。这些位置应专用于生成物，不放手写代码或其他数据。

## Excel 格式

固定保留前三行，数据从第四行开始。例如 Sheet 命名为 `people`：

| 行 | 第一列 | 第二列 | 第三列 |
| --- | --- | --- | --- |
| 1：字段名 | id | name | rewards |
| 2：类型 | int | string | int[] |
| 3：说明 | 编号 | 名称 | 奖励 |
| 4：数据 | 101 | 张三 | 1;2;3 |

- 第三行仅作说明，即使没有说明也要保留该行位置。
- `id` 列使用 int，值必须大于 0 且表内唯一；建议放在第一列。第一列为空或以 `#` 开头的数据行会被跳过。
- 字段名为空、以 `#` 开头，或缺少类型的列不会导出。
- 单元格文本会裁剪首尾空白；字符串引号不会自动去除。
- JSON 包含 `table`、`version` 和 `items`；数据属性使用 PascalCase，例如 `Id`、`Name`，记录按 id 排序。

| 类型写法 | 单元格示例与约定 |
| --- | --- |
| `int`、`long`、`float`、`double` | 数字，小数使用 `.` |
| `bool` | `true` / `false`、`1` / `0` |
| `string` | 文本 |
| `Vector2`、`Vector3`、`Vector4` | 分量以 `;` 分隔，例如 `1;2;3` |
| `Color` | `r;g;b;a`，例如 `1;0;0;1`；不自动把 0–255 转为 0–1 |
| `enum:MyEnum` | 填整数值；枚举类型须在生成代码中可见，不接受枚举名称单元格 |
| `int[]`、`long[]`、`float[]`、`double[]`、`bool[]`、`string[]` | 元素用 `;` 分隔 |
| `int[][]`、`long[][]`、`float[][]`、`double[][]`、`string[][]` | 行用 `\|`、元素用 `;`，例如 `1;2\|3;4` |
| `int[][][]` | 层用 `^`，例如 `1;2\|3;4^5;6\|7;8` |

空数组使用空单元格；不要写 `[]`。字符串数组没有分隔符转义规则。使用上表列出的数组类型，不将其他数组写法视为受支持类型。

## 注册与加载

示例使用 `LiteQuark.Runtime` 命名空间，放在 `IGameEntry.Initialize(CancellationToken ct)` 中。以下示例使用上面的表和默认 `GamePlay` 命名空间。

```csharp
var config = LiteRuntime.Get<ConfigSystem>();
GamePlay.ConfigSystemGenerated.Register(config);
await config.LoadAll(ct);
if (config.TryGetData<GamePlay.PeopleData>(101, out var person))
{
    LLog.Info("Name: {0}", person.Name);
}
```

每个系统实例中，一个行类型对应一张表。重复注册同一行类型会抛 `ArgumentException`。注册后显式调用 `LoadAll(ct)` 或 `Load<T>(ct)`。批量加载同时提交各表的读取请求，由 AssetSystem 控制并发。每张表在资源就绪后于 Unity 主线程解析并发布。

| API | 用途与结果 |
| --- | --- |
| `RegisterTable<T>(assetPath, parseFunc)` | 登记使用整数 Id 的文本表，T 实现 `IConfigData` |
| `RegisterTable<T, TKey>(assetPath, parseFunc, keySelector)` | 登记自定义主键表，解析函数接收 `TextAsset` 并返回 `List<T>`，主键选择器从行数据取得键 |
| `Load<T>(ct)` / `LoadAll(ct)` | 加载指定类型／全部已注册表。可传入启动 token，重复调用会重新加载和解析 |
| `GetData<T>(id)` | 未加载或找不到 id 时记录警告并返回 default |
| `TryGetData<T>(id, out data)` | 找到返回 true，否则返回 false；表未加载时也会记录警告 |
| `GetTable<T>()` / `GetTable<T, TKey>()` | 返回对应类型的表或 null。表提供 `Count`、`Get`、`TryGet`、`GetAll` |

### 自定义主键和二进制表

手写表可以用 `string` 作主键，或用元组组合多列。解析函数可以读取 `TextAsset.text`，也可以从以 `.bytes` 导入的资源读取 `TextAsset.bytes`。下面以业务定义的 `RewardRow` 和 `ParseRewards(byte[])` 为例。 [Unity TextAsset 手册](https://docs.unity3d.com/cn/2022.3/Manual/class-TextAsset.html)

```csharp
config.RegisterTable<RewardRow, (string Region, int Level)>(
    "Configs/rewards.bytes",
    asset => ParseRewards(asset.bytes),
    row => (row.Region, row.Level));
await config.Load<RewardRow>(ct);
var rewards = config.GetTable<RewardRow, (string Region, int Level)>();
if (rewards != null && rewards.TryGet(("sg", 10), out var reward))
{
    // 使用 reward。
}
```

资源路径遵循 AssetSystem 的规则，包含扩展名。随应用发布时将这些资源纳入资源构建。解析函数应生成独立的行数据。ConfigSystem 在解析期间持有 TextAsset Handle，解析结束时释放。

加载失败会记录日志。批次结束后可以查询各表结果，重载失败时保留该表的旧数据。`GetAll()` 返回表持有的列表，应按只读使用。系统关闭会清空表和注册信息。

## 生命周期

注册和加载从 Unity 主线程调用，已注册实例由 Runtime 统一释放。调用方取消 token 会结束该次加载，其他并发调用可继续。`DisposeAsync()` 取消本系统的在途 TextAsset Handle，并清空表和注册信息。取消的 `Load<T>(ct)` 或 `LoadAll(ct)` 以 `OperationCanceledException` 结束。

共享加载和原生资源收尾仍由 AssetSystem 负责，Config 不另行等待；普通资源／解析失败仍按原有日志和返回方式处理。

完整接口见 [ConfigSystem.cs](ConfigSystem.cs)、[ConfigTable.cs](ConfigTable.cs)；导出规则见 [Config Tool](../../Editor/Config)。
