# LiteQuark

LiteQuark is a lightweight Unity framework for application startup, lifecycle management, asset loading, and common runtime services. LiteQuark.Addons provides optional UI, audio, effects, HTTP, persistence, and configuration tables.

## Setup

Open this repository's root in Unity Hub with the editor version in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), currently **2022.3.62f3**.

### Run an example

- **Framework startup:** Open [Demo.unity](Assets/Scenes/Demo.unity) and enter Play Mode. The default build scene starts the framework and `UISystem` with no game entry.
- **LiteBattle:** Open [LiteNexusEditorScene.unity](Assets/LiteBattle/Resources/Scenes/LiteNexusEditorScene.unity) and enter Play Mode. Use `Lite > Nexus Engine > Editor Window` to author and preview Timelines. [DemoGameEntry](Assets/Scripts/DemoGameEntry.cs) starts `LiteNexusEngine`; await its startup before reading `LiteNexusConfig.Instance` in game code.

### Integrate into an existing project

Copy `Assets/LiteQuark` and optionally `Assets/LiteQuark.Addons`, including their `.meta` files, or use the published package branches in Package Manager:

```text
https://github.com/UnSkyToo/LiteQuark.git#LiteQuark
https://github.com/UnSkyToo/LiteQuark.git#LiteQuark.Addons
```

Published branches can differ from this checkout. Pin a verified release tag or commit for reproducible installs, and check the [Addons manifest](Assets/LiteQuark.Addons/package.json) for the required core version.

- [Core guide](Assets/LiteQuark/README.md): Launcher setup, lifecycle, and links to the built-in systems.
- [Addons guide](Assets/LiteQuark.Addons/README.md): install and register optional systems.
- [Asset guide](Assets/LiteQuark/Runtime/Asset/README.md): put loadable assets under `Assets/StandaloneAssets` by default, and retain their Handles until they are no longer in use.

## Validation and builds

Run the isolated regression suite from the repository root: `& .\Validation\Run-Validation.ps1` (PowerShell 7) or `bash ./Validation/Run-Validation.sh` (macOS). See the [validation guide](Validation/README.txt) for requirements, suite selection, and reports.

For a local compile, close any editor using this checkout, set `$unityEditor` to the full path of the matching Unity executable, and check the exit code and log:

```powershell
& $unityEditor -batchmode -projectPath . -quit -logFile Logs/compile.log
```

Use `Lite > Builder` with the target's Unity build support installed. For example, build Android resources and copy them to StreamingAssets with [these CLI options](Assets/LiteQuark/Editor/Builder/ProjectBuilderCommandLine.cs):

```powershell
& $unityEditor -batchmode -projectPath . -executeMethod LiteQuark.Editor.ProjectBuilderCommandLine.Build -target Android -enableRes true -enableApp false -copyToStreaming true -appVersion 1.0.0 -logFile Logs/build.log
```

The build entry exits Unity. `-appVersion` sets the application and resource build version independently of UPM. `-copyToStreaming true` replaces `Assets/StreamingAssets/LiteQuark`; build preparation can also change project settings.

`Lite > Asset` contains **Bundle Viewer**, **Version Viewer**, and the load **Profiler**.

## Publishing and contributing

Before releasing, align the package metadata, dependencies, and [Core](Assets/LiteQuark/CHANGELOG.md) / [Addons](Assets/LiteQuark.Addons/CHANGELOG.md) changelogs. `publish.sh` / `publish.cmd` publish Core (`<version>`); `publish.addons.sh` / `publish.addons.cmd` publish Addons (`addons/<version>`). Each pushes its subtree branch and **all local tags** to `origin`.

See [AGENTS.md](AGENTS.md) for contribution and validation rules.

LiteQuark is distributed under the [MIT license](LICENSE). Bundled libraries, fonts and demo assets retain their own license and attribution notices.

For questions and bug reports, use [GitHub Issues](https://github.com/UnSkyToo/LiteQuark/issues) or contact [unskytoo@gmail.com](mailto:unskytoo@gmail.com).
