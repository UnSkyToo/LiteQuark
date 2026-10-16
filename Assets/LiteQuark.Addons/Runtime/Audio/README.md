# AudioSystem 音频

AudioSystem 属于 LiteQuark.Addons。先在 Launcher 的 `SystemList` 注册，再通过 `LiteRuntime.Get<AudioSystem>()` 获取；它依赖内置 AssetSystem 和 ObjectPoolSystem。

## 播放

在框架和扩展系统初始化后调用：

```csharp
var audio = LiteRuntime.Get<AudioSystem>();
ulong soundId = audio.PlaySound("Audio/Click.wav", limit: 3, volume: 0.8f);
ulong musicId = audio.PlayMusic("Audio/Main.ogg", isLoop: true, volume: 0.5f);
// 不再需要循环音乐时：
audio.StopAudio(musicId);
```

资源路径相对 `Assets/StandaloneAssets`，包含扩展名。需要场景中有可用的 AudioListener。

| 接口 | 参数与行为 |
| --- | --- |
| `PlaySound(path, isLoop = false, limit = 0, volume = 1f, delay = 0f)` | 音效；`limit` 限制相同路径的活动音频数，0 表示不限；delay 单位为秒 |
| `PlayMusic(path, isLoop = true, volume = 1f, isOnly = true)` | 音乐；默认先停止其他音乐 |
| `GetAudio(id)` | 返回 `AudioObject` 或 null |
| `StopAudio(id)` | 停止指定音频并加入清理队列 |
| `StopAllSound()` / `StopAllMusic()` | 停止分类下所有音频 |

ID 为 `ulong`；触及并发限制时返回 0。非零 ID 表示请求已建立，资源仍可能处于加载中，不保证已成功播放。操作 `AudioObject.Source` 前检查 `IsLoaded` / `IsValid()`。

## 音量、静音与暂停

- `MuteAllSound(bool)`、`MuteAllMusic(bool)`、`MuteAllAudio(bool)` 会保存分类静音状态，并应用于后续加载的音频。
- `SetAllAudioVolume(float)` 修改当前已加载音频的音量，不保存全局默认值；仍在加载中或后续创建的音频使用播放调用的 volume 参数。
- 单个音频可使用 `GetAudio(id)?.SetVolume(volume)`；当前没有分类音量设置接口或播放优先级 order 参数。
- `PauseAllAudio()` / `ResumeAllAudio()` 同时控制 `AudioListener.pause` 与已有 AudioSource 的暂停/恢复。当前自动回收依据 `!Source.isPlaying`，若暂停期间 AudioSystem 继续 Tick，暂停音频可能被回收；需要可靠暂停时应先验证该场景。

## 生命周期

AudioObject 持有音频 AssetHandle，系统清理时释放 Handle 并归还承载对象。循环音频由业务保存 ID 并主动停止；已结束音频在系统 Tick 中清理。停止后不要继续使用缓存的 AudioObject 或 Source 引用。

实现见 [AudioSystem.cs](AudioSystem.cs) 和 [AudioObject.cs](AudioObject.cs)。
