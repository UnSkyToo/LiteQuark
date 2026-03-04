# 基于 Unity 的配置表系统设计文档（ConfigSystem）

> 目标：在 Unity 编辑器内，将 Excel 配置表导出为运行时可读取的数据文件（默认 Json），并自动生成对应 C# 数据结构与加载代码，提供统一的运行时访问接口 `ConfigSystem`。

## 1. 范围与目标

### 1.1 功能目标
- 在 Unity Editor 中提供 `EditorWindow` 工具：
  - 扫描指定 Excel 目录，列出可导出的 Sheet（以 Sheet 为配置表单位），以 #/~ 开头不导出。
  - 解析并校验 Excel 表数据（类型、必填、重复 id、格式等）。
  - 导出数据文件（默认 Json，预留 Binary 等扩展）。
  - 生成 C# 代码（Entry 数据类 + Table 容器/加载类 + 总注册文件）。
  - 支持增量导出：仅导出变更的 Sheet。

- 在运行时提供 `ConfigSystem`：
  - 统一 `Load`（异步）与 `GetData` 访问接口。
  - 表数据以 `id:int` 为主键管理，快速查询。

### 1.2 非目标（本期不做/可后续扩展）
- 不做 Excel 编辑能力（只读解析导出）。
- 不做在线热更下载（只负责本地资产加载，热更由上层系统实现）。
- 不做复杂关系型查询（如多字段索引），仅保证 id 主键查询；二级索引可扩展。

---

## 2. 总体架构

### 2.1 Editor 侧模块
- `ConfigToolWindow`：UI、设置、任务调度
- `ExcelReader`：跨平台读取 `.xlsx/.xls`（ExcelDataReader）
- `SheetParser`：把 Sheet 解析为统一中间结构 `TableSchema + RowData`
- `Validator`：校验规则（类型、id 唯一、必填、越界等）
- `Exporter`：
  - `JsonExporter`（默认，LitJson 序列化）
  - 预留 `BinaryExporter`
- `CodeGenerator`：生成 ConfigData/注册文件
- `ExportCache`：记录 hash，用于“变更自动勾选/增量导出”

### 2.2 Runtime 侧模块
- `IConfigData`：所有 ConfigData 的基接口（必须包含 `int Id {get;}`）
- `ConfigTable<T>`：通用表容器（Dictionary<int, T>）
- `ConfigSystem`：统一加载/获取入口
- `ConfigSystemGenerated`（自动生成）：注册所有表与资源路径映射（避免反射扫描）

---

## 3. Excel 配置表规范（强约束）

### 3.1 基本约定
- Excel 文件名不参与导出命名，仅作为组织方式。
- **导出单位为 Sheet**，Sheet 名称即表名（导出名）。
- Sheet 名必须全局唯一（跨所有 Excel 文件）。
- Sheet 命名规则（建议）：
  - 仅允许：`a-zA-Z0-9_`
  - 建议使用 `snake_case`（如 `people_data`）

### 3.2 表头行定义（固定三行）
1. **注释行（可选）**：第 1 行
   - 以 `#` 开头的单元格视为注释，仅用于说明，不参与导出。
2. **字段名行（必选）**：第 2 行
   - 示例：`id | name | age`
3. **类型行（必选）**：第 3 行
   - 示例：`int | string | int`

> 数据从第 4 行开始，直到遇到连续空行（可配置）或文件结束。

### 3.3 列规则
- 必须包含 `id` 列：
  - 字段名必须为 `id`（大小写不敏感，导出统一为 `Id`）
  - 类型必须为 `int`
  - `id` 必须唯一、>0
- 字段名为空的列将被忽略。
- 字段名以 `#` 开头：该列视为注释列，忽略导出（可选约定，建议支持）。

### 3.4 行规则
- id 必须是第一列
- 数据行第 1 列（id）为空：该行视为无效行，忽略或报错（建议可配置：默认报错）。
- 整行为空：忽略。
- 以 `#` 开头的 id 单元格：整行注释行，忽略（建议支持）。

---

