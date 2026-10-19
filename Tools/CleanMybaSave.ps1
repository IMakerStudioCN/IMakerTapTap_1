<#
.SYNOPSIS
    修剪存档里被写爆的 mybaSystemData（贴吧列表）。

.DESCRIPTION
    SaveWrittenNewsCommand 曾经自己 SendCommand 自己，导致一次提交把整个提交流程
    递归跑了上千遍：mybaSendEvent 被发了 N 次，贴吧列表累积成几千条，
    存档从几 KB 涨到 1MB+。

    这个脚本只动 mybaSystemData 这一个 entry：把它的 4 个数组各裁到最近 Keep 条，
    其它 entry（Tag / 新闻获得记录 / 草稿 / 写好的新闻……）原样保留。

    跑之前会自动备份成 SaveData.json.bak。

.PARAMETER Keep
    保留最近多少条贴吧记录。默认 10。

.PARAMETER SavePath
    存档路径。默认是 Unity 的 persistentDataPath。

.EXAMPLE
    pwsh -File Tools\CleanMybaSave.ps1
    pwsh -File Tools\CleanMybaSave.ps1 -Keep 0      # 直接清空贴吧列表
#>
[CmdletBinding()]
param(
    [int] $Keep = 10,
    [string] $SavePath = "$env:USERPROFILE\AppData\LocalLow\DefaultCompany\TapTapFirst\SaveData.json"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $SavePath)) {
    Write-Error "找不到存档：$SavePath"
    return
}

$before = (Get-Item $SavePath).Length
Write-Host "存档：$SavePath（$before 字节）"

$chars = [System.IO.File]::ReadAllText($SavePath, [System.Text.Encoding]::UTF8)
$root = $chars | ConvertFrom-Json

$target = $null
foreach ($entry in $root.entries) {
    if ($entry.key -eq 'mybaSystemData') { $target = $entry; break }
}

if ($null -eq $target) {
    Write-Warning '存档里没有 mybaSystemData，什么都不用做。'
    return
}

$inner = $target.json | ConvertFrom-Json

$fields = @('myBaLineContent', 'dataList', 'clickList', 'repliesList')
$report = @()

foreach ($field in $fields) {
    $list = $inner.$field

    if ($null -eq $list) { continue }

    $count = @($list).Count
    $kept = if ($Keep -le 0) { @() } else { @($list) | Select-Object -Last $Keep }
    $inner.$field = @($kept)
    $report += "  {0,-16} {1,6} -> {2}" -f $field, $count, @($kept).Count
}

Write-Host '贴吧列表：'
$report | ForEach-Object { Write-Host $_ }

$target.json = ($inner | ConvertTo-Json -Depth 20 -Compress)

# 备份 + 写回
$backup = "$SavePath.bak"
Copy-Item $SavePath $backup -Force
Write-Host "已备份：$backup"

$out = $root | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($SavePath, $out, (New-Object System.Text.UTF8Encoding($false)))

$after = (Get-Item $SavePath).Length
Write-Host ("完成：{0} 字节 -> {1} 字节" -f $before, $after)
