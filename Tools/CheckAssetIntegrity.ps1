#requires -Version 5.1
<#
.SYNOPSIS
    Unity 工程资源完整性检查。

.DESCRIPTION
    检查下面几类问题：

      1. 被提交进仓库的 git 冲突标记（<<<<<<< / ======= / >>>>>>>）。
         .meta 里出现这类内容时，Unity 的 YAML 解析会失败，退化成“用字符串匹配取第一个 guid”。
         如果第一个 guid 不是场景所引用的那个，Inspector 里就会显示 Missing，而 art 目录里文件其实还在。
      2. 同一个 GUID 出现在多个 .meta 里（重复 GUID）。
      3. 场景 / 预制件 / 资源引用了工程里已不存在的 GUID（悬空引用）。
      4. Assets 下的资源文件缺少配套 .meta（Unity 会在每台机器上各自生成不同 GUID，合并时必然冲突，
         典型例子：Assets/Art/ui/desktop/popup.png 曾经没有 popup.png.meta）。

    模式：
      Full    全量扫描 Assets（冲突标记 + 重复 GUID + 悬空引用）。本地跑最准：有 Library/PackageCache 时
              才能解析包内置的 GUID。没有 PackageCache 时，悬空引用只作为提示（warning），不会导致失败。
      Diff    对齐基线版本，只检查本次改动：冲突标记 + 已有 .meta 的 GUID 被改动 + 重复 GUID。CI 用。
      Staged  只检查 git 暂存区里的文件是否有冲突标记。pre-commit 钩子用。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File Tools\CheckAssetIntegrity.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File Tools\CheckAssetIntegrity.ps1 -Mode Diff -Base origin/main

.NOTES
    退出码 0 = 通过，1 = 发现问题。
#>
[CmdletBinding()]
param(
    [ValidateSet('Full', 'Diff', 'Staged')][string]$Mode = 'Full',
    [string]$Base = 'origin/main',
    [string]$RepoRoot,
    [string]$IgnoreFile,
    [string]$AllowGuidChangeFile,
    [string]$MissingMetaIgnoreFile
)

# 注意：这里必须用 Continue。Windows PowerShell 5.1 下把原生命令（git）的 stderr 重定向到 $null 时，
# 若 ErrorActionPreference=Stop 会抛 NativeCommandError 直接中断脚本。
$ErrorActionPreference = 'Continue'

if (-not $RepoRoot) { $RepoRoot = Split-Path -Parent $PSScriptRoot }
if (-not $IgnoreFile) { $IgnoreFile = Join-Path $RepoRoot 'Tools/asset-integrity-ignore.txt' }
if (-not $AllowGuidChangeFile) { $AllowGuidChangeFile = Join-Path $RepoRoot 'Tools/asset-guid-change-allowlist.txt' }
if (-not $MissingMetaIgnoreFile) { $MissingMetaIgnoreFile = Join-Path $RepoRoot 'Tools/asset-missing-meta-ignore.txt' }

$AssetsRoot = Join-Path $RepoRoot 'Assets'
$PackagesRoot = Join-Path $RepoRoot 'Packages'
$PackageCacheRoot = Join-Path $RepoRoot 'Library/PackageCache'

$script:ProblemCount = 0
$script:Problems = New-Object System.Collections.Generic.List[string]
$script:Notes = New-Object System.Collections.Generic.List[string]

$MarkerRegex = [regex]'(?m)^(<{7}|>{7}|\|{7})(\s|$)'
$SeparatorRegex = [regex]'(?m)^={7}(\s|$)'
$GuidLineRegex = [regex]'(?m)^guid:\s*([0-9a-fA-F]{32})\s*$'
$AnyGuidRegex = [regex]'guid:\s*([0-9a-fA-F]{32})'
$LfsPointerPrefix = 'version https://git-lfs.github.com/spec/v1'

$ReferenceExtensions = @(
    '.unity', '.prefab', '.asset', '.mat', '.controller', '.anim', '.playable', '.mixer',
    '.spriteatlas', '.overridecontroller', '.asmref', '.shadergraph', '.shadersubgraph',
    '.rendertexture', '.terrainlayer', '.guiskin', '.fontsettings', '.preset', '.physicsmaterial2d'
)

function Add-Problem([string]$message) {
    $script:ProblemCount++
    $script:Problems.Add($message)
}

function Add-Note([string]$message) {
    $script:Notes.Add($message)
}

function Test-IsBinaryFile([string]$path) {
    $stream = [System.IO.File]::OpenRead($path)
    try {
        $buffer = New-Object byte[] 8192
        $read = $stream.Read($buffer, 0, $buffer.Length)
        for ($i = 0; $i -lt $read; $i++) {
            if ($buffer[$i] -eq 0) { return $true }
        }
    }
    finally { $stream.Dispose() }
    return $false
}

