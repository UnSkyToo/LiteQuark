# AudioSystem 音频管理系统 (Addon)

AudioSystem 是 LiteQuark.Addons 的音频管理模块，提供了简单易用的音频播放和管理功能。

## 功能特性

### ✅ 核心功能
- **音效/音乐分类**：自动区分Sound和Music
- **并发限制**：限制同时播放的音效数量
- **音量控制**：全局音量、分类音量
- **静音控制**：分类静音
- **循环播放**：支持循环和单次播放

### ✅ 设计特点
- **自动管理**：播放完毕自动清理
- **性能优化**：对象池复用AudioSource
- **简单API**：一行代码播放音频

---

## 快速开始

### 1. 播放音效

```csharp
// 播放音效
var soundId = LiteRuntime.Get<AudioSystem>().PlaySound("Audio/click.mp3");

// 带配置的音效
var soundId = LiteRuntime.Get<AudioSystem>().PlaySound(
    "Audio/explosion.mp3",
    isLoop: false,
    limit: 3,      // 最多同时3个
    volume: 0.8f,
    delay: 0.5f    // 延迟0.5秒播放
);
```

### 2. 播放背景音乐

```csharp
// 播放背景音乐（循环）
var musicId = LiteRuntime.Get<AudioSystem>().PlayMusic(
    "Audio/bgm.mp3",
    isLoop: true,
    volume: 0.6f,
    isOnly: true   // 停止其他音乐
);
```

---

## API 文档

### PlaySound
```csharp
public ulong PlaySound(string path, bool isLoop = false, int limit = 0, float volume = 1.0f, float delay = 0f, int order = 0)
```

播放音效。

**参数：**
- `path` - 音频文件路径
- `isLoop` - 是否循环
- `limit` - 并发数量限制（0为无限制）
- `volume` - 音量（0-1）
- `delay` - 延迟播放时间（秒）
- `order` - 播放优先级

**返回值：** 音频ID

**示例：**
```csharp
// 按钮点击音效
var clickId = LiteRuntime.Get<AudioSystem>().PlaySound("Audio/click.mp3", volume: 0.5f);

// 爆炸音效（限制同时3个）
var explosionId = LiteRuntime.Get<AudioSystem>().PlaySound("Audio/explosion.mp3", limit: 3);
```

### PlayMusic
```csharp
public ulong PlayMusic(string path, bool isLoop = true, float volume = 1.0f, bool isOnly = false, float delay = 0f, int order = 0)
```

播放背景音乐。

**参数：**
- `isOnly` - 是否停止其他音乐

**示例：**
```csharp
// 播放战斗音乐（停止之前的音乐）
var battleMusicId = LiteRuntime.Get<AudioSystem>().PlayMusic(
    "Audio/battle_theme.mp3",
    isLoop: true,
    volume: 0.7f,
    isOnly: true
);
```

### StopAudio
```csharp
public void StopAudio(ulong audioId)
```

停止指定音频。

**示例：**
```csharp
ulong musicId = LiteRuntime.Get<AudioSystem>().PlayMusic("Audio/bgm.mp3", isLoop: true);

// 5秒后停止
LiteRuntime.Timer.AddTimer(5.0f, () =>
{
    LiteRuntime.Get<AudioSystem>().StopAudio(musicId);
});
```

### 音量和静音控制

```csharp
// 设置全局音量
LiteRuntime.Get<AudioSystem>().SetAllAudioVolume(0.5f);

// 静音所有音效
LiteRuntime.Get<AudioSystem>().MuteAllSound(true);

// 静音所有音乐
LiteRuntime.Get<AudioSystem>().MuteAllMusic(true);

// 检查静音状态
bool isMuted = LiteRuntime.Get<AudioSystem>().IsMuteSound();
```

---

## 使用场景

### 1. UI音效

```csharp
public class UIButton : MonoBehaviour
{
    void OnClick()
    {
        // 播放点击音效
        LiteRuntime.Get<AudioSystem>().PlaySound("Audio/UI/click.mp3", volume: 0.8f);
    }
}
```

### 2. 背景音乐切换

```csharp
public class MusicManager
{
    private ulong currentMusicId;

    public void PlayMenuMusic()
    {
        StopCurrentMusic();
        currentMusicId = LiteRuntime.Get<AudioSystem>().PlayMusic(
            "Audio/menu_theme.mp3",
            isLoop: true,
            isOnly: true
        );
    }

    public void PlayBattleMusic()
    {
        StopCurrentMusic();
        currentMusicId = LiteRuntime.Get<AudioSystem>().PlayMusic(
            "Audio/battle_theme.mp3",
            isLoop: true,
            isOnly: true
        );
    }

    void StopCurrentMusic()
    {
        if (currentMusicId != 0)
        {
            LiteRuntime.Get<AudioSystem>().StopAudio(currentMusicId);
        }
    }
}
```

### 3. 游戏音效

```csharp
public class Player : MonoBehaviour
{
    public void Jump()
    {
        // 跳跃音效
        LiteRuntime.Get<AudioSystem>().PlaySound("Audio/jump.mp3");
    }

    public void TakeDamage()
    {
        // 受伤音效
        LiteRuntime.Get<AudioSystem>().PlaySound("Audio/hurt.mp3", volume: 0.9f);
    }

    void OnCollectCoin()
    {
        // 收集音效（限制同时5个）
        LiteRuntime.Get<AudioSystem>().PlaySound("Audio/coin.mp3", limit: 5);
    }
}
```

### 4. 设置面板

```csharp
public class SettingsUI : BaseUI
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle muteMusicToggle;
    [SerializeField] private Toggle muteSoundToggle;

    protected override void OnOpen(params object[] args)
    {
        var audioSystem = LiteRuntime.Get<AudioSystem>();

        // 初始化UI
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SfxVolume", 1.0f);

        // 绑定事件
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        muteMusicToggle.onValueChanged.AddListener(audioSystem.MuteAllMusic);
        muteSoundToggle.onValueChanged.AddListener(audioSystem.MuteAllSound);
    }

    void OnMusicVolumeChanged(float volume)
    {
        // 设置音乐音量
        LiteRuntime.Get<AudioSystem>().SetMusicVolume(volume);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    void OnSfxVolumeChanged(float volume)
    {
        // 设置音效音量
        LiteRuntime.Get<AudioSystem>().SetSoundVolume(volume);
        PlayerPrefs.SetFloat("SfxVolume", volume);
    }
}
```

---

## 最佳实践

### 1. 使用并发限制

```csharp
// 限制同时播放的枪声数量
LiteRuntime.Get<AudioSystem>().PlaySound("Audio/gunshot.mp3", limit: 5);
```

### 2. 区分音效和音乐

```csharp
// 音效：短促、单次、可能大量播放
PlaySound("Audio/footstep.mp3");

// 音乐：长时间、循环、通常只有一个
PlayMusic("Audio/bgm.mp3", isLoop: true, isOnly: true);
```

### 3. 保存用户音量设置

```csharp
void SaveAudioSettings()
{
    var audioSystem = LiteRuntime.Get<AudioSystem>();
    PlayerPrefs.SetFloat("MusicVolume", audioSystem.GetMusicVolume());
    PlayerPrefs.SetFloat("SfxVolume", audioSystem.GetSoundVolume());
    PlayerPrefs.SetInt("MuteMusic", audioSystem.IsMuteMusic() ? 1 : 0);
    PlayerPrefs.SetInt("MuteSound", audioSystem.IsMuteSound() ? 1 : 0);
}
```

---
