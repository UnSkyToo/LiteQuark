using System;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

/// <summary>
/// DataSystem使用示例
/// 演示了数据持久化的各种用法
/// </summary>
public class DataSystemExample : MonoBehaviour
{
    [Serializable]
    public class PlayerData
    {
        public string playerName;
        public int level;
        public int coins;
        public float[] position;
    }

    [Serializable]
    public class GameSettings
    {
        public float musicVolume;
        public float sfxVolume;
        public int graphicsQuality;
        public bool fullscreen;
    }

    // ============================================
    // 1. 基础保存和加载
    // ============================================
    public void Example_BasicSaveLoad()
    {
        // 保存数据
        var playerData = new PlayerData
        {
            playerName = "Player1",
            level = 10,
            coins = 1000,
            position = new float[] { 1.5f, 2.3f, 4.8f }
        };

        LiteRuntime.Get<DataSystem>().Save("player_data", playerData);
        Debug.Log("数据已保存");

        // 加载数据
        var loadedData = LiteRuntime.Get<DataSystem>().Load<PlayerData>("player_data");
        if (loadedData != null)
        {
            Debug.Log($"玩家名称：{loadedData.playerName}");
            Debug.Log($"等级：{loadedData.level}");
            Debug.Log($"金币：{loadedData.coins}");
        }
    }

    // ============================================
    // 2. 带默认值的加载
    // ============================================
    public void Example_LoadWithDefault()
    {
        // 如果键不存在，返回默认值
        var defaultSettings = new GameSettings
        {
            musicVolume = 0.8f,
            sfxVolume = 1.0f,
            graphicsQuality = 2,
            fullscreen = true
        };

        var settings = LiteRuntime.Get<DataSystem>().Load("game_settings", defaultSettings);
        Debug.Log($"音乐音量：{settings.musicVolume}");
        Debug.Log($"音效音量：{settings.sfxVolume}");
    }

    // ============================================
    // 3. 异步保存和加载
    // ============================================
    public async void Example_AsyncSaveLoad()
    {
        var playerData = new PlayerData
        {
            playerName = "AsyncPlayer",
            level = 20,
            coins = 5000,
            position = new float[] { 10f, 20f, 30f }
        };

        // 异步保存
        await LiteRuntime.Get<DataSystem>().SaveAsync("async_player", playerData);
        Debug.Log("异步保存完成");

        // 异步加载
        var loadedData = await LiteRuntime.Get<DataSystem>().LoadAsync<PlayerData>("async_player");
        Debug.Log($"异步加载完成：{loadedData.playerName}");
    }

    // ============================================
    // 4. 检查键是否存在
    // ============================================
    public void Example_CheckKeyExists()
    {
        if (LiteRuntime.Get<DataSystem>().Has("player_data"))
        {
            Debug.Log("玩家数据存在");
        }
        else
        {
            Debug.Log("玩家数据不存在，使用默认值");
        }
    }

    // ============================================
    // 5. 删除数据
    // ============================================
    public void Example_DeleteData()
    {
        // 删除单个键
        LiteRuntime.Get<DataSystem>().Delete("old_data");
        Debug.Log("旧数据已删除");

        // 删除所有数据（慎用！）
        // LiteRuntime.Get<DataSystem>().DeleteAll();
    }

    // ============================================
    // 6. 保存简单类型
    // ============================================
    public void Example_SaveSimpleTypes()
    {
        // 保存基础类型需要包装在类中
        LiteRuntime.Get<DataSystem>().Save("high_score", new { score = 99999 });
        LiteRuntime.Get<DataSystem>().Save("player_name", new { name = "Champion" });

        // 或者使用Dictionary
        var userData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "highScore", 99999 },
            { "lastLogin", DateTime.Now.ToString() }
        };
        LiteRuntime.Get<DataSystem>().Save("user_data", userData);
    }

    // ============================================
    // 7. 存档槽管理
    // ============================================
    public void Example_SaveSlots()
    {
        // 创建3个存档槽
        for (int i = 1; i <= 3; i++)
        {
            var slotData = new PlayerData
            {
                playerName = $"Player{i}",
                level = i * 5,
                coins = i * 100,
                position = new float[] { i, i * 2, i * 3 }
            };

            LiteRuntime.Get<DataSystem>().Save($"save_slot_{i}", slotData);
        }

        Debug.Log("3个存档槽已创建");

        // 加载特定存档槽
        var slot2 = LiteRuntime.Get<DataSystem>().Load<PlayerData>("save_slot_2");
        Debug.Log($"存档槽2：{slot2.playerName}, Level {slot2.level}");
    }

    // ============================================
    // 8. 数据迁移示例
    // ============================================
    public void Example_DataMigration()
    {
        // 检查旧版本数据
        if (LiteRuntime.Get<DataSystem>().Has("old_player_data"))
        {
            // 加载旧数据
            var oldData = LiteRuntime.Get<DataSystem>().Load<PlayerData>("old_player_data");

            // 迁移到新格式（这里简化处理）
            var newData = new PlayerData
            {
                playerName = oldData.playerName,
                level = oldData.level,
                coins = oldData.coins * 2, // 假设新版本金币翻倍
                position = oldData.position
            };

            // 保存新数据
            LiteRuntime.Get<DataSystem>().Save("player_data", newData);

            // 删除旧数据
            LiteRuntime.Get<DataSystem>().Delete("old_player_data");

            Debug.Log("数据迁移完成");
        }
    }

    // ============================================
    // 9. 自动保存示例
    // ============================================
    private PlayerData currentPlayerData;

    void Start()
    {
        // 启动时加载
        currentPlayerData = LiteRuntime.Get<DataSystem>().Load("auto_save", new PlayerData
        {
            playerName = "NewPlayer",
            level = 1,
            coins = 0,
            position = new float[] { 0, 0, 0 }
        });

        // 每30秒自动保存
        InvokeRepeating(nameof(AutoSave), 30f, 30f);
    }

    void AutoSave()
    {
        LiteRuntime.Get<DataSystem>().Save("auto_save", currentPlayerData);
        Debug.Log("自动保存完成");
    }

    void OnApplicationQuit()
    {
        // 退出时保存
        LiteRuntime.Get<DataSystem>().Save("auto_save", currentPlayerData);
    }

    // ============================================
    // 10. 配置数据管理
    // ============================================
    public class ConfigManager
    {
        private const string CONFIG_KEY = "game_config";
        private GameSettings currentSettings;

        public void Initialize()
        {
            // 加载配置，如果不存在则使用默认值
            currentSettings = LiteRuntime.Get<DataSystem>().Load(CONFIG_KEY, new GameSettings
            {
                musicVolume = 0.8f,
                sfxVolume = 1.0f,
                graphicsQuality = 2,
                fullscreen = true
            });
        }

        public void SetMusicVolume(float volume)
        {
            currentSettings.musicVolume = volume;
            SaveConfig();
        }

        public void SetGraphicsQuality(int quality)
        {
            currentSettings.graphicsQuality = quality;
            SaveConfig();
        }

        private void SaveConfig()
        {
            LiteRuntime.Get<DataSystem>().Save(CONFIG_KEY, currentSettings);
        }

        public GameSettings GetCurrentSettings()
        {
            return currentSettings;
        }
    }
}
