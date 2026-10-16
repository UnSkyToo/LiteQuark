# Repository guidance

## Project map

- Use the Unity version in `ProjectSettings/ProjectVersion.txt` (currently `2022.3.62f3`). Open the repository root as the Unity project.
- `Assets/LiteQuark`: core runtime, editor tools, bundled third-party libraries. `Assets/LiteQuark.Addons`: optional systems. `Assets/LiteBattle`: battle runtime and authoring tools. `Assets/Scripts`: examples.
- For setup, builds and publishing, use [README.md](README.md). For lifecycle or registration, use the [framework guide](Assets/LiteQuark/README.md). For resource ownership, use the [asset guide](Assets/LiteQuark/Runtime/Asset/README.md). Read other module guides as the task requires.

## Change constraints

- Follow nearby C# style: four spaces, braces on separate lines, PascalCase types/methods/properties, camelCase locals, and the existing private-field convention. Preserve module namespaces.
- Keep runtime/editor assembly boundaries intact. Editor APIs in runtime source require `UNITY_EDITOR` guards; editor tools belong in Editor assemblies.
- Preserve Unity asset GUIDs. Include matching `.meta` files for added, moved, or renamed assets and folders.
- Do not hand-edit generated `Library`, `Temp`, `Logs`, `obj`, `.csproj`, or `.sln` files. Keep third-party code, license notices and imported assets unchanged unless relevant to the task.
- Asset ownership is Handle-based: retain the Handle while its resource is in use and release through `Dispose()`. Update consumers and documentation when changing public APIs or serialized configuration.
- Keep package manifest and lockfile changes consistent; verify the source and revision of Git dependencies before updating them. Check package versions and the Addons core dependency for releases; avoid incidental upgrades.

## Validation

- Documentation-only edits: check referenced APIs, local links and `git diff --check`; a Unity launch is unnecessary.
- Runtime, assembly-definition or serialized-asset changes: run a Unity editor compile. Exercise affected behavior; include screenshots or recordings for editor UI changes when possible.
- No project test assembly currently exists. Add Unity Test Framework EditMode/PlayMode tests with a test `.asmdef` when regression coverage is warranted.
- Local compilation and relevant checks are part of the change. Fix resulting failures and repeat affected checks; broaden validation for concrete concerns. Report checks that could not run.

## Delivery

- Review `git status --short` for incidental Unity changes. Do not commit caches or credentials.
- Commit messages use `[F]`, `[U]`, `[A]`, `[D]`, or `[M]` plus a concise summary, usually Chinese. PRs describe the affected module, behavior change and actual validation.
- `publish.sh`, `publish.addons.sh` and their `.cmd` variants create subtree branches, tag versions and push to `origin` with `--tags`. Run them as part of a requested release, never as validation.
