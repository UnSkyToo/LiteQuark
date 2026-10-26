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
| `Assets/LiteQuark.Addons` | [UI](Assets/LiteQuark.Addons/Runtime/UI/README.md), [audio](Assets/LiteQuark.Addons/Runtime/Audio/README.md), [effects](Assets/LiteQuark.Addons/Runtime/Effect/README.md), [HTTP](Assets/LiteQuark.Addons/Runtime/Network/README.md), [persistence](Assets/LiteQuark.Addons/Runtime/Data/README.md), configuration tables |
| `Assets/LiteBattle` | Entities, input, movement, state machines, Timeline events, battle editor/preview tools |
| `Assets/Scripts` | Battle, network and persistence examples; these are not automated tests |
| `Assets/StandaloneAssets` | Bundle source assets and demo content |

Core bundles UniTask, LitJson and IngameDebugConsole. Unity package dependencies are recorded in [manifest.json](Packages/manifest.json) and [packages-lock.json](Packages/packages-lock.json).

For configuration tables, open `Lite > Config Tool` to parse Excel sheets and export JSON/C# definitions. Add the optional `ConfigSystem` to the Launcher. In business initialization, register tables with `RegisterTable<T>(path, parseFunc)` or the generated `ConfigSystemGenerated.Register(system)`, then await `Load<T>()` / `LoadAll()` before using `GetData<T>(id)`. `LoadAllOnInitialize` only loads tables registered before system initialization; it does not discover exported tables automatically. See [ConfigSystem.cs](Assets/LiteQuark.Addons/Runtime/Config/ConfigSystem.cs).

The battle entry is [DemoLogic.cs](Assets/Scripts/DemoLogic.cs), which starts `LiteNexusEngine`. The authoring window is `Lite > Nexus Engine > Editor Window`. Battle scene/configuration setup is separate from the default launcher scene.

## Validation and builds

The maintained regression suite runs in a generated, isolated Unity project. From the repository root, use PowerShell 7:

```powershell
& .\Validation\Run-Validation.ps1
& .\Validation\Run-Validation.ps1 -Suite paths,manifest,collector
```

On macOS, run `bash ./Validation/Run-Validation.sh` from Terminal, optionally with `--suite paths,manifest,collector`. This is a native Bash entry using system tools; it does not require PowerShell, Python, or jq. Both platforms need Git, the matching licensed Unity editor with native desktop AssetBundle build support, and access to the declared package sources. Windows has a full regression baseline; native macOS execution remains to be verified.

Both native entries copy the current working tree (including uncommitted changes), restore the main project's locked UPM dependencies, compile, and run the same C# modules with matching result and isolation checks. Use `-UnityEditor` on Windows or `--unity-editor` on macOS for a non-default editor executable. No existing validation project, package mirror, or test Bundles are required. See the validation guide for timeout options.

Validation sources, their `.meta` files, the entry, and documentation are versioned under `Validation`; generated projects, caches, reports, and Bundles are ignored. Results are written to `Validation/Reports/verification-report.html` and `summary.json`, with regression cases, known limitations, and diagnostics reported separately. The entry checks that source files, Git status, and the index remain unchanged. See [validation instructions and coverage](Validation/README.txt) and [maintenance rules](Validation/AGENTS.md). The old ignored `LiteQuark/SessionValidation` caches and historical reports are retained and are not used by the new entry.

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
