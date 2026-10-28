<#
.SYNOPSIS
    把已经校验并解压的 Codex 独立包安装到 standalone 与 app-server-daemon 两棵树。

.DESCRIPTION
    本脚本只负责“安装已经下载并验证过的包”，不负责联网下载。
    它会：
      1. 核对包内 version、variant 和 bin\codex.exe --version；
      2. 原样复制到 standalone 与 app-server-daemon 各自的 releases；
      3. 翻转 standalone\current，并复核 PATH 上的 codex --version；
      4. 调用 FinalizeCodexDaemonUpdate.ps1 切换守护进程；
      5. 守护进程也切换成功后，按需清理旧版。

    任一步失败时，会把 standalone\current 翻回切换前的版本。
    守护进程的回滚由 FinalizeCodexDaemonUpdate.ps1 自己处理。

.NOTES
    请完全退出 Codex 后再运行本脚本。当前 Codex 会话不能安全地重启自己的后端。
    脚本需保存为 UTF-8 带 BOM + CRLF，避免 Windows PowerShell 5.1 错读中文。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackageDirectory,

    [Parameter(Mandatory)]
    [string]$Version,

    # 保留两棵安装树里的旧版本，便于手工回滚。
    [switch]$KeepOld,

    # 只检查输入与当前安装状态，不复制、不切链接、不碰守护进程。
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$codexRoot       = Join-Path $env:USERPROFILE '.codex\packages'
$standaloneRoot  = Join-Path $codexRoot 'standalone'
$standaloneRel   = Join-Path $standaloneRoot 'releases'
$daemonRoot      = Join-Path $codexRoot 'app-server-daemon'
$daemonRel       = Join-Path $daemonRoot 'releases'
$releaseName     = $Version + '-x86_64-pc-windows-msvc'
$standaloneNew   = Join-Path $standaloneRel $releaseName
$daemonNew       = Join-Path $daemonRel $releaseName
$standaloneCur   = Join-Path $standaloneRoot 'current'
$codexCli        = Join-Path $env:LOCALAPPDATA 'Programs\OpenAI\Codex\bin\codex.exe'
$finalizer       = Join-Path $PSScriptRoot 'FinalizeCodexDaemonUpdate.ps1'

function Get-LinkTarget([string]$Path) {
    $target = (Get-Item -LiteralPath $Path -Force).Target
    if ($target -is [array]) { return $target[0] }
    return $target
}

function Set-LinkTarget([string]$LinkPath, [string]$TargetPath) {
    if (Test-Path -LiteralPath $LinkPath) {
        # 不能用 Remove-Item 删 Junction；PS 5.1 会抛 NullReferenceException。
        [System.IO.Directory]::Delete($LinkPath, $false)
    }
    New-Item -ItemType Junction -Path $LinkPath -Target $TargetPath | Out-Null
}

function Get-CodexVersion([string]$Executable) {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $lines = & $Executable --version 2>&1
    $ErrorActionPreference = $previousPreference
    $match = @($lines | Where-Object { $_ -match '^codex-cli\s+' }) | Select-Object -Last 1
    if (-not $match) { return $null }
    return (($match -split '\s+')[1]).Trim()
}

function Get-DaemonVersion {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $lines = & $codexCli app-server daemon version 2>&1
    $ErrorActionPreference = $previousPreference
    $json = @($lines | Where-Object { $_.Trim().StartsWith('{') }) | Select-Object -Last 1
    if (-not $json) { return $null }
    try { return (ConvertFrom-Json $json).appServerVersion } catch { return $null }
}

function Assert-Package([string]$Root) {
    $manifestPath = Join-Path $Root 'codex-package.json'
    $binaryPath   = Join-Path $Root 'bin\codex.exe'
    if (-not (Test-Path -LiteralPath $manifestPath)) { throw "缺少包清单：$manifestPath" }
    if (-not (Test-Path -LiteralPath $binaryPath)) { throw "缺少 codex.exe：$binaryPath" }

    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    if ($manifest.version -ne $Version) {
        throw "包版本不符：期望 $Version，实际 $($manifest.version)"
    }
    if ($manifest.layoutVersion -ne 1) {
        throw "不支持的包布局：$($manifest.layoutVersion)"
    }
    if ($manifest.variant -ne 'codex') {
        throw "包变体不符：$($manifest.variant)"
    }

    $binaryVersion = Get-CodexVersion $binaryPath
    if ($binaryVersion -ne $Version) {
        throw "包内 codex.exe 版本不符：期望 $Version，实际 $binaryVersion"
    }
}

