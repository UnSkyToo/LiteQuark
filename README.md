# LiteQuark

LiteQuark is a Unity framework with a core package, optional systems, and a battle example with Timeline authoring tools. This repository is a complete Unity project; open its root in Unity Hub.

## Setup

Use the editor version in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), currently **2022.3.62f3**. Package versions and declared minimum Unity versions are recorded separately in each `package.json`; those declarations are not a compatibility test of older editors.

1. Clone the repository outside another project's `Assets` directory and open it with the matching editor.
2. Open [Demo.unity](Assets/Scenes/Demo.unity), the only enabled scene in Build Settings.
3. Inspect the `Launcher` object's Debug/Release settings. The checked-in scene enables `LogRuntimeLogic` and the optional `UISystem`; it does **not** register the battle `DemoLogic`.
4. Follow the [framework guide](Assets/LiteQuark/README.md) to register your own `ILogic` and systems. Put loadable assets under `Assets/StandaloneAssets`.

To integrate into an existing project, copy `Assets/LiteQuark` and optionally `Assets/LiteQuark.Addons`, including their `.meta` files, or use the published package branches in Package Manager:

```text
https://github.com/UnSkyToo/LiteQuark.git#LiteQuark
https://github.com/UnSkyToo/LiteQuark.git#LiteQuark.Addons
```

Branches track their published contents, which may differ from this checkout. For reproducible integration, select a verified release tag or commit. Both local packages currently declare `0.4.3`; Addons still declares a core dependency of `0.4.2`. Check that pairing before publishing or installing a release.

## Modules and documentation

| Location | Purpose / guide |
| --- | --- |
| `Assets/LiteQuark/Runtime` | [Lifecycle](Assets/LiteQuark/README.md), [assets](Assets/LiteQuark/Runtime/Asset/README.md), [events](Assets/LiteQuark/Runtime/Core/Event/README.md), [tasks](Assets/LiteQuark/Runtime/Core/Async/Task/README.md), [timers](Assets/LiteQuark/Runtime/Core/Async/Timer/README.md), [actions](Assets/LiteQuark/Runtime/Core/Async/Action/README.md), [object pools](Assets/LiteQuark/Runtime/Core/ObjectPool/README.md), logging and utilities |
| `Assets/LiteQuark/Editor` | Build tools, Bundle Viewer, load profiler, inspectors and debugger |
| `Assets/LiteQuark.Addons` | [UI](Assets/LiteQuark.Addons/Runtime/UI/README.md), [audio](Assets/LiteQuark.Addons/Runtime/Audio/README.md), [effects](Assets/LiteQuark.Addons/Runtime/Effect/README.md), [HTTP](Assets/LiteQuark.Addons/Runtime/Network/README.md), [persistence](Assets/LiteQuark.Addons/Runtime/Data/README.md), [configuration tables](Assets/LiteQuark.Addons/Runtime/Config/README.md) |
| `Assets/LiteBattle` | Entities, input, movement, state machines, Timeline events, battle editor/preview tools |
| `Assets/Scripts` | Battle, network and persistence examples; these are not automated tests |
| `Assets/StandaloneAssets` | Bundle source assets and demo content |

Core bundles UniTask, LitJson and IngameDebugConsole. Unity package dependencies are recorded in [manifest.json](Packages/manifest.json) and [packages-lock.json](Packages/packages-lock.json).

For optional systems, start with the [Addons guide](Assets/LiteQuark.Addons/README.md). The [Config guide](Assets/LiteQuark.Addons/Runtime/Config/README.md) covers Excel input, JSON/C# export and runtime registration.

The battle entry is [DemoLogic.cs](Assets/Scripts/DemoLogic.cs), which starts `LiteNexusEngine`; the authoring window is `Lite > Nexus Engine > Editor Window`. Battle scene/configuration setup is separate from the default launcher scene. Await engine startup before accessing `LiteNexusConfig.Instance`: the data manager loads configuration asynchronously and releases its Handles on initialization failure or shutdown. Editor tools access configuration through AssetDatabase.

## Validation and builds

The regression suite validates the current working tree in an isolated Unity project. From the repository root:

```powershell
# Windows, PowerShell 7
& .\Validation\Run-Validation.ps1
```

```bash
# macOS, system Bash
bash ./Validation/Run-Validation.sh
```

Both platforms require Git, the matching licensed Unity editor with native AssetBundle build support, and access to declared package sources. Read [validation instructions](Validation/README.txt) for preparation, suite selection, platform coverage and report interpretation; read [maintenance rules](Validation/AGENTS.md) before changing the suite.

For a local compile, use the matching Unity executable. In PowerShell, set `$unityEditor` to its absolute path, then run from the repository root:

```powershell
& $unityEditor -batchmode -projectPath . -quit -logFile Logs/compile.log
```

Use an editor session already open on this project, or close it before starting batchmode on the same checkout. Check the exit status and log for compilation errors. Unity Test Framework is present through package dependencies, but there is currently no dedicated test assembly or checked-in CI workflow.

Open `Lite > Builder` for application/resource builds. The command-line entry is `LiteQuark.Editor.ProjectBuilderCommandLine.Build`; for example, to build Android resources and copy them to StreamingAssets:

```powershell
& $unityEditor -batchmode -projectPath . -executeMethod LiteQuark.Editor.ProjectBuilderCommandLine.Build -target Android -enableRes true -enableApp false -copyToStreaming true -appVersion 0.4.3 -logFile Logs/build.log
```

Install the target's Unity build support first. The entry point exits the editor itself. With `-copyToStreaming true`, it deletes and replaces `Assets/StreamingAssets/LiteQuark` with the generated resources; keep unrelated files outside that directory. Build preparation can also change project settings, so review the diff afterward. See [command-line options](Assets/LiteQuark/Editor/Builder/ProjectBuilderCommandLine.cs) for application, resource and custom build flags. Inspection tools are `Lite > Asset > Bundle Viewer` and `Lite > Asset > Profiler`.

## Publishing and contributing

`publish.sh` / `publish.cmd` publish the core subtree; `publish.addons.sh` / `publish.addons.cmd` publish Addons. They prompt for a version, create a subtree branch and tag (`<version>` or `addons/<version>`), then push the branch **and all local tags** to `origin`. These are publishing commands. Align package metadata, dependencies and changelogs before running them.

See [AGENTS.md](AGENTS.md) for conventions and [core](Assets/LiteQuark/CHANGELOG.md) / [Addons](Assets/LiteQuark.Addons/CHANGELOG.md) changelogs for release history. This checkout includes unreleased Handle API changes; released-version notes describe their own versions.

LiteQuark is distributed under the [MIT license](LICENSE). Bundled libraries, fonts and demo assets retain their own license and attribution notices.

For questions and bug reports, use [GitHub Issues](https://github.com/UnSkyToo/LiteQuark/issues) or contact [unskytoo@gmail.com](mailto:unskytoo@gmail.com).
