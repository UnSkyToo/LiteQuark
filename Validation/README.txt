Validation — 当前工作区的隔离 Unity 验证

文档分工
  README.txt 说明怎么运行、验证什么、如何读结果。
  AGENTS.md 约束维护方式；修改源码或入口前阅读它，不重复维护运行说明。

运行
  Windows（PowerShell 7，从仓库根目录）：
    & .\Validation\Run-Validation.ps1
    & .\Validation\Run-Validation.ps1 -Suite paths,manifest,collector

  macOS（系统 Bash，从仓库根目录，无需 PowerShell）：
    bash ./Validation/Run-Validation.sh
    bash ./Validation/Run-Validation.sh --suite paths,manifest,collector

  两个平台使用各自原生入口，共享同一套 C# 用例和主工程依赖声明。
  默认全量；Windows -Suite 接受 PowerShell 数组，macOS --suite 接受逗号分隔列表。
  编辑器路径：Windows -UnityEditor，macOS --unity-editor；路径有空格时加引号。
  准备超时：-PreparationTimeoutSeconds / --preparation-timeout-seconds，默认 1800 秒。
  模块超时：-TimeoutSeconds / --timeout-seconds，默认 600 秒。
  macOS 的 --help 显示用法，也兼容对应的 PowerShell 风格参数名。

环境与准备
  Windows 使用 PowerShell 7；macOS 使用系统自带 Bash、shasum、lsof 和 osascript
  （只用内置 JavaScript 处理 JSON，不控制其他应用）。macOS 不要求 pwsh、Python 或 jq。
  两者均需要 Git、ProjectVersion.txt 指定版本的 Unity（当前 2022.3.62f3）、
  可用许可，以及对应本机平台的 AssetBundle 构建支持。
  默认编辑器位置：
    Windows: C:/Program Files/Unity/Hub/Editor/<版本>/Editor/Unity.exe
    macOS:   /Applications/Unity/Hub/Editor/<版本>/Unity.app/Contents/MacOS/Unity
  Windows 构建 Windows64 Bundle，macOS 构建 macOS Bundle。
  首次恢复需要访问主工程声明的 Unity 包源和 GitHub；本机自行配置代理和许可。
  验证目录须可写，并允许生命周期用例监听回环 TCP。运行前关闭隔离 Project
  的编辑器；生成目录须为普通目录，不可链接到主工程。
  macOS 入口拒绝带制表符或换行符的源码文件名。异常中断若残留 .validation.lock，
  先确认没有验证进程占用，再手动移除；入口不自动删除其他运行的锁。

  直接运行入口即可，不必先打开主工程或准备旧缓存。入口同步当前工作区
  （包括未提交修改），按主工程锁定的依赖恢复并检查一致性，不回写主工程。
  Bundle 和测试资源自动生成；首次运行需要联网，不承诺完全离线恢复。

目录
  Sources/                 唯一维护的验证源码及 .meta
  Run-Validation.ps1       Windows 原生验证入口
  Run-Validation.sh        macOS 原生验证入口（同步、准备、执行、核对及汇总）
  Project/                 生成的隔离 Unity 工程，含编译缓存
  Reports/                 最近一次运行的结果和日志
  Bundles/                 自动构建的测试 Bundle
  Scratch/                 测试临时文件，正常结束后为空
  源码、入口和文档入库；生成目录及 .validation.lock 忽略。
  验证源码不随核心或 Addons 包发布；入口不使用 PackageSources 或旧 SessionValidation 缓存。