## 4. 类型系统规范

### 4.1 基础类型（必须支持）
- `int`, `long`, `float`, `double`
- `bool`（支持 `true/false/0/1`）
- `string`（默认原样，建议自动 trim；支持 `'xxx'` 或 `"xxx"` 包裹时自动去引号）

### 4.2 Unity 常用类型（建议支持）
- `Vector2`：`x,y`
- `Vector3`：`x,y,z`
- `Vector4`：`x,y,z,w`
- `Color`：`r,g,b,a`（0~1 或 0~255 可配置）

### 4.3 Enum（建议支持）
- 类型行写法：`enum:MyEnum`
- 单元格内容支持：
  - 名称：`Idle`
  - 或数值：`1`
- 约束：`MyEnum` 必须在项目中可见（或可选：由工具生成 enum，后续扩展）。

### 4.4 数组类型（必须补充语法）
支持 1~3 维数组，类型写法：
- 1D：`int[]`
- 2D：`int[][]`
- 3D：`int[][][]`

单元格分隔符规范（建议）：
- 1D：使用 `;` 分隔
  - `1;2;3`
- 2D：行用 `|`，列用 `;`
  - `1;2|3;4`  => `[[1,2],[3,4]]`
- 3D：层用 `^`，行用 `|`，列用 `,`
  - `1;2|3;4 ^ 5;6|7;8`

空数组：
- 空单元格或 `[]` => 空数组（建议支持）
- 对 value type：空单元格也可视为默认值（可配置，但建议数组统一为空数组）

> 如果需要字符串数组包含分隔符，建议后续增加转义/引号规则；本期可先约束字符串数组不含 `|, ^`。

---

## 5. 导出物规范

### 5.1 输出目录（可在工具中配置）
- 数据文件输出（必须位于运行时可加载位置）：
  - `Assets/StandaloneAssets/Configs/`
- 代码输出：
  - `Assets/GamePlay/Config/`

### 5.2 文件命名
- 每个 Sheet 导出一个数据文件：
  - `{sheetName}.json`（或 `.bytes`，取决于导出格式）
- 生成代码：
  - `{SheetNamePascal}Data.cs`
  - `{SheetNamePascal}Table.cs`
- 统一注册文件：
  - `ConfigSystemGenerated.cs`

### 5.3 Json Schema（建议固定，便于扩展）
建议导出为对象结构（方便带元信息）：
```json
{
  "table": "people_data",
  "version": 1,
  "items": [
    { "id": 101, "name": "张三", "age": 18 },
    { "id": 102, "name": "李四", "age": 20 }
  ]
}
```

### 5.4 代码生成约定
- 字段命名：
  - Excel 字段名 `snake_case`/`lowerCamel` 自动转为 C# `PascalCase`（如 `item_id` -> `ItemId`）
  - `id` 强制生成 `Id`
- 生成内容：
  - `Data`：纯数据结构，实现 `IConfigData`
  - `Table`：负责从 Json 解析并构建 `Dictionary<int, Data>`
  - `ConfigSystemGenerated`：注册表名到加载器/路径，供 `ConfigSystem` 使用（避免运行时反射）

> 解析库：使用 **LitJson**。建议生成“显式解析代码”（不要反射），兼容 IL2CPP/AOT。

### 5.5 规则约定
- Json 输出 items 按 id 排序（保证 diff 稳定、hash 稳定）
- 数字格式统一用 InvariantCulture（避免不同地区小数点导致 hash 抖动）
- Sheet 被删/改名后，旧的 .cs/.json 要自动清理或提示清理。

---

## 6. EditorWindow 工具需求（补全细则）

### 6.1 配置项（需持久化到本地）
- Excel 根目录（支持文件夹递归扫描）
- 导出数据目录（默认 `Assets/StandaloneAssets/Configs/`）
- 导出代码目录（默认 `Assets/GamePlay/Config/`）
- 导出格式：Json / Binary（预留）
- MenuItem + -executeMethod 支持
- 解析选项：
  - 是否忽略空行
  - id 为空是报错还是跳过
  - 数组分隔符（高级设置，可选）

