#!/bin/bash
# macOS system Bash 3.2; structured JSON uses the built-in osascript runtime.
set -euo pipefail
export LC_ALL=C GIT_OPTIONAL_LOCKS=0

die() { infrastructure_error="$*"; printf '%s\n' "$*" >&2; exit 1; }
usage() {
    printf '%s\n' 'Usage: bash Validation/Run-Validation.sh [--suite all|paths,manifest,...]' \
        '  --unity-editor PATH                 Unity executable (not the .app directory)' \
        '  --timeout-seconds N                 Per module: 60..3600, default 600' \
        '  --preparation-timeout-seconds N     Preparation: 60..7200, default 1800'
}
suite_arg=all unity_editor= timeout=600 preparation_timeout=1800
while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help) usage; exit 0 ;;
        --suite|-Suite) [ "$#" -ge 2 ] || die "Missing value for $1"; suite_arg=$2 ;;
        --unity-editor|-UnityEditor) [ "$#" -ge 2 ] || die "Missing value for $1"; unity_editor=$2 ;;
        --timeout-seconds|-TimeoutSeconds) [ "$#" -ge 2 ] || die "Missing value for $1"; timeout=$2 ;;
        --preparation-timeout-seconds|-PreparationTimeoutSeconds)
            [ "$#" -ge 2 ] || die "Missing value for $1"; preparation_timeout=$2 ;;
        *) die "Unknown option: $1" ;;
    esac
    shift 2
done
[[ "$timeout" =~ ^[0-9]+$ ]] && [ "$timeout" -ge 60 ] && [ "$timeout" -le 3600 ] || die 'Invalid module timeout.'
[[ "$preparation_timeout" =~ ^[0-9]+$ ]] && [ "$preparation_timeout" -ge 60 ] && [ "$preparation_timeout" -le 7200 ] || die 'Invalid preparation timeout.'
[ "$(uname -s)" = Darwin ] || die 'This entry requires macOS; use Run-Validation.ps1 on Windows.'
for tool in git shasum osascript lsof; do command -v "$tool" >/dev/null || die "Required system tool is missing: $tool"; done
validation_root=$(cd "$(dirname "$0")" && pwd -P)
repository_root=$(cd "$validation_root/.." && pwd -P)
project="$validation_root/Project"
reports="$validation_root/Reports"
sources="$validation_root/Sources"
lock="$validation_root/.validation.lock"
asmdef=Assets/LiteQuark/Editor/LiteQuark.Editor.asmdef
test_directory=Assets/LiteQuark/Editor/SessionValidation
unity_version=$(sed -n 's/^m_EditorVersion: //p' "$repository_root/ProjectSettings/ProjectVersion.txt" | tr -d '\r')
[ -n "$unity_version" ] || die 'Cannot read ProjectVersion.txt.'
[ -n "$unity_editor" ] || unity_editor="/Applications/Unity/Hub/Editor/$unity_version/Unity.app/Contents/MacOS/Unity"
[ -f "$unity_editor" ] && [ -x "$unity_editor" ] || die "Unity editor not found or not executable: $unity_editor"
installed_version=$("$unity_editor" -version) || die 'Cannot query Unity version.'
case "$installed_version" in "$unity_version"|"$unity_version "*|"$unity_version"_*) ;; *) die "Unity must match ProjectVersion.txt: $unity_version" ;; esac
[ "$(cd "$(git -C "$repository_root" rev-parse --show-toplevel)" && pwd -P)" = "$repository_root" ] || die 'Validation must be directly inside the repository root.'

all_suites='paths manifest collector packages tasks lifecycle loading'
case "$suite_arg" in ''|,*|*,|*,,*) die 'Empty suite name.' ;; esac
requested=$(printf '%s' "$suite_arg" | tr ',' ' ')
for name in $requested; do
    case " all $all_suites " in *" $name "*) ;; *) die "Unknown suite: $name" ;; esac
