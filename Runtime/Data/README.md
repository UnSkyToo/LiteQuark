# DataSystem 数据持久化系统

DataSystem 是 LiteQuark 框架的数据持久化模块，提供了简单易用的存档管理功能。

## 功能特性

### ✅ 核心功能
- **多种存储模式**：PlayerPrefs / JSON文件 / 二进制文件
- **数据加密**：AES加密防止玩家修改存档
- **同步/异步API**：支持同步和异步保存/加载
- **类型安全**：泛型支持，编译时类型检查
- **简单易用**：一行代码完成保存/加载

### ✅ 设计特点
- **零依赖**：不依赖第三方库
- **自动序列化**：使用LitJson自动处理JSON序列化
- **灵活扩展**：可自定义IDataProvider实现

---

## 快速开始

### 1. 配置数据设置

在 LiteSetting 中配置数据存储参数：

```csharp
数据设置:
├── 存储模式: JsonFile
├── 启用加密: true
└── 加密密钥: YourGameEncryptionKey2024
```

### 2. 保存和加载数据

```csharp
// 定义数据结构
[Serializable]
public class PlayerData
{
    public string playerName;
    public int level;
    public int coins;
}

// 保存数据
var playerData = new PlayerData { playerName = "Player1", level = 10, coins = 1000 };
LiteRuntime.Data.Save("player_data", playerData);

// 加载数据
var loadedData = LiteRuntime.Data.Load<PlayerData>("player_data");
Debug.Log($"玩家等级：{loadedData.level}");
```

---

## 存储模式对比

| 模式 | 优点 | 缺点 | 适用场景 |
|------|------|------|----------|
| **PlayerPrefs** | 使用简单，跨平台 | 容量小，性能一般 | 少量配置数据（音量、画质等） |
| **JsonFile** | 可读性好，易调试 | 文件较大 | 结构化数据，需要查看存档内容 |
| **BinaryFile** | 文件小，性能最好 | 不可读，难调试 | 大量数据，生产环境 |

---

## API 文档

### 同步API

#### Save<T>
```csharp
public void Save<T>(string key, T data)
```
保存数据到存储中。

**参数：**
- `key` - 数据键（唯一标识）
- `data` - 要保存的数据对象

**示例：**
```csharp
var playerData = new PlayerData { level = 10 };
LiteRuntime.Data.Save("player", playerData);
```

#### Load<T>
```csharp
public T Load<T>(string key, T defaultValue = default)
```
从存储中加载数据。

**参数：**
- `key` - 数据键
- `defaultValue` - 如果键不存在，返回此默认值

**返回值：** 加载的数据对象，或默认值

**示例：**
```csharp
var defaultData = new PlayerData { level = 1 };
var playerData = LiteRuntime.Data.Load("player", defaultData);
```

#### Has
```csharp
public bool Has(string key)
```
检查指定键是否存在。

**示例：**
```csharp
if (LiteRuntime.Data.Has("player_data")) {
    // 数据存在
}
```

#### Delete
```csharp
public void Delete(string key)
```
删除指定键的数据。

**示例：**
```csharp
LiteRuntime.Data.Delete("old_data");
```

#### DeleteAll
```csharp
public void DeleteAll()
```
删除所有数据（慎用！）。

---

### 异步API

#### SaveAsync<T>
```csharp
public async UniTask SaveAsync<T>(string key, T data)
```
异步保存数据。

**示例：**
```csharp
await LiteRuntime.Data.SaveAsync("player", playerData);
Debug.Log("保存完成");
```

#### LoadAsync<T>
```csharp
public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
```
异步加载数据。

**示例：**
```csharp
var playerData = await LiteRuntime.Data.LoadAsync<PlayerData>("player");
```

---

## 使用场景

### 1. 玩家存档
```csharp
[Serializable]
public class SaveData
{
    public int level;
    public float[] position;
    public int[] inventory;
    public string lastSaveTime;
}

// 保存
var saveData = new SaveData
{
    level = player.level,
    position = new float[] { player.x, player.y, player.z },
    inventory = player.inventory.ToArray(),
    lastSaveTime = DateTime.Now.ToString()
};
LiteRuntime.Data.Save("save_slot_1", saveData);

// 加载
var loadedSave = LiteRuntime.Data.Load<SaveData>("save_slot_1");
```

