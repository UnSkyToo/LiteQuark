# LiteQuark.Addons

LiteQuark 的可选系统包，需要先安装核心包。版本、最低 Unity 声明和核心依赖见 [package.json](package.json)，升级说明见 [CHANGELOG](CHANGELOG.md)。这些版本声明不代表所有旧编辑器均经过兼容验证。

## 安装与接入

可将本目录连同 `.meta` 文件复制到 Unity 项目，也可在 Package Manager 使用发布分支：

```text
https://github.com/UnSkyToo/LiteQuark.git#LiteQuark.Addons
```

核心包发布分支为 `#LiteQuark`。可复现安装应选择核对过的 tag 或 commit；安装前检查 Addons 声明的核心依赖是否与所选核心版本匹配。

扩展系统不会自动注册。将需要的系统加入 LiteLauncher 的 `SystemList`，配置对应 `SystemSettings`，分别检查 Debug / Release 设置。在业务 `ILogic.Initialize()` 或之后通过 `LiteRuntime.Get<T>()` 获取已初始化的系统。

已注册扩展系统由 Runtime 统一调用 Initialize 和 DisposeAsync；业务不要直接释放系统或在释放后继续使用旧实例。DisposeAsync 采用统一的可等待接口，同步清理可返回 `UniTask.CompletedTask`；它不额外保证每个模块的在途请求或原生操作已经结束，具体边界以模块指南为准。

| 模块 | 能力与使用说明 |
| --- | --- |
| [UI](Runtime/UI/README.md) | 界面打开、层级、事件与资源清理 |
| [Audio](Runtime/Audio/README.md) | 音效、音乐、音量和停止 |
| [Effect](Runtime/Effect/README.md) | 特效配置、播放与延迟回收 |
| [Network](Runtime/Network/README.md) | HTTP、显式重试、上传下载与取消 |
| [Data](Runtime/Data/README.md) | 存档 Provider、保存、读取与迁移 |
| [Config](Runtime/Config/README.md) | Excel 导出、配置表注册、加载与查询 |

按需阅读模块指南后再接入；资源释放、取消和回调行为以对应模块契约为准。
