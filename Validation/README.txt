Validation — LiteQuark 隔离验证

运行
  从仓库根目录运行。Windows 使用 PowerShell 7，macOS 使用系统 Bash。
    & .\Validation\Run-Validation.ps1
    & .\Validation\Run-Validation.ps1 -Suite paths,manifest,collector
    bash ./Validation/Run-Validation.sh
    bash ./Validation/Run-Validation.sh --suite paths,manifest,collector

  默认执行全部 suite。paths 和 manifest 同时选中时共用一个 Edit Mode Unity 进程，
  也可单独选跑。其他 suite 各自启动 Unity。
  指定 Editor 用 -UnityEditor / --unity-editor。准备阶段超时用
  -PreparationTimeoutSeconds / --preparation-timeout-seconds（默认 1800 秒），
  每个 suite 进程超时用 -TimeoutSeconds / --timeout-seconds（默认 600 秒）；共用进程共用该时限。
  macOS 可用 --help 查看参数，另需 shasum、lsof 和 osascript。

环境
  需要 Git、ProjectSettings/ProjectVersion.txt 指定的 Unity（2022.3.62f3）、可用许可和
  本机平台的 Bundle 构建支持。Windows 构建 Win64，macOS 构建 OSXUniversal。
  默认从 Unity Hub 标准安装目录定位 Editor。首次恢复依赖需要访问主工程声明的包源。
  运行前关闭隔离工程的编辑器；验证目录须可写，系统允许回环 TCP。
  macOS 入口拒绝文件名中的制表符和换行。异常中断残留 .validation.lock 时，
  先确认没有进程占用，再处理本次遗留锁。

主要覆盖
  Scenarios.json 同时记录套件入口和预期场景 ID；在 Sources 下搜索完整的 suite/id 可定位验证源码。

  paths      宿主路径与 URI、过滤复制和文件改名。
  manifest   资源身份、依赖 ID、重复导出与 Hash/Flat 文件名。
  collector  收集、分包冲突、Viewer 数据、Play Mode 快照和真实 UPM 依赖构建。
  tasks      Core Play Mode 验证，包括任务、Timer、Action、Event、对象池、Launcher、日志和 Runtime 生命周期。
             包含 Demo 退场及 LiteBattle 的同帧相机和 HUD 验证。
  loading    寻址、后端选择、清单启动与取消、Remote 回环下载和原生场景限制。
  lifecycle  资源 Handle、依赖、缓存、场景所有权、关闭与重载，以及优先级和并发额度。
  addons     UI、Audio、Effect、HTTP、Data 和 Config；含真实 Bundle 下 UI 跨重启重开。

判定与边界
  Unity 侧用同一份判定逻辑比对预期与实际 ID；缺失、重复、意外 ID 或清理失败均阻断验证。
  两端入口只接收已完成的结果，编译失败、Unity 版本不符或异常退出也会失败。
  日常回归、已知限制和独立诊断分别计数。唯一允许的限制是 UNITY-SAME-NAME-SCENE-BUNDLES：
  loading/same-name-limit 须记录正确场景的路径与内容、指定 Bundle 冲突、错误及证据，且 passed=false；
  loading/same-name-native 须独立确认两包各自可加载，且正反顺序均重现指定冲突。
  额外错误、未确认的诊断和泄漏均阻断验证。

  原生卸载检查实际 Bundle / Scene；Simulate 不验证原生包释放。真实网络限回环 HTTP，
  URI、Player 模式和树数据检查不代表移动端实机、远端部署或 Editor 窗口渲染验证。
  即时重启仅覆盖已有用例，具体加载状态和模块组合见对应源码。

隔离与输出
  验证使用当前工作区（含未提交源码）及 Sources，在本目录的 Project 中运行；
  入口校验主工程、Git 状态、验证输入、生成代码和隔离 Packages 文件集合。隔离维护细节见 AGENTS.md。

  Reports/verification-report.html    两端由 Report.html 生成的本次运行汇总与日志链接
  Reports/summary.json                数量、分类、准备及隔离结果
  Reports/<suite>/results.json         ID、状态、断言数、失败、耗时和证据
  Reports/<suite>/unity.log           Unity 进程原始日志，共用进程的套件各保留同一份日志
  Reports/prepare.log                 包恢复、导入与编译日志
  Reports/source-state.json           基准提交、宿主平台、构建目标及指纹
  Reports/source-hashes.csv           工作区文件指纹

  共用进程的套件各自判定，退出码和进程耗时相同。全量运行替换 Reports；专项只替换所选
  suite，汇总只代表本轮。普通场景失败仍汇总其他场景；编译、启动或报告缺失可提前终止。
  退出码 0 要求本轮日常回归、限制及诊断分类和隔离检查均通过。平台结果以实际宿主报告为准。