模块
  paths       路径组合、原生根目录、URI、StreamingAssets、文件操作与导航
  manifest    完整路径清单、依赖 ID、Hash/Flat 命名、重复身份和旧格式边界
  collector   Bundle 归属、共享依赖、循环、路径映射、BundleViewer 和 Flat 冲突
  packages    真实 UPM 纹理、隐式/显式依赖打包及框架索引边界
  tasks       任务调度、终态与结果通知、异步等待的失败/取消传播、Runtime 初始化与关闭
  lifecycle   Handle/Lease、缓存、场景、取消、关闭、资源 Task 原生收尾、优先级和并发限制
  loading     路径别名、同名资源/场景、真实加载、构建选项和 Locater 配置

  资源生命周期覆盖迟到结果清理、结果交接、引用与依赖保护、关闭等待及失败传播，
  包含 Bundle 和 Editor 模式。具体用例见 Sources 与本轮报告。
  ObjectPool 覆盖模板成功/失败/取消、移除时同 key 池替换、批量清理中的取消回调、预生成关闭、入池异常与剩余实例清理、真实 prefab Handle 释放和借出实例责任；
  Task 实例化用例覆盖原生完成但尚未发布时的清理，以及成功通知中的包装器释放。

  各模块使用独立 Unity 进程，每组含多项断言；用例和断言数量以本轮报告为准。
  Windows 已有全量回归基线；macOS 入口及本机 Bundle 目标已适配，尚待 macOS
  实机执行确认，Windows 结果不代表 macOS 结果。
  路径根和盘符组合按宿主文件系统语义断言；Windows 保留驱动器/UNC 覆盖，
  macOS 使用 POSIX 根目录。URI、资源标识中的 Windows 字符串仍是固定测试输入。
  lifecycle、loading 和 tasks 使用 Play Mode；失败不会放宽后续断言。
  编译、启动或结果缺失会停止后续模块，普通用例失败仍继续汇总。

报告与退出码
  Reports/verification-report.html  本轮汇总及逐模块链接
  Reports/summary.json              总结果、准备状态和隔离检查
  Reports/<模块>/results.json        逐用例断言、失败、分类及证据
  Reports/<模块>/unity.log           模块日志
  Reports/prepare.log               依赖恢复、导入和编译日志
  Reports/source-state.json         基准提交、宿主/构建平台、模块与入口/源码指纹
  Reports/source-hashes.csv          当前工作区文件指纹

  Commit 是运行时基准 HEAD；未提交内容须结合 Status 和文件指纹判断。
  HostPlatform/BuildTarget 区分宿主和构建目标。开始和结束核对源码、Git 状态、
  暂存区、隔离代码、验证源码、入口与依赖配置；运行期间修改会使本轮失败。
  隔离代码核对前后完整路径集合及哈希，新增、修改、删除代码或对应 .meta 均失败。
  同步前拒绝生成源码、设置、包声明、报告及验证源码树中的链接；特殊 asmdef 和
  验证目录 .meta 写入也单独检查目标，包含目标尚不存在的符号链接。
  全量覆盖本目录既有报告；专项只替换所选模块，汇总仅描述本轮，不累积历史。
  退出码 0 表示日常回归、要求的诊断及隔离检查通过；其他失败返回非零。

已知限制与覆盖边界
  Passed/RegressionCases 仅计日常回归；KnownLimitations 和
  Diagnostics/DiagnosticsPassed 单列，不计入日常回归通过数；各类数量以实际报告为准。
  唯一允许的非阻断限制是 UNITY-SAME-NAME-SCENE-BUNDLES：
  loading 第 08 组必须实际执行，确认一份场景成功且内容/路径正确，另一份
  出现指定 Bundle 冲突且无额外错误；第 17 组还须独立确认两份 Bundle 单独
  可加载、正反顺序并发均冲突。限制保留 passed=false、编号、异常和证据。
  不满足这些条件、诊断失败或清理泄漏仍阻断验证。

  真实加载覆盖本机 Editor、file URI 和受控回环 HTTP；不代表远程服务器、
  WebGL、其他 Player 平台或真实网络故障。移动端 jar URI 仅作字符串测试。
  BuiltIn 定位会在真实 persistentDataPath 下创建随机临时目录并清理；
  BundleViewer 验证树数据和定位函数，不验证窗口渲染。
  不覆盖真正 UPM 直接加载、旧格式自动迁移、已提交原生场景操作的强制取消
  或完整系统即时重启；最终 Bundle 释放阶段的异常继续处理未做故障注入验证。
  真实 UPM 打包与框架公共加载边界是不同测试目标。