### 2. 游戏设置
```csharp
[Serializable]
public class GameSettings
{
    public float musicVolume = 0.8f;
    public float sfxVolume = 1.0f;
    public int quality = 2;
    public bool fullscreen = true;
}

// 初始化设置
var settings = LiteRuntime.Data.Load("settings", new GameSettings());

// 修改设置
settings.musicVolume = 0.5f;
LiteRuntime.Data.Save("settings", settings);
```

### 3. 多存档槽管理
```csharp
public class SaveSlotManager
{
    private const string SLOT_PREFIX = "save_slot_";
    private const int MAX_SLOTS = 3;

    public void SaveToSlot(int slotIndex, PlayerData data)
    {
        if (slotIndex < 1 || slotIndex > MAX_SLOTS)
            return;

        LiteRuntime.Data.Save($"{SLOT_PREFIX}{slotIndex}", data);
    }

    public PlayerData LoadFromSlot(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > MAX_SLOTS)
            return null;

        return LiteRuntime.Data.Load<PlayerData>($"{SLOT_PREFIX}{slotIndex}");
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        return !LiteRuntime.Data.Has($"{SLOT_PREFIX}{slotIndex}");
    }

    public void DeleteSlot(int slotIndex)
    {
        LiteRuntime.Data.Delete($"{SLOT_PREFIX}{slotIndex}");
    }
}
```

### 4. 自动保存
```csharp
public class AutoSaveManager : MonoBehaviour
{
    private const float SAVE_INTERVAL = 60f; // 每60秒保存一次
    private float lastSaveTime;

    void Update()
    {
        if (Time.time - lastSaveTime >= SAVE_INTERVAL)
        {
            AutoSave();
            lastSaveTime = Time.time;
        }
    }

    void AutoSave()
    {
        var gameState = GameManager.Instance.GetCurrentState();
        LiteRuntime.Data.Save("auto_save", gameState);
        Debug.Log("自动保存完成");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            AutoSave(); // 切换到后台时保存
        }
    }
}
```

---

## 数据加密

### 启用加密

在 LiteSetting 中：
```csharp
数据设置:
├── 启用加密: ✓
└── 加密密钥: MySecretKey2024
```

### 加密说明

- **算法**：AES-256对称加密
- **密钥派生**：使用SHA256从字符串密钥派生固定长度密钥
- **安全性**：每次加密使用随机IV，即使相同数据加密结果也不同
- **性能影响**：极小，几乎无感知

### 自定义加密密钥

```csharp
// 方式1：在LiteSetting中配置（推荐）
LiteSetting.Data.EncryptionKey = "YourCustomKey";

// 方式2：运行时修改（不推荐，会导致旧数据无法解密）
// 仅在初始化前修改
```

**注意事项：**
1. 每个游戏使用不同的加密密钥
2. 密钥修改后旧数据将无法解密
3. 不要将密钥硬编码在代码中（使用混淆或服务器下发）

---

## 数据迁移

### 版本管理

```csharp
[Serializable]
public class SaveData
{
    public int version = 1; // 数据版本号
    public PlayerData playerData;
}

public class DataMigration
{
    private const string SAVE_KEY = "game_save";
    private const int CURRENT_VERSION = 2;

    public SaveData LoadAndMigrate()
    {
        var saveData = LiteRuntime.Data.Load<SaveData>(SAVE_KEY);

        if (saveData == null)
        {
            return new SaveData { version = CURRENT_VERSION };
        }

        // 迁移旧版本数据
        if (saveData.version < CURRENT_VERSION)
        {
            saveData = MigrateToVersion2(saveData);
            saveData.version = CURRENT_VERSION;
            LiteRuntime.Data.Save(SAVE_KEY, saveData);
        }

        return saveData;
    }

    private SaveData MigrateToVersion2(SaveData oldData)
    {
        // 执行迁移逻辑
        // 例如：添加新字段，转换旧字段等
        return oldData;
    }
}
```

---

## 配置说明

### LiteSetting 配置项

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| ProviderMode | Enum | JsonFile | 存储模式（PlayerPrefs/JsonFile/BinaryFile） |
| EnableEncryption | bool | true | 是否启用数据加密 |
| EncryptionKey | string | "YourGameEncryptionKey2024" | 加密密钥 |

### 存储路径

- **PlayerPrefs**: Unity内置，平台相关
  - Windows: 注册表 `HKCU\Software\[CompanyName]\[ProductName]`
  - Android: SharedPreferences
  - iOS: NSUserDefaults

