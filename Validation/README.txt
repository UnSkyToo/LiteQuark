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
  UI 覆盖同一轮 Update 中 A 关闭 B 后跳过 B 的更新，并保留下一次 Tick 通过关闭队列释放实例的时序。
  UI 关闭还覆盖在途请求同步取消、Forget 默认不报告取消且不执行 ContinueWith 回调、关闭后的新请求拒绝、共享 prefab 迟到成功不再交付 UI，以及原打开/关闭队列和非取消错误行为；真实 Bundle prefab 的原生收尾仍由 Asset 承担。
  UI 调用方取消用例使用真实实体的生命周期 token，覆盖实体释放后不执行 OnOpen／成功续体、同 prefab 的另一请求仍成功、交付后取消不影响 UI，以及预取消不能绕过 mutex 入口检查。
  UI 交付用例还覆盖 Handle 成功后的同步重入：绑定 Attribute 构造时释放调用方仍不进入 OnOpen，OnOpen 释放调用方并抛取消时不交付 UI；两条路径均检查实例和 Handle 清理。
  UI 打开失败和调用方取消还检查 Tag 监听注销、Sprite Handle 提前释放，以及有效 Sprite 迟到成功后不执行回调且归还 Lease；不调用业务 OnClose。
  UI 关闭覆盖递归／重复关闭、OnClose 新增资源的最终清理、未交付引用的终态守卫，以及框架释放异常的传播和入队。UISystem 关闭验证业务 OnClose 抛错不影响其他界面和 Canvas 清理，并检查原 Runtime／服务保持可用，后续资源请求仍能成功。
  Canvas 生命周期用例检查关闭时 CanvasRoot 引用清空、跨帧后系统根和子层实际销毁，以及配置失败后已创建根节点的释放。
  Audio 覆盖共享真实 Clip 的在途关闭与迟到成功隔离、关闭后拒绝播放、重复停止和已加载音频的 Handle／Carrier 精确归还、非法路径及结果处理异常、未初始化／重复释放，以及全局暂停状态恢复和旧实例的暂停入口拒绝；不依赖物理音频输出设备。
  Audio 私有池用例覆盖首次播放前及停止后反复清理共享空闲池、后续音效／音乐成功加载和 Carrier 复用，以及关闭时不影响同名共享池。
  Effect 覆盖关闭／停止后模板迟到成功不执行 Unity OnEnable 续体及自然完成回调、借还平衡且无活动实例残留，并通过正常借出的正向对照确认生命周期观察有效；同时覆盖关闭入口拒绝、归还回调重入、排序精确回退、旧引用与复用实例隔离，以及正常完成／RetainTime、模板失败和池关闭取消的原有时序。
  Network 使用回环 HTTP 覆盖在途传输和排队关闭、连接实际断开、无在途发送时仍等待重试延迟取消，以及 timeScale 为零时 CancelAfter 在线程池取消但等待重试的续体保持主线程；同时覆盖批处理在关闭返回前完成并保持顺序／停止后续组、异步请求／响应拦截器退出、上传下载与进度收尾，以及普通成功／失败、调用方排队取消、关闭入口和替换系统。
  Data 覆盖在途读写关闭等待、全部关闭入口、释放重入、读写与 Dispose 异常、未初始化释放、关闭等待已接受的 Provider 初始化及主线程最终释放，以及 Scratch 内真实 JSON／Binary 文件、加密读写、替换 Provider 与独立 PlayerPrefs 键持久化；不改写业务存档。
  Provider 切换独立覆盖同实例／null、候选初始化失败清理、初始化期间旧同步 API 仍可用、后台初始化后的主线程发布，以及旧 Provider 释放失败后新实例的所有权。
  Config 覆盖共享 TextAsset 加载期间关闭与取消续体、关闭后的注册／加载拒绝及原有查询结果、缓存命中的单表／批量交付、重载发布和临时 Handle 归还；同时保留初始化自动加载顺序、普通解析／资源失败、重载保留旧表及失败后继续后续表的覆盖。
  Runtime 重启用例由真实 Logic.Tick 调用公开 Restart()，不手工拼接关闭／启动代替被测入口；通过实际 Bundle 版本清单和 file URI 初始化两轮 Runtime，以 Asset 任务组控制旧 OpenUI 保持在途。覆盖旧 UI 取消后真实 prefab 迟到成功不触发 OnOpen、异步系统释放阻挡新一轮、全部已注册 System 换代及旧 Bundle／Task 收尾、新 UI 同路径打开／更新／关闭，以及延迟销毁后只保留新一轮 Canvas；不替换 Provider 或手动推进系统。
  重启用例清理保留首个异常及其堆栈，同时报告额外异步／清理错误；先释放 Gate，等待当前关闭退出，ShutdownFailed 后不重试关闭，只有 Idle 才恢复基线。末项回归在真实 Runtime 关闭尾部的生命周期 CTS Dispose 注入故障，使系统已释放但 Runtime 保持 ShutdownFailed，检查原始主体／Dispose 故障可定位、无二次 Shutdown／非 Idle Startup、静态回调／Camera／Scratch 清理，以及没有主体异常时清理失败仍向上传播。该故障用例在本模块进程最后运行，不重置 Runtime 状态，不把预期故障当作生产恢复成功；不增加 Asset 原生 Cleanup 阻挡重启的集成覆盖。

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
  或任意加载状态／全模块组合的即时重启；上述代表性 UI 重启链路不扩大这些保证。
  最终 Bundle 释放阶段的异常继续处理未做故障注入验证。
  真实 UPM 打包与框架公共加载边界是不同测试目标。
