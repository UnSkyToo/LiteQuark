#Requires -Version 7.0
param(
    [ValidateSet('all', 'paths', 'manifest', 'collector', 'packages', 'tasks', 'lifecycle', 'loading')]
    [string[]]$Suite = @('all'),
    [string]$UnityEditor,
    [ValidateRange(60, 3600)][int]$TimeoutSeconds = 600,
    [ValidateRange(60, 7200)][int]$PreparationTimeoutSeconds = 1800
)

$ErrorActionPreference = 'Stop'
if (!$IsWindows) { throw 'Use Run-Validation.sh on macOS; this entry is for Windows.' }
$validationRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = Split-Path $validationRoot -Parent
$projectRoot = Join-Path $validationRoot 'Project'
$reportRoot = Join-Path $validationRoot 'Reports'
$sourceRoot = Join-Path $validationRoot 'Sources'
$editorAsmdef = 'Assets/LiteQuark/Editor/LiteQuark.Editor.asmdef'
$testDirectory = 'Assets/LiteQuark/Editor/SessionValidation'
$versionText = Get-Content (Join-Path $repositoryRoot 'ProjectSettings/ProjectVersion.txt') -Raw
$unityVersion = [regex]::Match($versionText, 'm_EditorVersion: (\S+)').Groups[1].Value
if (!$UnityEditor) { $UnityEditor = "C:/Program Files/Unity/Hub/Editor/$unityVersion/Editor/Unity.exe" }
if (!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity editor not found: $UnityEditor" }
if ((Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion -notmatch ('^' + [regex]::Escape($unityVersion) + '(?:_|\s|$)'))
{ throw "Unity editor must match ProjectVersion.txt: $unityVersion" }
$null = Get-Command git -ErrorAction Stop
$suites = [ordered]@{
    paths = @{ Runner = 'PathValidationRunner'; Cases = 16 }
    manifest = @{ Runner = 'ManifestValidationRunner'; Cases = 13 }
    collector = @{ Runner = 'CollectorValidationRunner'; Cases = 15 }
    packages = @{ Runner = 'PackageValidationRunner'; Cases = 1 }
    tasks = @{ Runner = 'TaskSystemValidationRunner'; Cases = 34 }
    lifecycle = @{ Runner = 'AssetLifecycleValidationRunner'; Cases = 116 }
    loading = @{ Runner = 'AssetLoadingValidationRunner'; Cases = 27 }
}
$selected = @($suites.Keys | Where-Object { $Suite -contains 'all' -or $Suite -contains $_ })

function Assert-ChildPath([string]$Path, [string]$Parent)
{
    $full = [IO.Path]::GetFullPath($Path)
    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd('\', '/')
    $prefix = $parentFull + [IO.Path]::DirectorySeparatorChar
    if (!$full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Path outside owned directory: $full" }
    $cursor = $full
    while ($cursor.Length -ge $parentFull.Length)
    {
        $item = Get-Item -LiteralPath $cursor -Force -ErrorAction SilentlyContinue
        if ($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint))
        { throw "Reparse point is not supported in generated output: $cursor" }
        $cursor = Split-Path $cursor -Parent
    }
    return $full
}
function Remove-Generated([string]$Path, [string]$Parent)
{
    $full = Assert-ChildPath $Path $Parent
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
}
function Copy-Changed([string]$Source, [string]$Destination)
{
    $null = Assert-ChildPath $Destination $projectRoot
    if (!(Test-Path -LiteralPath $Destination) -or (Get-FileHash -LiteralPath $Source).Hash -ne (Get-FileHash -LiteralPath $Destination).Hash)
    {
        New-Item -ItemType Directory -Path (Split-Path $Destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $Source -Destination $Destination
    }
}
function Get-RepositoryPaths
{
    $paths = @(git -C $repositoryRoot -c core.quotepath=false ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate the current working tree.' }
    return @($paths | Where-Object { Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf } | Sort-Object -Unique)
}
function Get-Changes($Hashes, [string]$Base)
{
    foreach ($row in $Hashes)
    {
        $path = Join-Path $Base $row.Path
        if (!(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-FileHash -LiteralPath $path).Hash -ne $row.SHA256) { $row.Path }
    }
}
function Get-IsolatedCodeHashes
{
    $assetsRoot = Join-Path $projectRoot 'Assets'
    if (!(Test-Path -LiteralPath $assetsRoot -PathType Container)) { return }
    Get-ChildItem -LiteralPath $assetsRoot -Recurse -File -Force | Where-Object { $_.Name -match '\.(cs|asmdef|asmref|dll)(\.meta)?$' } | ForEach-Object {
        [pscustomobject]@{
            Path = [IO.Path]::GetRelativePath($projectRoot, $_.FullName).Replace('\', '/')
            SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash
        }
    }
}
function Wait-Unity($UnityProcess, [int]$Timeout, [string]$Label, [string]$LogPath)
{
    $started = [DateTime]::UtcNow
    $lastProgress = $started
    while (!$UnityProcess.WaitForExit(1000))
    {
        if (([DateTime]::UtcNow - $started).TotalSeconds -gt $Timeout)
        { $UnityProcess.Kill(); $UnityProcess.WaitForExit(); throw "$Label exceeded $Timeout seconds. See $LogPath" }
        if (([DateTime]::UtcNow - $lastProgress).TotalSeconds -ge 30)
        { Write-Output "$Label is running ($([int]([DateTime]::UtcNow - $started).TotalSeconds)s)."; $lastProgress = [DateTime]::UtcNow }
    }
}
function Assert-PackageResolution
{
    $expected = (Get-Content (Join-Path $repositoryRoot 'Packages/packages-lock.json') -Raw | ConvertFrom-Json -AsHashtable).dependencies
    $actual = (Get-Content (Join-Path $projectRoot 'Packages/packages-lock.json') -Raw | ConvertFrom-Json -AsHashtable).dependencies
    if (@(Compare-Object @($expected.Keys) @($actual.Keys)).Count) { throw 'Resolved package set differs from the main project lockfile.' }
    foreach ($name in $expected.Keys)
    {
        foreach ($field in @('version', 'source', 'hash', 'url'))
        {
            if ($expected[$name][$field] -cne $actual[$name][$field])
            { throw "Resolved package differs from the main project lockfile: $name ($field)." }
        }
    }
}

$lock = $null
$process = $null
$initialHashes = @()
$runnerHashes = @()
$codeHashes = @()
$packageHashes = @()
$state = $null
$scriptHash = $null
$infrastructureError = $null
$preparation = [ordered]@{ Success = $false; ExitCode = $null; DurationSeconds = 0 }
$runFailed = $false
$summaries = [Collections.Generic.List[object]]::new()
try
{
    foreach ($generated in @('Project', 'Reports', 'Bundles', 'Scratch', '.validation.lock'))
    { $null = Assert-ChildPath (Join-Path $validationRoot $generated) $validationRoot }
    $lock = [IO.File]::Open((Join-Path $validationRoot '.validation.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    $unityLock = Join-Path $projectRoot 'Temp/UnityLockfile'
    if (Test-Path -LiteralPath $unityLock)
    {
        try { $probe = [IO.File]::Open($unityLock, 'Open', 'ReadWrite', 'None'); $probe.Dispose() }
        catch { throw 'The isolated validation project is already open in Unity.' }
    }
    # Reject links before any snapshot writes or cleanup, including special-case files.
    foreach ($relative in @('Project/Assets', 'Project/ProjectSettings', 'Project/Packages', 'Reports', 'Sources'))
    {
        $root = Assert-ChildPath (Join-Path $validationRoot $relative) $validationRoot
        if (!(Test-Path -LiteralPath $root)) { continue }
        $link = Get-ChildItem -LiteralPath $root -Recurse -Force -Attributes ReparsePoint | Select-Object -First 1
        if ($link) { throw "Reparse point is not supported in validation files: $($link.FullName)" }
    }
    $initialPaths = @(Get-RepositoryPaths)
    $initialHashes = @(foreach ($relative in $initialPaths)
    {
        [pscustomobject]@{ Path = $relative; SHA256 = (Get-FileHash -LiteralPath (Join-Path $repositoryRoot $relative)).Hash }
    })
    $initialStatus = git --no-optional-locks -C $repositoryRoot status --porcelain=v1 | Out-String
    $indexPath = (git -C $repositoryRoot rev-parse --path-format=absolute --git-path index).Trim()
    $indexHash = (Get-FileHash -LiteralPath $indexPath).Hash
    $commit = (git -C $repositoryRoot rev-parse HEAD).Trim()
    New-Item -ItemType Directory -Path $reportRoot -Force | Out-Null
    # Full runs replace all previous evidence. Targeted runs replace only selected suites.
    if ($Suite -contains 'all')
    { Get-ChildItem -LiteralPath $reportRoot -Force | ForEach-Object { Remove-Generated $_.FullName $reportRoot } }
    else
    { foreach ($name in $selected) { Remove-Generated (Join-Path $reportRoot $name) $reportRoot } }
    $initialHashes | Export-Csv (Join-Path $reportRoot 'source-hashes.csv') -NoTypeInformation -Encoding utf8
    Set-Content (Join-Path $reportRoot 'source-commit.txt') $commit -Encoding utf8
    $copyPaths = @($initialPaths | Where-Object { $_ -match '^(Assets|ProjectSettings|Packages)/' })
    $allowedFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($relative in $copyPaths)
    {
        $null = $allowedFiles.Add($relative)
        if ($relative -ne $editorAsmdef) { Copy-Changed (Join-Path $repositoryRoot $relative) (Join-Path $projectRoot $relative) }
    }
    foreach ($file in (Get-ChildItem -LiteralPath $sourceRoot -File))
    {
        if ($file.Name -notmatch '\.cs(\.meta)?$') { continue }
        $relative = "$testDirectory/$($file.Name)"
        $null = $allowedFiles.Add($relative)
        Copy-Changed $file.FullName (Join-Path $projectRoot $relative)
    }
    $folderMeta = "$testDirectory.meta"
    $null = $allowedFiles.Add($folderMeta)
    $folderMetaCopy = Assert-ChildPath (Join-Path $projectRoot $folderMeta) $projectRoot
    if (!(Test-Path -LiteralPath $folderMetaCopy))
    { Set-Content -LiteralPath $folderMetaCopy "fileFormatVersion: 2`nguid: 97cb97e674a34dcfa25e3b744ec4d804`nfolderAsset: yes" -Encoding utf8 }
    # Sources, settings and dependency declarations all follow the same current snapshot.
    foreach ($directory in @('Assets', 'ProjectSettings', 'Packages'))
    {
        $generatedRoot = Join-Path $projectRoot $directory
        foreach ($file in @(Get-ChildItem -LiteralPath $generatedRoot -Recurse -File -Force))
        {
            $relative = [IO.Path]::GetRelativePath($projectRoot, $file.FullName).Replace('\', '/')
            if (!$allowedFiles.Contains($relative)) { Remove-Generated $file.FullName $generatedRoot }
        }
        Get-ChildItem -LiteralPath $generatedRoot -Recurse -Directory | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
            if (!(Get-ChildItem -LiteralPath $_.FullName -Force | Select-Object -First 1)) { Remove-Generated $_.FullName $generatedRoot }
        }
    }
    # Tests access internal editor APIs; derive the isolated adapter from today's asmdef.
    $asmdef = Get-Content (Join-Path $repositoryRoot $editorAsmdef) -Raw | ConvertFrom-Json
    $asmdef.references = @(@($asmdef.references) + @('UniTask', 'LitJson') | Select-Object -Unique)
    $asmdefCopy = Assert-ChildPath (Join-Path $projectRoot $editorAsmdef) $projectRoot
    $asmdefJson = $asmdef | ConvertTo-Json -Depth 20
    if (!(Test-Path $asmdefCopy) -or [IO.File]::ReadAllText($asmdefCopy).Trim() -ne $asmdefJson.Trim())
    { [IO.File]::WriteAllText($asmdefCopy, $asmdefJson, [Text.UTF8Encoding]::new($false)) }
    $snapshotHashes = @($initialHashes | Where-Object { $_.Path -in $copyPaths -and $_.Path -ne $editorAsmdef })
    $snapshotChanges = @(Get-Changes $snapshotHashes $projectRoot)
    if ($snapshotChanges.Count) { throw "Snapshot mismatch: $($snapshotChanges -join ', ')" }
    $scriptHash = (Get-FileHash -LiteralPath $PSCommandPath).Hash
    $runnerHashes = @(Get-ChildItem -LiteralPath $sourceRoot -File | ForEach-Object {
        [pscustomobject]@{ Path = $_.Name; SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
    })
    foreach ($row in $runnerHashes)
    {
        $copy = Join-Path $projectRoot "$testDirectory/$($row.Path)"
        if (!(Test-Path -LiteralPath $copy) -or (Get-FileHash -LiteralPath $copy).Hash -ne $row.SHA256)
        { throw "Validation source copy mismatch: $($row.Path)" }
    }
    $codeHashes = @(Get-IsolatedCodeHashes)
    $state = [ordered]@{
        CapturedUtc = [DateTime]::UtcNow.ToString('O'); Commit = $commit; Status = $initialStatus
        IndexSHA256 = $indexHash; UnityVersion = $unityVersion; SelectedSuites = $selected
        HostPlatform = 'Windows'; BuildTarget = 'Win64'
        ProductionFiles = $initialHashes.Count; CopiedFiles = $copyPaths.Count; RunnerHashes = $runnerHashes
        ScriptSHA256 = $scriptHash
        AssemblyAdapter = 'Current LiteQuark.Editor.asmdef plus UniTask and LitJson references in isolated Project only.'
        AssemblyAdapterSHA256 = (Get-FileHash -LiteralPath $asmdefCopy).Hash
    }
    $state | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $reportRoot 'source-state.json') -Encoding utf8
    Write-Output "Synchronized $($copyPaths.Count) current files; Unity $unityVersion; suites: $($selected -join ', ')"
    # UPM restores the main project's locked dependencies; no local package mirror is required.
    $preparationLog = Join-Path $reportRoot 'prepare.log'
    $preparationStarted = [DateTime]::UtcNow
    Write-Output 'Preparing packages and compiling the isolated project ...'
    $process = Start-Process -FilePath $UnityEditor -ArgumentList @(
        '-batchmode', '-nographics', '-quit', '-buildTarget', 'Win64',
        '-projectPath', ('"' + $projectRoot + '"'), '-logFile', ('"' + $preparationLog + '"')
    ) -WindowStyle Hidden -PassThru
    Wait-Unity $process $PreparationTimeoutSeconds 'Preparation' $preparationLog
    $preparation.ExitCode = $process.ExitCode
    $preparation.DurationSeconds = [math]::Round(([DateTime]::UtcNow - $preparationStarted).TotalSeconds, 1)
    $process.Dispose(); $process = $null
    if ($preparation.ExitCode -ne 0) { throw "Preparation failed (exit $($preparation.ExitCode)). See $preparationLog" }
    Assert-PackageResolution
    $packageHashes = @(Get-ChildItem (Join-Path $projectRoot 'Packages') -Recurse -File | ForEach-Object {
        [pscustomobject]@{ Path = [IO.Path]::GetRelativePath($projectRoot, $_.FullName); SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
    })
    if ((Get-FileHash (Join-Path $projectRoot 'Packages/manifest.json')).Hash -ne
        (Get-FileHash (Join-Path $repositoryRoot 'Packages/manifest.json')).Hash)
    { throw 'Preparation changed the copied package manifest.' }
    $preparation.Success = $true
    foreach ($name in $selected)
    {
        $entry = $suites[$name]
        $suiteRoot = Join-Path $reportRoot $name
        New-Item -ItemType Directory -Path $suiteRoot -Force | Out-Null
        $resultPath = Join-Path $suiteRoot 'results.json'
        if (Test-Path -LiteralPath $resultPath) { Remove-Generated $resultPath $suiteRoot }
        $logPath = Join-Path $suiteRoot 'unity.log'
        $started = [DateTime]::UtcNow
        $summary = [ordered]@{
            Suite = $name; Passed = 0; Failed = 0; Cases = 0; RegressionCases = 0
            KnownLimitations = 0; Diagnostics = 0; DiagnosticsPassed = 0
            Assertions = 0; ExitCode = $null; Success = $false; Error = $null
            Failures = @(); KnownLimitationDetails = @(); DiagnosticDetails = @()
        }
        try
        {
            Write-Output "Running $name ..."
            $process = Start-Process -FilePath $UnityEditor -ArgumentList @(
                '-batchmode', '-nographics', '-buildTarget', 'Win64', '-projectPath', ('"' + $projectRoot + '"'),
                '-executeMethod', ('LiteQuark.Editor.' + $entry.Runner + '.Begin'), '-logFile', ('"' + $logPath + '"')
            ) -WindowStyle Hidden -PassThru
            Wait-Unity $process $TimeoutSeconds $name $logPath
            $summary.ExitCode = $process.ExitCode
            if (!(Test-Path -LiteralPath $resultPath) -or (Get-Item $resultPath).LastWriteTimeUtc -lt $started)
            { throw "No fresh result; Unity exit $($summary.ExitCode). See $logPath" }
            $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
            $cases = if ($name -eq 'packages') { @($result) } else { @($result.cases) }
            $summary.Cases = $cases.Count
            $known = @($cases | Where-Object { $_.status -eq 'known_limitation' })
            $diagnostics = @($cases | Where-Object diagnostic)
            # Only this specific, independently confirmed Unity limitation is non-blocking.
            # Do not infer a limitation from a case name or an arbitrary failure message.
            if ($name -eq 'loading')
            {
                if ($diagnostics.Count -ne 1 -or $diagnostics[0].name -ne '17 Native same-name Bundle collision diagnostic')
                { throw 'Missing or unexpected native diagnostic classification.' }
            }
            elseif ($diagnostics.Count) { throw 'Unexpected diagnostic classification outside loading.' }
            foreach ($item in $known)
            {
                if ($name -ne 'loading' -or $known.Count -ne 1 -or $item.passed -or $item.diagnostic -or
                    $item.name -ne '08 Bundle same-name scenes in different folders' -or
                    $item.limitationId -ne 'UNITY-SAME-NAME-SCENE-BUNDLES' -or
                    !$diagnostics[0].passed -or $diagnostics[0].status -ne 'diagnostic_passed')
                { throw 'Unconfirmed or unexpected known limitation; counted as a validation failure.' }
            }
            $summary.KnownLimitations = $known.Count
            $summary.Diagnostics = $diagnostics.Count
            $summary.DiagnosticsPassed = @($diagnostics | Where-Object passed).Count
            $summary.RegressionCases = $cases.Count - $known.Count - $diagnostics.Count
            $summary.Passed = @($cases | Where-Object { $_.passed -and !$_.diagnostic }).Count
            $summary.Assertions = ($cases | ForEach-Object { if ($null -ne $_.checks) { $_.checks } else { $_.assertions } } | Measure-Object -Sum).Sum
            $summary.Failures = @($cases | Where-Object { !$_.passed -and $_.status -ne 'known_limitation' } | ForEach-Object { [pscustomobject]@{ Name = $_.name; Failure = $_.failure } })
            $summary.Failed = $summary.Failures.Count
            $summary.KnownLimitationDetails = @($known | ForEach-Object { [pscustomobject]@{ Id = $_.limitationId; Name = $_.name; Reason = $_.failure; Evidence = $_.evidence } })
            $summary.DiagnosticDetails = @($diagnostics | ForEach-Object { [pscustomobject]@{ Name = $_.name; Confirmed = $_.passed; Evidence = $_.evidence; Failure = $_.failure } })
            $harnessCleanup = @($cases | Where-Object { @($_.evidence | Where-Object { $_ -like 'Harness-only cleanup*' }).Count -gt 0 }).Count
            $summary.Success = $summary.ExitCode -eq 0 -and $summary.Cases -eq $entry.Cases -and
                ($summary.Passed + $summary.KnownLimitations + $summary.DiagnosticsPassed) -eq $entry.Cases -and $summary.Failed -eq 0 -and
                !$result.fatalError -and !@($result.unexpectedErrors | Where-Object { $_ }).Count -and !$harnessCleanup -and $result.unityVersion -eq $unityVersion
            if (!$summary.Success) { $summary.Error = "Validation failed. Expected $($entry.Cases) cases; fatal error: $($result.fatalError); harness cleanup: $harnessCleanup" }
        }
        catch { $summary.Error = $_.Exception.Message }
        finally { if ($process) { $process.Dispose(); $process = $null } }
        $summary.DurationSeconds = [math]::Round(([DateTime]::UtcNow - $started).TotalSeconds, 1)
        if (!$summary.Success) { $runFailed = $true }
        $summaries.Add([pscustomobject]$summary)
        $summary | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $suiteRoot 'summary.json') -Encoding utf8
        Write-Output "$name : regression $($summary.Passed)/$($summary.RegressionCases), failed $($summary.Failed), known limitations $($summary.KnownLimitations), diagnostics $($summary.DiagnosticsPassed)/$($summary.Diagnostics), $($summary.Assertions) assertions, exit $($summary.ExitCode)"
        if ($summary.Cases -eq 0) { break }
    }
}
catch { $runFailed = $true; $infrastructureError = $_.Exception.Message; Write-Output ("Validation infrastructure failed: " + $infrastructureError) }
finally
{
    if ($process) { if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }; $process.Dispose() }
    if ($initialHashes.Count)
    {
        $sourceChanges = @(Get-Changes $initialHashes $repositoryRoot)
        $currentPaths = @(Get-RepositoryPaths)
        $pathChanges = @(Compare-Object $initialPaths $currentPaths)
        $statusUnchanged = (git --no-optional-locks -C $repositoryRoot status --porcelain=v1 | Out-String) -eq $initialStatus
        $indexUnchanged = (Get-FileHash -LiteralPath $indexPath).Hash -eq $indexHash
        $runnersUnchanged = $runnerHashes.Count -gt 0 -and @(Get-Changes $runnerHashes $sourceRoot).Count -eq 0
        $currentCodeHashes = @(Get-IsolatedCodeHashes)
        $copyChanges = @(Compare-Object -ReferenceObject $codeHashes -DifferenceObject $currentCodeHashes -Property Path,SHA256 -CaseSensitive |
            Select-Object -ExpandProperty Path | Sort-Object -Unique -CaseSensitive)
        $snapshotUnchanged = $codeHashes.Count -gt 0 -and $copyChanges.Count -eq 0
        $scriptUnchanged = $scriptHash -and (Get-FileHash -LiteralPath $PSCommandPath).Hash -eq $scriptHash
        $packagesUnchanged = $preparation.Success -and $packageHashes.Count -gt 0 -and @(Get-Changes $packageHashes $projectRoot).Count -eq 0
        $sourceContentsUnchanged = !$sourceChanges.Count -and !$pathChanges.Count
        $productionUnchanged = $sourceContentsUnchanged -and $statusUnchanged -and $indexUnchanged
        if (!$productionUnchanged -or !$runnersUnchanged -or !$snapshotUnchanged -or !$scriptUnchanged -or !$packagesUnchanged) { $runFailed = $true }
        $summary = [ordered]@{
            FinishedUtc = [DateTime]::UtcNow.ToString('O'); Commit = $commit; UnityVersion = $unityVersion
            HostPlatform = 'Windows'; BuildTarget = 'Win64'
            Success = !$runFailed -and $summaries.Count -eq $selected.Count
            Preparation = $preparation; InfrastructureError = $infrastructureError; PackagesUnchanged = $packagesUnchanged
            Cases = ($summaries | Measure-Object Cases -Sum).Sum
            RegressionCases = ($summaries | Measure-Object RegressionCases -Sum).Sum
            Passed = ($summaries | Measure-Object Passed -Sum).Sum
            Failed = ($summaries | Measure-Object Failed -Sum).Sum
            KnownLimitations = ($summaries | Measure-Object KnownLimitations -Sum).Sum
            Diagnostics = ($summaries | Measure-Object Diagnostics -Sum).Sum
            DiagnosticsPassed = ($summaries | Measure-Object DiagnosticsPassed -Sum).Sum
            ProductionUnchanged = $productionUnchanged; SourceChanges = $sourceChanges; PathChanges = $pathChanges
            SourceContentsUnchanged = $sourceContentsUnchanged
            StatusUnchanged = $statusUnchanged; IndexUnchanged = $indexUnchanged; RunnersUnchanged = $runnersUnchanged
            SnapshotCodeUnchanged = $snapshotUnchanged; SnapshotChanges = $copyChanges; ScriptUnchanged = $scriptUnchanged
            Suites = @($summaries.ToArray()); SelectedSuites = $selected
        }
        $summary | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $reportRoot 'summary.json') -Encoding utf8
        $rows = foreach ($item in $summaries)
        {
            $status = if (!$item.Success) { 'FAIL' } elseif ($item.KnownLimitations) { 'OK · 含已知限制' } else { 'PASS' }
            $details = [Net.WebUtility]::HtmlEncode(($item.Error + "`n" + ($item.Failures | ConvertTo-Json -Depth 5)))
            $knownDetails = [Net.WebUtility]::HtmlEncode(($item.KnownLimitationDetails | ConvertTo-Json -Depth 5))
            $diagnosticDetails = [Net.WebUtility]::HtmlEncode(($item.DiagnosticDetails | ConvertTo-Json -Depth 5))
            "<tr><td>$($item.Suite)</td><td>$status</td><td>$($item.Passed)/$($item.RegressionCases)</td><td>$($item.Failed)</td><td>$($item.KnownLimitations)<pre>$knownDetails</pre></td><td>$($item.DiagnosticsPassed)/$($item.Diagnostics)<details><summary>诊断证据</summary><pre>$diagnosticDetails</pre></details></td><td>$($item.Assertions)</td><td><a href='$($item.Suite)/results.json'>结果</a> · <a href='$($item.Suite)/unity.log'>日志</a><pre>$details</pre></td></tr>"
        }
        $html = @"
<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>SessionValidation</title>
<style>body{font:16px system-ui;margin:32px;max-width:1200px}table{border-collapse:collapse;width:100%}td,th{border:1px solid #ccc;padding:10px;text-align:left;vertical-align:top}pre{white-space:pre-wrap}</style>
<h1>当前代码统一验证</h1><p>Unity $unityVersion · $commit</p>
<p>主工程文件内容保持不变：$sourceContentsUnchanged。Git 状态保持不变：$statusUnchanged。暂存区保持不变：$indexUnchanged。</p>
<p>测试源码保持不变：$runnersUnchanged。隔离代码保持不变：$snapshotUnchanged。</p>
<p>准备成功：$($preparation.Success)。依赖配置保持不变：$packagesUnchanged。<a href='prepare.log'>准备日志</a></p>
<pre>$([Net.WebUtility]::HtmlEncode($infrastructureError))</pre>
<p>本次范围：$($selected -join ', ')。日常回归 $($summary.Passed)/$($summary.RegressionCases)，失败 $($summary.Failed)，已知限制 $($summary.KnownLimitations)，诊断确认 $($summary.DiagnosticsPassed)/$($summary.Diagnostics)。共执行 $($summary.Cases) 组。</p>
<p>已知限制和诊断单独计数，均不计入日常回归通过数。只有经原生诊断确认的指定限制不阻断回归；其他失败仍返回非零退出码。</p>
<table><tr><th>模块</th><th>结果</th><th>日常回归</th><th>失败</th><th>已知限制</th><th>诊断确认</th><th>断言</th><th>证据</th></tr>$($rows -join "`n")</table></html>
"@
        Set-Content (Join-Path $reportRoot 'verification-report.html') $html -Encoding utf8
        Write-Output "Production unchanged: $productionUnchanged; report: $(Join-Path $reportRoot 'verification-report.html')"
    }
    if ($lock) { $lock.Dispose(); Remove-Item -LiteralPath (Join-Path $validationRoot '.validation.lock') -Force }
}
if ($runFailed -or $summaries.Count -ne $selected.Count) { exit 1 }