- **JsonFile / BinaryFile**: `Application.persistentDataPath/SaveData/`
  - Windows: `C:\Users\[Username]\AppData\LocalLow\[CompanyName]\[ProductName]\SaveData\`
  - Android: `/storage/emulated/0/Android/data/[PackageName]/files/SaveData/`
  - iOS: `/var/mobile/Containers/Data/Application/[GUID]/Documents/SaveData/`

---

## 高级用法

### 自定义Provider

实现 `IDataProvider` 接口可以自定义存储方式：

```csharp
public class CloudSaveProvider : IDataProvider
{
    public async UniTask<bool> Initialize()
    {
        // 连接云存储服务
        return true;
    }

    public void Save<T>(string key, T data)
    {
        // 上传到云端
    }

    public T Load<T>(string key, T defaultValue = default)
    {
        // 从云端下载
        return defaultValue;
    }

    // ... 实现其他方法
}

// 使用自定义Provider
var cloudProvider = new CloudSaveProvider();
await LiteRuntime.Data.SwitchProvider(cloudProvider);
```

---

## 常见问题

### Q: 数据会自动保存吗？
**A:** 不会，需要手动调用 `Save()` 方法。建议在关键时刻保存：
- 玩家操作后（购买、升级等）
- 定时自动保存（每分钟）
- 应用暂停/退出时

### Q: 如何防止数据丢失？
**A:** 实现双重保存机制：
```csharp
// 先保存到临时文件
LiteRuntime.Data.Save("player_data_temp", data);
// 验证成功后覆盖正式文件
LiteRuntime.Data.Save("player_data", data);
```

### Q: 数据加密安全吗？
**A:** AES-256加密足以防止普通玩家修改，但无法防止专业破解。完全防作弊需要服务器验证。

### Q: 如何备份玩家数据？
**A:** 文件模式下直接复制存档文件夹；PlayerPrefs需要导出为文件：
```csharp
var allData = LiteRuntime.Data.Load<Dictionary<string, object>>("all_saves");
File.WriteAllText("backup.json", JsonUtility.ToJson(allData));
```

### Q: 不同存储模式可以共存吗？
**A:** 不能自动共存。切换模式需要手动迁移数据：
```csharp
// 从PlayerPrefs迁移到JsonFile
LiteRuntime.Setting.Data.ProviderMode = DataProviderMode.PlayerPrefs;
var data = LiteRuntime.Data.Load<PlayerData>("player");

LiteRuntime.Setting.Data.ProviderMode = DataProviderMode.JsonFile;
LiteRuntime.Data.Save("player", data);
```

---

## 性能建议

1. **减少保存频率**：不要每帧保存，合并多次修改后统一保存
2. **使用异步API**：大数据保存时使用 `SaveAsync` 避免卡顿
3. **分模块存储**：将大对象拆分为多个小对象，按需加载
4. **缓存加载数据**：避免重复加载同一数据

```csharp
// 不好的做法
void Update() {
    LiteRuntime.Data.Save("player", currentData); // 每帧保存
}

// 好的做法
void OnPlayerLevelUp() {
    currentData.level++;
    LiteRuntime.Data.Save("player", currentData); // 关键时刻保存
}
```

---

## 最佳实践

### 1. 定义清晰的数据结构
```csharp
[Serializable]
public class GameSave
{
    public int version = 1;
    public PlayerData player;
    public WorldData world;
    public QuestData quests;
    public DateTime saveTime;
}
```

### 2. 使用常量管理Key
```csharp
public static class SaveKeys
{
    public const string PLAYER_DATA = "player_data";
    public const string GAME_SETTINGS = "game_settings";
    public const string ACHIEVEMENTS = "achievements";
}

// 使用
LiteRuntime.Data.Save(SaveKeys.PLAYER_DATA, playerData);
```

### 3. 封装存档管理类
```csharp
public class SaveManager
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance ??= new SaveManager();

    private GameSave currentSave;

    public void Initialize()
    {
        currentSave = LiteRuntime.Data.Load<GameSave>("current_save") ?? CreateNewSave();
    }

    public void Save()
    {
        currentSave.saveTime = DateTime.Now;
        LiteRuntime.Data.Save("current_save", currentSave);
    }

    private GameSave CreateNewSave()
    {
        return new GameSave { version = 1, player = new PlayerData() };
    }
}
```

---

