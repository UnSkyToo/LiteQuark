# LiteQuark.Addons

LiteQuark 的可选系统包。先安装 [package.json](package.json) 所要求的核心版本；升级见 [CHANGELOG](CHANGELOG.md)。

## 安装与接入

最低 Unity 版本为 **2022.3.62f3**。将本目录连同 `.meta` 文件复制到项目，并启用 uGUI（`com.unity.ugui`）；也可在 Package Manager 使用 `https://github.com/UnSkyToo/LiteQuark.git#LiteQuark.Addons`。核心分支为 `#LiteQuark`；可复现安装应固定已核对的 tag 或 commit。

在 Launcher 的 **System List > +** 注册所需系统并配置条目，再于 `IGameEntry.Initialize` 或之后用 `LiteRuntime.Get<T>()` 获取。关闭时停止提交新工作，重启后重新获取实例；释放、取消和回调的边界见各模块指南。

| 模块 | 能力与使用说明 |
| --- | --- |
| [UI](Runtime/UI/README.md) | 界面打开、层级、事件与资源清理 |
| [Audio](Runtime/Audio/README.md) | 音效、音乐、音量和停止 |
| [Effect](Runtime/Effect/README.md) | 特效配置、播放与延迟回收 |
| [Network](Runtime/Network/README.md) | HTTP、显式重试、上传下载与取消 |
| [Data](Runtime/Data/README.md) | 存档 Provider、保存、读取与迁移 |
| [Config](Runtime/Config/README.md) | Excel 导出、配置表注册、加载与查询 |

## 许可证

LiteQuark.Addons 使用 [MIT 许可证](LICENSE.md)。随包组件的许可见[第三方许可声明](Third%20Party%20Notices.md)。