持久化方式：
- EditorPrefs（仅个人）

### 6.2 表列表展示
- 支持 ScrollView（大量表）
- 支持搜索（按表名/Excel 名）
- 显示字段：
  - 表名（Sheet）
  - 来源 Excel 文件
  - 上次导出时间
  - 变更状态（Changed/Unchanged）
  - 勾选导出
- 操作按钮：
  - 扫描/刷新
  - 全选/全不选/反选
  - 导出选中
  - 导出全部
  - 打开输出目录
  - 清理生成物（可选）

### 6.3 变更检测（“自动勾选”规则）
- 以 **Sheet 内容 hash** 为准（推荐 MD5/SHA1）：
  - hash 输入包含：字段名行、类型行、数据区所有单元格文本
- 将 `{sheetName -> hash}` 写入 `ExportCache`（json 文件或 ScriptableObject）
- 扫描时若 hash 变化：
  - 标记为 Changed
  - 自动勾选导出

### 6.4 校验与错误提示
必须校验并阻止导出：
- Sheet 名重复
- 缺少字段名行/类型行
- 缺少 id / id 非 int / id 重复 / id<=0
- 单元格类型转换失败（含数组/向量等）
- 字段名重复或生成后冲突（如 C# 关键字）

错误展示：
- 以表为单位汇总，支持定位到（行、列、字段名）

---

## 7. Runtime 运行时需求（补全 API 与行为）

### 7.1 资源加载约束
- 导出的数据必须位于：`Assets/StandaloneAssets/Configs/`
- 使用 `LiteRuntime.Asset.LoadAssetAsync` 读取

### 7.2 接口定义（建议）
```csharp
public interface IConfigData
{
    int Id { get; }
}

public static class ConfigSystem
{
    // 注册由 ConfigSystemGenerated 调用，由游戏启动流程显式调用一次
    public static void Register<T>(string tableName, string assetPath) where T : class, IConfigData;

    // 加载（可同步/异步二选一或都提供）
    public static UniTask LoadAll();
    public static UniTask Load<T>() where T : class, IConfigData;

    // 查询
    public static T GetData<T>(int id) where T : class, IConfigData;
    public static bool TryGetData<T>(int id, out T value) where T : class, IConfigData;
}
```

### 7.3 行为约定
- 重复 Load：
  - 默认不重复解析（有缓存），可提供 `Reload` 用于开发期调试。
- GetData 未加载：
  - 抛出明确异常或返回 null（建议提供 `TryGetData` 避免异常流控）。
- 找不到 id：
  - `GetData` 抛异常 / `TryGetData` 返回 false（建议）。

---

## 8. 扩展点设计（满足“支持更多格式/类型”）

### 8.1 导出格式扩展
定义接口：
- `IConfigFormatWriter`：中间结构 -> 文件（Json/Binary）
- `IConfigFormatReader`：文件 -> 运行时结构（Json/Binary）

### 8.2 类型解析扩展
- `ITypeConverter`：字符串/单元格值 -> 目标类型
  - 内置 converters：基础类型/Vector/Color/Enum/数组
  - 支持注册自定义类型（如 `FixedPoint`、`LangKey`）

### 8.3 代码生成模板化（建议）
- 使用简单模板（如 StringBuilder + 占位符）或 T4/自定义模板
- 生成代码尽量 stable（减少 git diff）

---

## 9. 验收标准（建议补充到需求）
- 支持在 Win/Mac Unity Editor 下读取 `.xlsx` 并导出成功（跨平台）。
- 任意新增 Sheet：
  - 一键导出生成对应 Json + C#，Unity 可编译通过。
- 运行时：
  - `ConfigSystem.LoadAll()` 后可 `GetData<PeopleData>(101)` 正确拿到数据。
- 校验：
  - 重复 id、类型错误等必须阻止导出并给出行列信息。
- 增量导出：
  - 未改动 Sheet 不会被自动勾选、不会重复生成文件（或生成但内容一致可选）。

---