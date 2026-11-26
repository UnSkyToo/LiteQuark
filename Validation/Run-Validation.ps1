#Requires -Version 7.0
param(
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
$catalog = Get-Content -Raw -LiteralPath (Join-Path $validationRoot 'Scenarios.json') | ConvertFrom-Json
$names = @($catalog.suites | ForEach-Object name)
if ($catalog.suites -isnot [array] -or !$names.Count -or ($names | Group-Object -CaseSensitive | Where-Object Count -gt 1))
{ throw 'Missing or duplicate suite names in Scenarios.json.' }
foreach ($entry in $catalog.suites)
{
    if ($entry.name -isnot [string] -or $entry.runner -isnot [string] -or
        $entry.name -cnotmatch '^[a-z][a-z0-9-]*\z' -or $entry.name -ceq 'all' -or $entry.runner -cnotmatch '^[A-Za-z][A-Za-z0-9]*\z')
    { throw "Invalid suite or runner in Scenarios.json: $($entry.name)." }
}
foreach ($name in $Suite)
{
    if ($name -ne 'all' -and $name -notin $names) { throw "Unknown suite: $name" }
}
$plan = @($catalog.suites | Where-Object { $Suite -contains 'all' -or $Suite -contains $_.name })
if (!$plan.Count) { throw 'No suites selected.' }
$selected = @($plan | ForEach-Object name)
$reportTemplate = Get-Content -Raw -LiteralPath (Join-Path $validationRoot 'Report.html')

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
function Get-PackageHashes
{
    $packagesRoot = Join-Path $projectRoot 'Packages'
    if (!(Test-Path -LiteralPath $packagesRoot -PathType Container)) { return }
    Get-ChildItem -LiteralPath $packagesRoot -Recurse -File -Force | ForEach-Object {
        [pscustomobject]@{
            Path = [IO.Path]::GetRelativePath($projectRoot, $_.FullName).Replace('\', '/')
            SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash
        }
    }
}
function Get-IsolatedCodeHashes
{
    $assetsRoot = Join-Path $projectRoot 'Assets'
    if (!(Test-Path -LiteralPath $assetsRoot -PathType Container)) { return }
    Get-ChildItem -LiteralPath $assetsRoot -Recurse -File -Force | Where-Object {
        $_.Name -match '\.(cs|asmdef|asmref|dll)(\.meta)?$' -or
            $_.FullName.StartsWith((Join-Path $projectRoot $testDirectory), [StringComparison]::OrdinalIgnoreCase)
    } | ForEach-Object {
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
    foreach ($file in (Get-ChildItem -LiteralPath $sourceRoot -Recurse -File))
    {
        $relative = "$testDirectory/$([IO.Path]::GetRelativePath($sourceRoot, $file.FullName).Replace('\', '/'))"
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
    $asmdef.references = @(@($asmdef.references) + @('UniTask', 'LitJson', 'LiteQuark.Runtime.Addons') | Select-Object -Unique)
    $asmdefCopy = Assert-ChildPath (Join-Path $projectRoot $editorAsmdef) $projectRoot
    $asmdefJson = $asmdef | ConvertTo-Json -Depth 20
    if (!(Test-Path $asmdefCopy) -or [IO.File]::ReadAllText($asmdefCopy).Trim() -ne $asmdefJson.Trim())
    { [IO.File]::WriteAllText($asmdefCopy, $asmdefJson, [Text.UTF8Encoding]::new($false)) }
    $snapshotHashes = @($initialHashes | Where-Object { $_.Path -in $copyPaths -and $_.Path -ne $editorAsmdef })
    $snapshotChanges = @(Get-Changes $snapshotHashes $projectRoot)
    if ($snapshotChanges.Count) { throw "Snapshot mismatch: $($snapshotChanges -join ', ')" }
    $scriptHash = (Get-FileHash -LiteralPath $PSCommandPath).Hash
    $runnerHashes = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | ForEach-Object {
        [pscustomobject]@{ Path = [IO.Path]::GetRelativePath($sourceRoot, $_.FullName).Replace('\', '/'); SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
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
        AssemblyAdapter = 'Current LiteQuark.Editor.asmdef plus UniTask, LitJson and LiteQuark.Runtime.Addons references in isolated Project only.'
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
    $packageHashes = @(Get-PackageHashes)
    if ((Get-FileHash (Join-Path $projectRoot 'Packages/manifest.json')).Hash -ne
        (Get-FileHash (Join-Path $repositoryRoot 'Packages/manifest.json')).Hash)
    { throw 'Preparation changed the copied package manifest.' }
    $preparation.Success = $true
    foreach ($runner in ($plan.runner | Select-Object -Unique))
    {
        $suiteNames = @($plan | Where-Object runner -ceq $runner | ForEach-Object name)
        $suiteArgument = $suiteNames -join ','
        foreach ($name in $suiteNames)
        {
            $suiteRoot = Assert-ChildPath (Join-Path $reportRoot $name) $reportRoot
            New-Item -ItemType Directory -Path $suiteRoot -Force | Out-Null
            $resultPath = Join-Path $suiteRoot 'results.json'
            if (Test-Path -LiteralPath $resultPath) { Remove-Generated $resultPath $suiteRoot }
        }
        $logPath = Assert-ChildPath (Join-Path $reportRoot "$($suiteNames[0])/unity.log") $reportRoot
        $started = [DateTime]::UtcNow
        $exitCode = $null
        $processError = $null
        try
        {
            Write-Output "Running $suiteArgument ..."
            $process = Start-Process -FilePath $UnityEditor -ArgumentList @(
                '-batchmode', '-nographics', '-buildTarget', 'Win64', '-projectPath', ('"' + $projectRoot + '"'),
                '-executeMethod', ('LiteQuark.Editor.' + $runner + '.Begin'), '-validationSuites', $suiteArgument,
                '-logFile', ('"' + $logPath + '"')
            ) -WindowStyle Hidden -PassThru
            Wait-Unity $process $TimeoutSeconds $suiteArgument $logPath
            $exitCode = $process.ExitCode
        }
        catch { $processError = $_.Exception.Message }
        finally { if ($process) { $process.Dispose(); $process = $null } }
        $duration = [math]::Round(([DateTime]::UtcNow - $started).TotalSeconds, 1)
        $groupSummaries = @(foreach ($name in $suiteNames)
        {
            $suiteRoot = Join-Path $reportRoot $name
            $suiteLog = Assert-ChildPath (Join-Path $suiteRoot 'unity.log') $reportRoot
            if ($suiteLog -ne $logPath -and (Test-Path -LiteralPath $logPath -PathType Leaf))
            { Copy-Item -LiteralPath $logPath -Destination $suiteLog }
            $resultPath = Join-Path $suiteRoot 'results.json'
            $summary = [ordered]@{
                Suite = $name; Cases = 0; Passed = 0; Failed = 0; RegressionCases = 0
                KnownLimitations = 0; Diagnostics = 0; DiagnosticsPassed = 0
                Assertions = 0; Success = $false; Error = $null
                Failures = @(); KnownLimitationDetails = @(); DiagnosticDetails = @()
            }
            try
            {
                if ($processError) { throw $processError }
                if (!(Test-Path -LiteralPath $resultPath) -or (Get-Item $resultPath).LastWriteTimeUtc -lt $started)
                { throw "No fresh result for $name; Unity exit $exitCode. See $suiteLog" }
                $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json -AsHashtable
                $reported = $result.summary
                if ($result.cases -isnot [array] -or $reported.Suite -cne $name -or
                    $reported.Cases -ne $result.cases.Count -or $reported.Success -isnot [bool])
                { throw "No complete result for $name. See $suiteLog" }
                if ($result.unityVersion -cne $unityVersion)
                { throw "Unexpected Unity version for ${name}: $($result.unityVersion)." }
                $summary = $reported
            }
            catch { $summary.Error = $_.Exception.Message }
            $summary.ExitCode = $exitCode
            $summary.DurationSeconds = $duration
            $summary
        })
        $complete = @($groupSummaries | Where-Object Cases -eq 0).Count -eq 0
        $expectedExitCode = if ($groupSummaries | Where-Object { !$_.Success }) { 2 } else { 0 }
        $normalExit = $complete -and $exitCode -eq $expectedExitCode
        foreach ($summary in $groupSummaries)
        {
            if ($summary.Success -and !$normalExit)
            {
                $summary.Success = $false
                $summary.Error = "Unity process for $suiteArgument returned exit $exitCode or did not finish all suites. See $logPath"
            }
            if (!$summary.Success) { $runFailed = $true }
            $summaries.Add([pscustomobject]$summary)
            $summary | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $reportRoot "$($summary.Suite)/summary.json") -Encoding utf8
            $status = if ($summary.Success) { 'PASS' } else { 'FAIL' }
            Write-Output "$($summary.Suite) [$status]: regression $($summary.Passed)/$($summary.RegressionCases), failed $($summary.Failed), known limitations $($summary.KnownLimitations), diagnostics $($summary.DiagnosticsPassed)/$($summary.Diagnostics), $($summary.Assertions) assertions, exit $($summary.ExitCode)"
        }
        if (!$complete) { break }
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
        $packagesUnchanged = $false
        if ($preparation.Success)
        {
            $currentPackageHashes = @(Get-PackageHashes)
            $packagesUnchanged = $packageHashes.Count -gt 0 -and $packageHashes.Count -eq $currentPackageHashes.Count -and
                !(Compare-Object $packageHashes $currentPackageHashes -Property Path,SHA256 -CaseSensitive)
        }
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
            Assertions = ($summaries | Measure-Object Assertions -Sum).Sum
            ProductionUnchanged = $productionUnchanged; SourceChanges = $sourceChanges; PathChanges = $pathChanges
            SourceContentsUnchanged = $sourceContentsUnchanged
            StatusUnchanged = $statusUnchanged; IndexUnchanged = $indexUnchanged; RunnersUnchanged = $runnersUnchanged
            SnapshotCodeUnchanged = $snapshotUnchanged; SnapshotChanges = $copyChanges; ScriptUnchanged = $scriptUnchanged
            Suites = @($summaries.ToArray()); SelectedSuites = $selected
        }
        $summaryJson = $summary | ConvertTo-Json -Depth 10
        Set-Content (Join-Path $reportRoot 'summary.json') $summaryJson -Encoding utf8
        $html = $reportTemplate.Replace('{{SUMMARY_JSON}}', $summaryJson.Replace('<', '\u003c'))
        Set-Content (Join-Path $reportRoot 'verification-report.html') $html -Encoding utf8
        Write-Output "Production unchanged: $productionUnchanged; report: $(Join-Path $reportRoot 'verification-report.html')"
    }
    if ($lock) { $lock.Dispose(); Remove-Item -LiteralPath (Join-Path $validationRoot '.validation.lock') -Force }
}
if ($runFailed -or $summaries.Count -ne $selected.Count) { exit 1 }