done
selected=
for name in $all_suites; do
    case " $requested " in *" all "*|*" $name "*) selected="$selected $name" ;; esac
done
[ -n "$selected" ] || die 'No suites selected.'

owned_path() {
    case "$1" in "$2"/*) ;; *) die "Path outside owned directory: $1" ;; esac
    case "$1/" in */../*|*/./*) die "Non-canonical generated path: $1" ;; esac
    local cursor=$1
    while :; do
        [ ! -L "$cursor" ] || die "Directory links are not supported in generated output: $cursor"
        [ "$cursor" != "$2" ] || break
        cursor=$(dirname "$cursor")
    done
}
remove_generated() { owned_path "$1" "$2"; rm -rf "$1"; }
hash_file() { shasum -a 256 < "$1" | awk '{print toupper($1)}'; }
check_name() {
    case "$1" in *$'\n'*|*$'\r'*|*$'\t'*) printf '%s\n' 'Tracked filenames must not contain tabs or line breaks.' >&2; return 1 ;; esac
}
snapshot_repository() {
    git -C "$repository_root" ls-files --cached --others --exclude-standard -z > "$lock/paths.bin" || return 1
    while IFS= read -r -d '' path; do
        check_name "$path" || return 1
        [ -f "$repository_root/$path" ] || continue
        digest=$(hash_file "$repository_root/$path") || return 1
        printf '%s\t%s\n' "$digest" "$path"
    done < "$lock/paths.bin" | sort -u
}
snapshot_tree() {
    local root=$1 file relative digest
    [ -d "$root" ] || return 0
    while IFS= read -r -d '' file; do
        relative=${file#"$root/"}
        check_name "$relative" || return 1
        digest=$(hash_file "$file") || return 1
        printf '%s\t%s\n' "$digest" "$relative"
    done < <(find "$root" -type f -print0) | sort
}
snapshot_code() { snapshot_tree "$project/Assets" | awk -F '\t' '$2 ~ /\.(cs|asmdef|asmref|dll)(\.meta)?$/'; }
copy_changed() {
    owned_path "$2" "$project"
    if ! cmp -s "$1" "$2"; then mkdir -p "$(dirname "$2")"; cp -p "$1" "$2"; fi
}

# Only JSON transformation/reporting lives here; process and filesystem control stays in Bash.
json_action() {
    osascript -l JavaScript - "$@" <<'JXA'
ObjC.import('Foundation');
function read(path) {
    var text = ObjC.unwrap($.NSString.stringWithContentsOfFileEncodingError(path, $.NSUTF8StringEncoding, Ref()));
    if (text === undefined || text === null) throw Error('Cannot read ' + path);
    return text;
}
function write(path, text) {
    if (!$(text).writeToFileAtomicallyEncodingError(path, true, $.NSUTF8StringEncoding, Ref())) throw Error('Cannot write ' + path);
}
function load(path) { return JSON.parse(read(path).replace(/^\uFEFF/, '')); }
function save(path, value) { write(path, JSON.stringify(value, null, 2) + '\n'); }
function hashes(path) {
    return read(path).split('\n').filter(Boolean).map(function(line) {
        var at = line.indexOf('\t');
        return { Path: line.slice(at + 1), SHA256: line.slice(0, at) };
    });
}
function changes(before, after, pathsOnly) {
    var a = {}, b = {};
    hashes(before).forEach(function(r) { a[r.Path] = pathsOnly ? true : r.SHA256; });
    hashes(after).forEach(function(r) { b[r.Path] = pathsOnly ? true : r.SHA256; });
    return Object.keys(a).concat(Object.keys(b)).filter(function(p, i, all) {
        return all.indexOf(p) === i && a[p] !== b[p];
    });
}
function html(value) {
    return String(value === undefined || value === null ? '' : value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
function run(a) {
    var mode = a[0], report = a[1], work = a[2];
    if (mode === 'asmdef') {
        var def = load(a[1]);
        def.references = (def.references || []).concat(['UniTask', 'LitJson']).filter(function(v, i, all) { return all.indexOf(v) === i; });
        save(a[2], def); return;
    }
    if (mode === 'packages') {
        var expected = load(a[1]).dependencies, actual = load(a[2]).dependencies;
        if (Object.keys(expected).sort().join('\n') !== Object.keys(actual).sort().join('\n')) throw Error('Resolved package set differs from the main lockfile.');
        Object.keys(expected).forEach(function(name) {
            ['version', 'source', 'hash', 'url'].forEach(function(field) {
                if (expected[name][field] !== actual[name][field]) throw Error('Resolved package differs: ' + name + ' (' + field + ')');
            });
        }); return;
    }
    if (mode === 'preparation') {
        save(report + '/preparation.json', { Success: a[2] === '1', ExitCode: Number(a[3]), DurationSeconds: Number(a[4]) }); return;
    }
    if (mode === 'state') {
        var rows = hashes(work + '/repo.before');
        save(report + '/source-state.json', {
            CapturedUtc: new Date().toISOString(), Commit: a[3], Status: read(work + '/status.before'),
            IndexSHA256: a[4], UnityVersion: a[5], HostPlatform: 'macOS', BuildTarget: 'OSXUniversal',
            SelectedSuites: a[6].trim().split(/\s+/), ProductionFiles: rows.length, CopiedFiles: Number(a[7]),
            RunnerHashes: hashes(work + '/runners.before'), ScriptSHA256: a[8],
            AssemblyAdapter: 'Current LiteQuark.Editor.asmdef plus UniTask and LitJson references in isolated Project only.',
            AssemblyAdapterSHA256: a[9]
        });
        var csv = function(v) { return '"' + v.replace(/"/g, '""') + '"'; };
        write(report + '/source-hashes.csv', '"Path","SHA256"\n' + rows.map(function(r) { return csv(r.Path) + ',' + csv(r.SHA256); }).join('\n') + '\n');
        return;
    }
    if (mode === 'suite') {
        var name = a[2], expectedCases = Number(a[3]), version = a[4];
        var s = { Suite: name, Passed: 0, Failed: 0, Cases: 0, RegressionCases: 0,
            KnownLimitations: 0, Diagnostics: 0, DiagnosticsPassed: 0, Assertions: 0,
            ExitCode: Number(a[5]), DurationSeconds: Number(a[6]), Success: false, Error: null,
            Failures: [], KnownLimitationDetails: [], DiagnosticDetails: [] };
        try {
            var result = load(report + '/' + name + '/results.json');
            var cases = name === 'packages' ? [result] : result.cases;
            if (!Array.isArray(cases)) throw Error('Missing cases.');
            s.Cases = cases.length;
            var known = cases.filter(function(c) { return c.status === 'known_limitation'; });
            var diagnostics = cases.filter(function(c) { return c.diagnostic; });
            if (name === 'loading') {
                if (diagnostics.length !== 1 || diagnostics[0].name !== '17 Native same-name Bundle collision diagnostic') throw Error('Missing or unexpected native diagnostic.');
            } else if (diagnostics.length) throw Error('Unexpected diagnostic outside loading.');
            known.forEach(function(c) {
                if (name !== 'loading' || known.length !== 1 || c.passed || c.diagnostic ||
                    c.name !== '08 Bundle same-name scenes in different folders' || c.limitationId !== 'UNITY-SAME-NAME-SCENE-BUNDLES' ||
                    !diagnostics[0].passed || diagnostics[0].status !== 'diagnostic_passed') throw Error('Unconfirmed or unexpected known limitation.');
            });
            s.KnownLimitations = known.length; s.Diagnostics = diagnostics.length;
            s.DiagnosticsPassed = diagnostics.filter(function(c) { return c.passed === true; }).length;
            s.RegressionCases = cases.length - known.length - diagnostics.length;
            s.Passed = cases.filter(function(c) { return c.passed === true && !c.diagnostic; }).length;
            cases.forEach(function(c) { s.Assertions += Number(c.checks === undefined ? c.assertions || 0 : c.checks); });
            s.Failures = cases.filter(function(c) { return !c.passed && c.status !== 'known_limitation'; }).map(function(c) { return { Name: c.name, Failure: c.failure }; });
            s.Failed = s.Failures.length;
            s.KnownLimitationDetails = known.map(function(c) { return { Id: c.limitationId, Name: c.name, Reason: c.failure, Evidence: c.evidence }; });
            s.DiagnosticDetails = diagnostics.map(function(c) { return { Name: c.name, Confirmed: c.passed, Evidence: c.evidence, Failure: c.failure }; });
            var cleanup = cases.some(function(c) { return (c.evidence || []).some(function(e) { return e.indexOf('Harness-only cleanup') === 0; }); });
            s.Success = s.ExitCode === 0 && cases.length === expectedCases && s.Passed + known.length + s.DiagnosticsPassed === expectedCases &&
                !s.Failed && !result.fatalError && !(result.unexpectedErrors || []).filter(Boolean).length && !cleanup && result.unityVersion === version;
            if (!s.Success) s.Error = 'Validation failed. Expected ' + expectedCases + ' cases; fatal error: ' + (result.fatalError || '') + '; harness cleanup: ' + cleanup;
        } catch (error) { s.Error = String(error); }
        save(report + '/' + name + '/summary.json', s);
        return (s.Success ? '1' : '0') + ' ' + s.Cases;
    }
    if (mode === 'finish') {
        var state = load(report + '/source-state.json'), flags = load(work + '/flags.json');
        var sourceChanges = changes(work + '/repo.before', work + '/repo.after');
        var copyChanges = changes(work + '/code.before', work + '/code.after');
        var sourceUnchanged = !sourceChanges.length;
        var statusUnchanged = read(work + '/status.before') === read(work + '/status.after');
        var runnersUnchanged = hashes(work + '/runners.before').length > 0 && !changes(work + '/runners.before', work + '/runners.after').length;
        var packagesUnchanged = flags.Prepared && !changes(work + '/packages.before', work + '/packages.after').length;
        var suites = read(work + '/completed').trim().split(/\s+/).filter(Boolean).map(function(name) { return load(report + '/' + name + '/summary.json'); });
        var summary = {
            FinishedUtc: new Date().toISOString(), Commit: state.Commit, UnityVersion: state.UnityVersion,
            HostPlatform: 'macOS', BuildTarget: 'OSXUniversal', Preparation: load(report + '/preparation.json'),
            InfrastructureError: read(work + '/error').trim() || null, SelectedSuites: state.SelectedSuites, Suites: suites,
            SourceContentsUnchanged: sourceUnchanged, SourceChanges: sourceChanges, PathChanges: changes(work + '/repo.before', work + '/repo.after', true),
            StatusUnchanged: statusUnchanged, IndexUnchanged: flags.IndexUnchanged,
            ProductionUnchanged: sourceUnchanged && statusUnchanged && flags.IndexUnchanged,
            RunnersUnchanged: runnersUnchanged, SnapshotCodeUnchanged: flags.Copied && hashes(work + '/code.before').length > 0 && !copyChanges.length,
            SnapshotChanges: copyChanges, ScriptUnchanged: flags.ScriptUnchanged, PackagesUnchanged: packagesUnchanged
        };
        ['Cases','RegressionCases','Passed','Failed','KnownLimitations','Diagnostics','DiagnosticsPassed','Assertions'].forEach(function(key) {
            summary[key] = suites.reduce(function(total, s) { return total + s[key]; }, 0);
        });
        summary.Success = !flags.Failed && summary.ProductionUnchanged && runnersUnchanged && summary.SnapshotCodeUnchanged &&
            flags.ScriptUnchanged && packagesUnchanged && suites.length === state.SelectedSuites.length && suites.every(function(s) { return s.Success; });
        save(report + '/summary.json', summary);
        var rows = suites.map(function(s) {
            return '<tr><td>' + html(s.Suite) + '</td><td>' + (s.Success ? 'PASS' : 'FAIL') + '</td><td>' +
                s.Passed + '/' + s.RegressionCases + '</td><td>' + s.KnownLimitations + '</td><td>' +
                s.DiagnosticsPassed + '/' + s.Diagnostics + '</td><td><a href="' + s.Suite + '/results.json">结果</a> · <a href="' + s.Suite +
                '/unity.log">日志</a><pre>' + html(JSON.stringify({ Error:s.Error, Failures:s.Failures, KnownLimitations:s.KnownLimitationDetails, Diagnostics:s.DiagnosticDetails }, null, 2)) + '</pre></td></tr>';
        }).join('');
        write(report + '/verification-report.html', '<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>Validation</title>' +
            '<style>body{font:16px system-ui;margin:32px}table{border-collapse:collapse}td,th{border:1px solid #ccc;padding:10px;vertical-align:top}pre{white-space:pre-wrap}</style>' +
            '<h1>当前代码统一验证</h1><p>macOS · OSXUniversal · Unity ' + html(state.UnityVersion) + ' · ' + html(state.Commit) +
            '</p><p>成功：' + summary.Success + '。日常回归 ' + summary.Passed + '/' + summary.RegressionCases +
            '，已知限制 ' + summary.KnownLimitations + '，诊断 ' + summary.DiagnosticsPassed + '/' + summary.Diagnostics +
            '。</p><p>源码/Git/暂存区不变：' + summary.ProductionUnchanged + '；验证源码：' + runnersUnchanged +
            '；隔离代码：' + summary.SnapshotCodeUnchanged + '；入口：' + flags.ScriptUnchanged + '；依赖配置：' + packagesUnchanged +
            '。</p><p><a href="prepare.log">准备日志</a> · <a href="summary.json">完整汇总与隔离检查</a></p><pre>' + html(summary.InfrastructureError) +
            '</pre><table><tr><th>模块</th><th>结果</th><th>日常回归</th><th>已知限制</th><th>诊断</th><th>证据</th></tr>' + rows + '</table></html>\n');
        return summary.Success ? '0' : '1';
    }
    throw Error('Unknown JSON action: ' + mode);
}
JXA
}

for generated in Project Reports Bundles Scratch .validation.lock; do owned_path "$validation_root/$generated" "$validation_root"; done
mkdir "$lock" 2>/dev/null || die 'Validation is already running or a stale .validation.lock exists; check its owner before removing it.'
printf '%s\n' "$$" > "$lock/pid"
unity_pid= captured=0 copied=0 prepared=0 run_failed=0 infrastructure_error=
finish() {
    local exit_code=$? final_code
    trap - EXIT INT TERM ERR
    set +e
    if [ -n "$unity_pid" ]; then
        kill "$unity_pid" 2>/dev/null
        sleep 2
        kill -KILL "$unity_pid" 2>/dev/null
        wait "$unity_pid" 2>/dev/null
    fi
    if [ "$captured" = 1 ]; then
        [ "$exit_code" = 0 ] || run_failed=1
        snapshot_repository > "$lock/repo.after" || run_failed=1
        snapshot_tree "$sources" > "$lock/runners.after" || run_failed=1
        snapshot_code > "$lock/code.after" || run_failed=1
        snapshot_tree "$project/Packages" > "$lock/packages.after" || run_failed=1
        git -C "$repository_root" status --porcelain=v1 > "$lock/status.after" || run_failed=1
        local index_same=false script_same=false
        [ "$(hash_file "$index_path")" != "$index_hash" ] || index_same=true
        [ "$(hash_file "$validation_root/Run-Validation.sh")" != "$script_hash" ] || script_same=true
        printf '%s\n' "$infrastructure_error" > "$lock/error"
        printf '{"Failed":%s,"Copied":%s,"Prepared":%s,"IndexUnchanged":%s,"ScriptUnchanged":%s}\n' \
            "$([ "$run_failed" = 0 ] && echo false || echo true)" "$([ "$copied" = 1 ] && echo true || echo false)" \
            "$([ "$prepared" = 1 ] && echo true || echo false)" "$index_same" "$script_same" > "$lock/flags.json"
        final_code=$(json_action finish "$reports" "$lock")
        if [ "$?" != 0 ] || [ "$final_code" != 0 ]; then exit_code=1; fi
        printf 'Validation exit %s; report: %s\n' "$exit_code" "$reports/verification-report.html"
    fi
    if [ ! -L "$lock" ] && [ "$(cat "$lock/pid" 2>/dev/null)" = "$$" ]; then
        rm -f "$lock"/*
        rmdir "$lock"
    fi
    exit "$exit_code"
}
trap finish EXIT
trap 'infrastructure_error="Command failed near line $LINENO."; exit 1' ERR
trap 'infrastructure_error="Interrupted"; exit 130' INT
trap 'infrastructure_error="Terminated"; exit 143' TERM
if [ -f "$project/Temp/UnityLockfile" ] && lsof -t "$project/Temp/UnityLockfile" >/dev/null 2>&1; then die 'The isolated project is already open in Unity.'; fi
for root in "$project/Assets" "$project/ProjectSettings" "$project/Packages" "$reports" "$sources"; do
    if [ -d "$root" ] && [ -n "$(find "$root" -type l -print)" ]; then die "Directory links are not supported: $root"; fi
done
snapshot_repository > "$lock/repo.before"
snapshot_tree "$sources" > "$lock/runners.before"
git -C "$repository_root" status --porcelain=v1 > "$lock/status.before"
index_path=$(git -C "$repository_root" rev-parse --path-format=absolute --git-path index)
index_hash=$(hash_file "$index_path")
script_hash=$(hash_file "$validation_root/Run-Validation.sh")
commit=$(git -C "$repository_root" rev-parse HEAD)
mkdir -p "$reports" "$project"
case " $requested " in
    *" all "*) for file in "$reports"/* "$reports"/.[!.]* "$reports"/..?*; do [ -e "$file" ] || continue; remove_generated "$file" "$reports"; done ;;
    *) for name in $selected; do remove_generated "$reports/$name" "$reports"; done ;;
esac
printf '%s\n' "$commit" > "$reports/source-commit.txt"
: > "$lock/allowed"; : > "$lock/completed"; : > "$lock/code.before"; : > "$lock/packages.before"
json_action state "$reports" "$lock" "$commit" "$index_hash" "$unity_version" "$selected" 0 "$script_hash" ''
json_action preparation "$reports" 0 0 0
captured=1
copied_files=0
while IFS=$'\t' read -r digest relative; do
    case "$relative" in Assets/*|ProjectSettings/*|Packages/*)
        copied_files=$((copied_files + 1))
        printf '%s\n' "$relative" >> "$lock/allowed"
        [ "$relative" = "$asmdef" ] || copy_changed "$repository_root/$relative" "$project/$relative"
    esac
done < "$lock/repo.before"
for file in "$sources"/*.cs "$sources"/*.cs.meta; do
    [ -f "$file" ] || continue
    relative="$test_directory/$(basename "$file")"
    printf '%s\n' "$relative" >> "$lock/allowed"
    copy_changed "$file" "$project/$relative"
done
printf '%s\n' "$test_directory.meta" >> "$lock/allowed"
if [ ! -f "$project/$test_directory.meta" ]; then
    printf 'fileFormatVersion: 2\nguid: 97cb97e674a34dcfa25e3b744ec4d804\nfolderAsset: yes\n' > "$project/$test_directory.meta"
fi
for directory in Assets ProjectSettings Packages; do
    root="$project/$directory"
    while IFS= read -r -d '' file; do
        relative=${file#"$project/"}
        grep -Fqx "$relative" "$lock/allowed" || remove_generated "$file" "$root"
    done < <(find "$root" -type f -print0)
    while IFS= read -r -d '' directory_path; do
        [ "$directory_path" = "$root" ] || rmdir "$directory_path" 2>/dev/null || :
    done < <(find "$root" -depth -type d -print0)
done
json_action asmdef "$repository_root/$asmdef" "$project/$asmdef"
snapshot_code > "$lock/code.before"
copied=1
json_action state "$reports" "$lock" "$commit" "$index_hash" "$unity_version" "$selected" "$copied_files" "$script_hash" "$(hash_file "$project/$asmdef")"

run_unity() {
    local limit=$1 log=$2 started=$SECONDS last_progress=$SECONDS
    shift 2
    "$unity_editor" -batchmode -nographics -buildTarget OSXUniversal -projectPath "$project" -logFile "$log" "$@" &
    unity_pid=$!
    while kill -0 "$unity_pid" 2>/dev/null; do
        if [ "$((SECONDS - started))" -ge "$limit" ]; then
            kill "$unity_pid" 2>/dev/null || :
            sleep 2
            kill -KILL "$unity_pid" 2>/dev/null || :
            wait "$unity_pid" 2>/dev/null || :
            unity_pid= unity_exit=124
            printf 'Unity exceeded %s seconds; see %s\n' "$limit" "$log" >&2
            return
        fi
        if [ "$((SECONDS - last_progress))" -ge 30 ]; then printf 'Unity is running (%ss).\n' "$((SECONDS - started))"; last_progress=$SECONDS; fi
        sleep 1
    done
    unity_exit=0
    wait "$unity_pid" || unity_exit=$?
    unity_pid=
}
printf 'Synchronized %s current files; Unity %s; suites:%s\n' "$copied_files" "$unity_version" "$selected"
started=$SECONDS
run_unity "$preparation_timeout" "$reports/prepare.log" -quit
json_action preparation "$reports" 0 "$unity_exit" "$((SECONDS - started))"
[ "$unity_exit" = 0 ] || die "Preparation failed (exit $unity_exit). See prepare.log"
json_action packages "$repository_root/Packages/packages-lock.json" "$project/Packages/packages-lock.json"
cmp -s "$repository_root/Packages/manifest.json" "$project/Packages/manifest.json" || die 'Preparation changed the package manifest.'
snapshot_tree "$project/Packages" > "$lock/packages.before"
prepared=1
json_action preparation "$reports" 1 "$unity_exit" "$((SECONDS - started))"
for name in $selected; do
    case "$name" in
        paths) runner=PathValidationRunner; expected=16 ;;
        manifest) runner=ManifestValidationRunner; expected=13 ;;
        collector) runner=CollectorValidationRunner; expected=15 ;;
        packages) runner=PackageValidationRunner; expected=1 ;;
        tasks) runner=TaskSystemValidationRunner; expected=29 ;;
        lifecycle) runner=AssetLifecycleValidationRunner; expected=102 ;;
        loading) runner=AssetLoadingValidationRunner; expected=27 ;;
    esac
    mkdir -p "$reports/$name"
    remove_generated "$reports/$name/results.json" "$reports/$name"
    printf 'Running %s ...\n' "$name"
    started=$SECONDS
    run_unity "$timeout" "$reports/$name/unity.log" -executeMethod "LiteQuark.Editor.$runner.Begin"
    outcome=$(json_action suite "$reports" "$name" "$expected" "$unity_version" "$unity_exit" "$((SECONDS - started))")
    read -r success cases <<< "$outcome"
    printf '%s\n' "$name" >> "$lock/completed"
    printf '%s: success=%s, cases=%s, exit=%s\n' "$name" "$success" "$cases" "$unity_exit"
    [ "$success" = 1 ] || run_failed=1
    [ "$cases" != 0 ] || break
done
exit "$run_failed"
