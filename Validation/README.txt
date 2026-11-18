Validation — LiteQuark 隔离验证

运行
  从仓库根目录运行。Windows 使用 PowerShell 7，macOS 使用系统 Bash。
    & .\Validation\Run-Validation.ps1
    & .\Validation\Run-Validation.ps1 -Suite addons
    & .\Validation\Run-Validation.ps1 -Suite paths,manifest,collector
    bash ./Validation/Run-Validation.sh
    bash ./Validation/Run-Validation.sh --suite addons
    bash ./Validation/Run-Validation.sh --suite paths,manifest,collector

  默认执行全部 suite。每个 suite 在独立 Unity 进程中运行。
  指定 Editor 用 -UnityEditor / --unity-editor。准备阶段超时用
  -PreparationTimeoutSeconds / --preparation-timeout-seconds（默认 1800 秒），
  单个 suite 超时用 -TimeoutSeconds / --timeout-seconds（默认 600 秒）。
  macOS 的 --help 可查看参数。仅需系统 Bash、shasum、lsof、osascript 和 Git。

环境
  使用 ProjectSettings/ProjectVersion.txt 指定的 Unity（2022.3.62f3）、可用许可和
  本机平台的 Bundle 构建支持。Windows 构建 Win64，macOS 构建 OSXUniversal。
  默认从 Unity Hub 标准安装目录定位 Editor。首次恢复依赖需要访问主工程声明的包源。
  运行前关闭隔离工程的编辑器；验证目录须可写，系统允许回环 TCP。
  macOS 入口拒绝文件名中的制表符和换行。异常中断残留 .validation.lock 时，
  先确认没有进程占用，再处理本次遗留锁。

验证位置
  Scenarios.json 是两个平台共用的预期场景 ID 清单。每个 ID 对应一份结果。
  在 Sources 下搜索完整的 suite/id 即可找到强类型登记入口和具体方法。

  paths      Core/PathValidationRunner.cs
             宿主路径及 URI、带过滤规则的目录复制、文件改名。
  manifest   Asset/ManifestValidationRunner.cs
             完整资源身份、稀疏依赖 ID 与重复导出、Hash/Flat 构建和加载名。
  collector  Asset/CollectorValidationRunner.cs、Asset/UpmBuild.cs
             收集与构建输入、分包规则和冲突、Viewer 数据与源定位、跨 Play Mode
             注册和快照。用真实 UPM 纹理核对材质的隐式依赖与显式拆包。
  tasks      Core/TaskSystemValidationRunner.cs、Core/RuntimeLifecycle.cs、Core/PoolValidation.cs
             准入与取消、终态和协程、下载快照与原生实例化、池的模板所有权；
             Runtime 启动回滚、关闭顺序、重启、部分释放失败。通过 Editor 退出
             真实 Demo；ShutdownFailed 场景最后执行，保留终态至进程退出。
  loading    Asset/AssetLoadingValidationRunner.cs
             公开寻址、后端选择、旧设置和初始化错误、BuiltIn 优先级、Remote URI
             与真实回环下载、构建文件名标志，以及 Unity 场景包限制。
  lifecycle  Asset/AssetLifecycleValidationRunner.cs 及同名前缀的主题文件
             从共享 Handle 和 Lease 到保留期、卸载、重载。覆盖排队终止、原生结果
             迟到、Loader 交付前关闭、Cache 等待依赖时关闭、场景独立实例、最后
             一个场景拒卸、预加载、优先级和两道并发限额。
  addons     Addons/AddonsValidationRunner.cs 及各模块的验证文件
             UI 打开、取消与关闭，Audio Clip 与 Carrier，Effect 模板及实例复用，
             HTTP 排队、重试、拦截器及文件传输，Data 读写／切换／持久化，Config
             重载及关闭。RuntimeRestartUI.cs 单独保护从旧 UI 在途到新 UI 同路径
             重开的真实链路，使用公开 Restart、真实 Bundle 及两轮 Runtime。

判定与边界
  两端脚本按 suite 比较预期与实际 ID；漏跑、重复、多跑、编译或清理失败均失败。
  日常回归、已知限制和独立诊断分别统计。唯一允许的限制为
  UNITY-SAME-NAME-SCENE-BUNDLES。loading/same-name-limit 必须记录正确场景
  的路径和内容、指定 Bundle 冲突、错误及证据，并保持 passed=false。
  loading/same-name-native 独立确认每份 Bundle 单独可加载，正反顺序均重现
  指定冲突。额外错误、未确认的诊断和泄漏均阻断验证。

  原生卸载检查实际 Bundle／Scene 的有效性。Simulate 从项目资产加载，不以
  AssetDatabase 对象存活证明原生包释放。真实网络范围为受控回环 HTTP；URI
  的 jar 字符串、Player 模式选择、Editor 树数据分别不构成移动 Player、远端
  部署或窗口渲染的实机验证。UPM 打包与框架公共索引边界各有独立断言。
  本套验证没有覆盖所有加载状态和所有模块组合下的即时重启。

隔离与输出
  入口从当前工作区同步 Assets、ProjectSettings 与 Packages，包括未提交源码；
  递归复制 Sources 中的验证源码及文件、目录 .meta。生成的 LiteQuark.Editor
  引用 UniTask、LitJson、LiteQuark.Runtime.Addons。运行前后核对主工程文件、
  Git 状态与暂存区、隔离代码路径集合及哈希、验证源码和入口指纹、依赖配置。
  生成树中的链接在写入前拒绝。只有 Project、Reports、Bundles、Scratch 和
  本次运行锁用于验证。Scratch 中的临时测试文件应随用例清理。

  Reports/verification-report.html    本次运行汇总与日志链接
  Reports/summary.json                数量、分类、准备及隔离结果
  Reports/<suite>/results.json         ID、状态、断言数、失败、耗时和证据
  Reports/<suite>/unity.log           Unity 进程原始日志
  Reports/prepare.log                 包恢复、导入与编译日志
  Reports/source-state.json           基准提交、宿主平台、构建目标及指纹
  Reports/source-hashes.csv           工作区文件指纹

  全量运行替换 Reports；专项只替换所选 suite，汇总只代表本轮。普通场景
  失败仍汇总其他场景；编译、启动或报告缺失可提前终止。退出码 0 要求
  本轮日常回归、限制及诊断分类和隔离检查均通过。平台结果以实际宿主报告为准。