$PackageDirectory = [System.IO.Path]::GetFullPath($PackageDirectory)
if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
    throw "预解压包目录不存在：$PackageDirectory"
}
if (-not (Test-Path -LiteralPath $finalizer -PathType Leaf)) {
    throw "缺少守护进程收尾脚本：$finalizer"
}

Assert-Package $PackageDirectory

if (-not (Test-Path -LiteralPath $standaloneCur -PathType Container)) {
    throw "standalone\current 不存在：$standaloneCur"
}
$oldStandalone = Get-LinkTarget $standaloneCur
if (-not $oldStandalone) { throw "无法解析 standalone\current 的目标。" }
if ([System.IO.Path]::GetFullPath($oldStandalone) -eq [System.IO.Path]::GetFullPath($standaloneNew)) {
    throw "standalone 已经指向 $Version，无需再次安装。"
}
if (-not (Test-Path -LiteralPath $oldStandalone -PathType Container)) {
    throw "当前 standalone 版本目录不存在，无法安全回滚：$oldStandalone"
}

Write-Host "目标版本：$Version"
Write-Host "预解压包：$PackageDirectory"
Write-Host "standalone 旧版：$oldStandalone"
Write-Host "standalone 新版：$standaloneNew"
Write-Host "daemon 新版：$daemonNew"

if ($DryRun) {
    Write-Host "standalone 新版已存在：$(Test-Path -LiteralPath $standaloneNew)"
    Write-Host "daemon 新版已存在：$(Test-Path -LiteralPath $daemonNew)"
    Write-Host "DRY RUN 结束，未做任何更改。"
    exit 0
}

if (Test-Path -LiteralPath $standaloneNew) {
    throw "目标版本目录已存在，拒绝覆盖：$standaloneNew"
}
if (Test-Path -LiteralPath $daemonNew) {
    throw "目标版本目录已存在，拒绝覆盖：$daemonNew"
}

$standaloneFlipped = $false
try {
    New-Item -ItemType Directory -Path $standaloneRel, $daemonRel -Force | Out-Null

    Copy-Item -LiteralPath $PackageDirectory -Destination $standaloneNew -Recurse
    Copy-Item -LiteralPath $PackageDirectory -Destination $daemonNew -Recurse
    Assert-Package $standaloneNew
    Assert-Package $daemonNew

    Set-LinkTarget $standaloneCur $standaloneNew
    $standaloneFlipped = $true

    $cliVersion = Get-CodexVersion $codexCli
    if ($cliVersion -ne $Version) {
        throw "standalone 切换失败：PATH 上的 codex 仍为 $cliVersion"
    }
    Write-Host "standalone 切换成功：codex-cli $cliVersion"

    & $finalizer -NewVersion $Version -KeepOld:$KeepOld
    if (-not $?) { throw "FinalizeCodexDaemonUpdate.ps1 返回失败。" }

    $daemonVersion = $null
    for ($i = 0; $i -lt 20; $i++) {
        $daemonVersion = Get-DaemonVersion
        if ($daemonVersion -eq $Version) { break }
        Start-Sleep -Seconds 1
    }
    if ($daemonVersion -ne $Version) {
        throw "守护进程切换失败：期望 $Version，实际 $daemonVersion"
    }
    Write-Host "守护进程切换成功：appServerVersion = $daemonVersion"

    if (-not $KeepOld) {
        $oldFull = [System.IO.Path]::GetFullPath($oldStandalone)
        $releasesPrefix = [System.IO.Path]::GetFullPath($standaloneRel).TrimEnd('\') + '\'
        if (-not $oldFull.StartsWith($releasesPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "拒绝删除 releases 目录之外的旧版：$oldFull"
        }
        if (Test-Path -LiteralPath $oldFull) {
            Remove-Item -LiteralPath $oldFull -Recurse -Force
            Write-Host "已清理 standalone 旧版：$oldFull"
        }
    }

    Write-Host "DONE: Codex CLI 与 app-server 均已切到 $Version。"
}
catch {
    Write-Warning $_.Exception.Message
    if ($standaloneFlipped -and (Test-Path -LiteralPath $oldStandalone -PathType Container)) {
        Set-LinkTarget $standaloneCur $oldStandalone
        Write-Warning "standalone 已回滚到：$oldStandalone"
    }
    throw
}
