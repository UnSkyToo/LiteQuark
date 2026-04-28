# Repository Guidelines

## Project Structure & Module Organization

This is a Unity project targeting Unity `2022.3.62f3`. Core framework code lives in `Assets/LiteQuark`, split into `Runtime`, `Editor`, and `Third` folders with matching `.asmdef` assemblies. Optional modules live in `Assets/LiteQuark.Addons`, also split into `Runtime` and `Editor`. Battle/demo tooling and sample resources live in `Assets/LiteBattle`. Unity package metadata is in `Packages`, project configuration is in `ProjectSettings`, and generated local folders such as `Library`, `Temp`, `Logs`, and `obj` should not be edited manually.

## Build, Test, and Development Commands

- Open locally with Unity Hub or the Unity Editor version listed in `ProjectSettings/ProjectVersion.txt`.
- `./publish.sh`: creates and tags a subtree branch from `Assets/LiteQuark`.
- `./publish.addons.sh`: creates and tags a subtree branch from `Assets/LiteQuark.Addons`.
- `git status --short`: check generated Unity changes before committing, especially `.meta` files.

For CI or local batch checks, prefer Unity batchmode commands such as:

```bash
Unity -batchmode -projectPath . -quit
```

## Coding Style & Naming Conventions

Use C# conventions already present in the repo: namespaces under `LiteQuark.Runtime`, `LiteQuark.Editor`, or module-specific equivalents; PascalCase for types, methods, and public properties; camelCase for parameters and locals; `_camelCase` for private fields where existing code uses that style. Keep braces on their own lines and use four-space indentation. Runtime code must stay out of `Editor` folders and editor-only APIs must remain in `Editor` assemblies. Commit Unity `.meta` files whenever assets, scripts, or folders are added, moved, or renamed.

## Testing Guidelines

No dedicated test assembly is currently present. When adding tests, use Unity Test Framework and place EditMode or PlayMode tests under an appropriate `Tests` folder with a matching test `.asmdef`. Name test classes after the unit under test, for example `LiteRuntimeTests`, and name test methods by behavior. At minimum, run a Unity editor compile after changing assembly definitions, runtime modules, or serialized assets.

## Commit & Pull Request Guidelines

Recent commits use bracketed change types such as `[F]` for fixes, `[U]` for updates, `[A]` for additions, and `[M]` for merges, often followed by a concise Chinese summary. Follow that pattern, for example `[F] 修复资源加载句柄释放异常`. Pull requests should describe the affected module, summarize behavior changes, list Unity validation performed, and include screenshots or recordings for editor UI changes.

## Security & Configuration Tips

Do not commit machine-specific Unity caches or credentials. Keep dependency changes in `Packages/manifest.json` and `Packages/packages-lock.json` together, and verify git-sourced packages before updating them.