function Get-RepositoryRelativePath([string]$fullPath) {
    $root = (Resolve-Path -LiteralPath $RepoRoot).Path.TrimEnd('\', '/')
    $full = (Resolve-Path -LiteralPath $fullPath).Path
    if ($full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($root.Length).TrimStart('\', '/')
    }
    return $full
}

function Test-HasConflictMarkers([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return $false }
    if (Test-IsBinaryFile $path) { return $false }
    $text = [System.IO.File]::ReadAllText($path)
    if ($MarkerRegex.IsMatch($text)) { return $true }
    if ($text.Contains('<<<<<<<') -and $SeparatorRegex.IsMatch($text)) { return $true }
    return $false
}

function Get-MetaGuidMap([string[]]$roots) {
    $map = @{}
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $metas = Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.meta' -ErrorAction SilentlyContinue
        foreach ($meta in $metas) {
            if (Test-IsBinaryFile $meta.FullName) { continue }
            $match = $GuidLineRegex.Match([System.IO.File]::ReadAllText($meta.FullName))
            if (-not $match.Success) { continue }
            $guid = $match.Groups[1].Value.ToLowerInvariant()
            if ($map.ContainsKey($guid)) { $map[$guid] = @($map[$guid]) + $meta.FullName }
            else { $map[$guid] = @($meta.FullName) }
        }
    }
    return $map
}

function Get-GuidSet([string]$file) {
    $set = @{}
    if (-not (Test-Path -LiteralPath $file)) { return $set }
    foreach ($line in [System.IO.File]::ReadAllLines($file)) {
        $trimmed = $line
        $hashIndex = $trimmed.IndexOf('#')
        if ($hashIndex -ge 0) { $trimmed = $trimmed.Substring(0, $hashIndex) }
        $trimmed = $trimmed.Trim()
        if ($trimmed.Length -eq 0) { continue }
        $first = ($trimmed -split '\s+')[0].ToLowerInvariant()
        if ($first -match '^[0-9a-f]{32}$') { $set[$first] = $true }
    }
    return $set
}

function Test-GuidChangeAllowed([string]$relativePath, [string]$oldGuid, [string]$newGuid, [string]$allowFile) {
    if (-not (Test-Path -LiteralPath $allowFile)) { return $false }
    foreach ($line in [System.IO.File]::ReadAllLines($allowFile)) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0) { continue }
        if ($trimmed.StartsWith('#')) { continue }
        $parts = $trimmed -split '\s+'
        if ($parts.Count -lt 1) { continue }
        if ($parts[0].Replace('\', '/') -ne $relativePath.Replace('\', '/')) { continue }
        if ($parts.Count -ge 2 -and $parts[1].ToLowerInvariant() -ne $oldGuid) { continue }
        if ($parts.Count -ge 3 -and $parts[2].ToLowerInvariant() -ne $newGuid) { continue }
        return $true
    }
    return $false
}

function Get-PathSet([string]$file) {
    $set = @{}
    if (-not (Test-Path -LiteralPath $file)) { return $set }
    foreach ($line in [System.IO.File]::ReadAllLines($file)) {
        $trimmed = $line
        $hashIndex = $trimmed.IndexOf('#')
        if ($hashIndex -ge 0) { $trimmed = $trimmed.Substring(0, $hashIndex) }
        $trimmed = $trimmed.Trim()
        if ($trimmed.Length -eq 0) { continue }
        $set[$trimmed.Replace('\', '/').TrimStart('/')] = $true
    }
    return $set
}

function Get-MissingMetaFiles($ignoreSet, [string[]]$onlyRelativePaths) {
    $missing = New-Object System.Collections.Generic.List[string]
    if (-not (Test-Path -LiteralPath $AssetsRoot)) { return $missing }

    if ($onlyRelativePaths) {
        foreach ($relative in $onlyRelativePaths) {
            $normalized = $relative.Replace('\', '/')
            if (-not $normalized.StartsWith('Assets/')) { continue }
            if ($normalized.EndsWith('.meta')) { continue }
            if ($ignoreSet.ContainsKey($normalized)) { continue }
            $full = Join-Path $RepoRoot $relative
            if (-not (Test-Path -LiteralPath $full)) { continue }
            if (-not (Test-Path -LiteralPath ($full + '.meta'))) { $missing.Add($relative) }
        }
        return $missing
    }

    Get-ChildItem -LiteralPath $AssetsRoot -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        $name = $_.Name
        if ($name.EndsWith('.meta')) { return }
        if ($name.StartsWith('.')) { return }
        if ($name -eq 'desktop.ini' -or $name -eq 'Thumbs.db') { return }
        $relative = Get-RepositoryRelativePath $_.FullName
        if ($ignoreSet.ContainsKey($relative.Replace('\', '/').TrimStart('/'))) { return }
        if (-not (Test-Path -LiteralPath ($_.FullName + '.meta'))) { $missing.Add($relative) }
    }
    return $missing
}

function Test-DuplicateGuids($map) {
    # 只在 Assets 范围内判断重复 GUID。
    # Library/PackageCache 的 package samples（Samples~ 目录）里本来就有同名同 GUID 的样本副本，
    # 那是 Unity 包自带的无害重复，不该让检查失败。
    $assetsRootResolved = (Resolve-Path -LiteralPath $AssetsRoot).Path
    foreach ($key in $map.Keys) {
        $paths = @($map[$key] | Where-Object { $_.StartsWith($assetsRootResolved, [System.StringComparison]::OrdinalIgnoreCase) })
        if ($paths.Count -gt 1) {
            Add-Problem ("重复 GUID {0}：{1}" -f $key, (($paths | ForEach-Object { Get-RepositoryRelativePath $_ }) -join ' | '))
        }
    }
}

function Get-DanglingReferences($map, $ignoreSet, [bool]$strict) {
    $dangling = @{}
    $knownIgnored = 0
    $lfsSkipped = 0
    if (-not (Test-Path -LiteralPath $AssetsRoot)) { return @{ Dangling = $dangling; Known = 0; LfsSkipped = 0 } }

    $files = Get-ChildItem -LiteralPath $AssetsRoot -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $ReferenceExtensions -contains $_.Extension.ToLowerInvariant() }

    foreach ($file in $files) {
        if (Test-IsBinaryFile $file.FullName) { continue }
        $text = [System.IO.File]::ReadAllText($file.FullName)
        if ($text.StartsWith($LfsPointerPrefix)) { $lfsSkipped++; continue }
        if ($text.IndexOf('guid:') -lt 0) { continue }
        $relative = $null
        foreach ($match in $AnyGuidRegex.Matches($text)) {
            $guid = $match.Groups[1].Value.ToLowerInvariant()
            if ($guid.StartsWith('0000000000000000')) { continue }
            if ($map.ContainsKey($guid)) { continue }
            if ($ignoreSet.ContainsKey($guid)) { $knownIgnored++; continue }
            if (-not $relative) { $relative = Get-RepositoryRelativePath $file.FullName }
            if ($dangling.ContainsKey($guid)) { $dangling[$guid] = @($dangling[$guid]) + $relative }
            else { $dangling[$guid] = @($relative) }
        }
    }

    foreach ($key in $dangling.Keys) {
        $where = (@($dangling[$key]) | Sort-Object -Unique) -join ', '
        $message = "悬空 GUID 引用 {0} <- {1}" -f $key, $where
        if ($strict) { Add-Problem $message } else { Add-Note $message }
    }

    return @{ Dangling = $dangling; Known = $knownIgnored; LfsSkipped = $lfsSkipped }
}

function Get-ChangedFiles([string]$fromRevision, [string]$toRevision) {
    $output = & git -C $RepoRoot -c core.quotepath=false diff --name-status --no-renames $fromRevision $toRevision 2>$null
    $result = @()
    foreach ($line in @($output)) {
        if ($line -match '^([A-Z])\s+(.+)$') {
            $result += [pscustomobject]@{ Status = $Matches[1]; Path = $Matches[2] }
        }
    }
    return $result
}

function Get-GuidFromText([string]$text) {
    if (-not $text) { return $null }
    $match = $AnyGuidRegex.Match($text)
    if ($match.Success) { return $match.Groups[1].Value.ToLowerInvariant() }
    return $null
}

Write-Host ("资源完整性检查：模式={0}  仓库={1}" -f $Mode, $RepoRoot)

switch ($Mode) {

    'Staged' {
        $staged = & git -C $RepoRoot -c core.quotepath=false diff --cached --name-only --diff-filter=ACM 2>$null
        $checked = 0
        foreach ($relative in @($staged)) {
            if ([string]::IsNullOrWhiteSpace($relative)) { continue }
            $full = Join-Path $RepoRoot $relative
            if (-not (Test-Path -LiteralPath $full)) { continue }
            $checked++
            if (Test-HasConflictMarkers $full) { Add-Problem "存在冲突标记：$relative" }
        }
        Write-Host ("  已检查暂存文件 {0} 个" -f $checked)
    }

    'Diff' {
        $mergeBase = (& git -C $RepoRoot merge-base $Base HEAD 2>$null | Select-Object -First 1)
        if ([string]::IsNullOrWhiteSpace($mergeBase)) { throw "找不到 $Base 与 HEAD 的共同祖先（merge-base）" }
        Write-Host ("  基线 {0} -> {1}" -f $Base, $mergeBase.Substring(0, 8))

        $changed = Get-ChangedFiles $mergeBase 'HEAD'
        Write-Host ("  本次改动文件 {0} 个" -f $changed.Count)

        $missingMetaIgnore = Get-PathSet $MissingMetaIgnoreFile

        foreach ($item in $changed) {
            $relative = $item.Path
            $full = Join-Path $RepoRoot $relative
            if ((Test-Path -LiteralPath $full) -and (Test-HasConflictMarkers $full)) {
                Add-Problem "存在冲突标记：$relative"
            }
            if ($item.Status -ne 'D') {
                foreach ($missing in (Get-MissingMetaFiles $missingMetaIgnore @($relative))) {
                    Add-Problem "缺少 .meta（Unity 会各自生成新 GUID，合并必然冲突）：$missing"
                }
            }
            if ($relative.EndsWith('.meta') -and $item.Status -eq 'M' -and (Test-Path -LiteralPath $full)) {
                $oldText = (& git -C $RepoRoot show ("{0}:{1}" -f $mergeBase, $relative) 2>$null) -join "`n"
                $oldGuid = Get-GuidFromText $oldText
                $newGuid = Get-GuidFromText ([System.IO.File]::ReadAllText($full))
                if ($oldGuid -and $newGuid -and $oldGuid -ne $newGuid) {
                    if (Test-GuidChangeAllowed $relative $oldGuid $newGuid $AllowGuidChangeFile) {
                        Add-Note ("允许的 GUID 变更：{0}  {1} -> {2}" -f $relative, $oldGuid, $newGuid)
                    }
                    else {
                        Add-Problem ("已有 .meta 的 GUID 被改动（会把场景/预制件里的引用打断）：{0}  {1} -> {2}；如确属有意，请登记到 Tools/asset-guid-change-allowlist.txt" -f $relative, $oldGuid, $newGuid)
                    }
                }
            }
        }

        $map = Get-MetaGuidMap @($AssetsRoot, $PackagesRoot)
        Test-DuplicateGuids $map
    }

    'Full' {
        $markerFiles = New-Object System.Collections.Generic.List[string]
        if (Test-Path -LiteralPath $AssetsRoot) {
            Get-ChildItem -LiteralPath $AssetsRoot -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
                if (Test-HasConflictMarkers $_.FullName) { $markerFiles.Add((Get-RepositoryRelativePath $_.FullName)) }
            }
        }
        foreach ($file in $markerFiles) { Add-Problem "存在冲突标记：$file" }
        Write-Host ("  冲突标记：{0} 个文件" -f $markerFiles.Count)

        $missingMetaIgnore = Get-PathSet $MissingMetaIgnoreFile
        $missingMetas = Get-MissingMetaFiles $missingMetaIgnore $null
        foreach ($file in $missingMetas) { Add-Problem "缺少 .meta（Unity 会各自生成新 GUID，合并必然冲突）：$file" }
        Write-Host ("  缺少 .meta：{0} 个文件" -f $missingMetas.Count)

        $map = Get-MetaGuidMap @($AssetsRoot, $PackagesRoot, $PackageCacheRoot)
        Test-DuplicateGuids $map
        Write-Host ("  已登记 .meta 的 GUID：{0} 个" -f $map.Count)

        $hasPackageCache = Test-Path -LiteralPath $PackageCacheRoot
        if (-not $hasPackageCache) {
            Add-Note "没有找到 Library/PackageCache，包内置 GUID 无法解析，悬空引用只作提示；请在 Unity 工程目录下运行以获得完整结果。"
        }

        $result = Get-DanglingReferences $map (Get-GuidSet $IgnoreFile) $hasPackageCache
        $danglingCount = $result.Dangling.Count
        Write-Host ("  悬空引用：{0} 个 GUID（其中 {1} 处在基线清单中忽略）" -f $danglingCount, $result.Known)
        if ($result.LfsSkipped -gt 0) {
            Add-Note ("有 {0} 个文件还是 git-lfs 指针（未拉取实体），已跳过引用扫描；可先执行 git lfs pull。" -f $result.LfsSkipped)
        }
    }
}

Write-Host ''
if ($script:Notes.Count -gt 0) {
    Write-Host '提示：'
    foreach ($note in $script:Notes) { Write-Host ("  - {0}" -f $note) }
}

if ($script:ProblemCount -gt 0) {
    Write-Host ''
    Write-Host ("检查未通过：[X] {0} 个问题" -f $script:ProblemCount) -ForegroundColor Red
    foreach ($problem in $script:Problems) { Write-Host ("  [X] {0}" -f $problem) -ForegroundColor Red }
    exit 1
}

Write-Host '检查通过：[OK] 未发现问题' -ForegroundColor Green
exit 0
