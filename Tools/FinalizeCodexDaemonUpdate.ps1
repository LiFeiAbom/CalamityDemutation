<#
.SYNOPSIS
    把本机 app-server 守护进程从 0.161.0 切到 0.162.0，并清掉 0.161.0 残留。

.DESCRIPTION
    守护进程（codex.exe -- app-server 模式）运行时会锁住自己的安装目录，
    `Remove-Item` 删不掉、`codex app-server daemon update` 又走联网通道（本机 403）。
    所以只能手工切：
      停守护进程 -> 把 current 联接翻到已预铺的 0.162.0 包 -> 重启守护进程 ->
      确认 appServerVersion 已是 0.162.0 后，删掉 releases\0.161.0-…；
    任一步失败就翻回 0.161.0 并重启（自动回滚）。

.NOTES
    守护进程就是当前 Codex 会话的后端：停它会中断正在进行的会话。
    从 Codex 会话里拉起本脚本时，请用分离进程（Start-Process），
    这样调用方退出后脚本还能跑完。全过程写入
    ~/.codex/packages/app-server-daemon/finalize-update.log。
#>
[CmdletBinding()]
param(
    # 开跑前先等这么多秒，留给"拉起它的那一轮会话"把话说完/退出。
    [int]$DelaySeconds = 0,
    # 只检查环境并打印现状，不动守护进程（用于自检脚本本身能否跑起来）。
    [switch]$DryRun
)

$ErrorActionPreference = 'Continue'

$daemonRoot = Join-Path $env:USERPROFILE '.codex\packages\app-server-daemon'
$releases   = Join-Path $daemonRoot 'releases'
$current    = Join-Path $daemonRoot 'current'
$oldDir     = Join-Path $releases '0.161.0-x86_64-pc-windows-msvc'
$newDir     = Join-Path $releases '0.162.0-x86_64-pc-windows-msvc'
$logPath    = Join-Path $daemonRoot 'finalize-update.log'
$codexCli   = Join-Path $env:LOCALAPPDATA 'Programs\OpenAI\Codex\bin\codex.exe'

function Write-Log([string]$Message) {
    $line = '[{0}] {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

function Invoke-DaemonCmd([string]$Verb) {
    Write-Log ("  > codex app-server daemon {0}" -f $Verb)
    $out = & $codexCli app-server daemon $Verb 2>&1 | Out-String
    if ($out.Trim()) { Write-Log ('    ' + ($out.Trim() -replace "`r?`n", "`n    ")) }
    return $out
}

function Get-AppServerVersion {
    $out = & $codexCli app-server daemon version 2>&1 | Out-String
    $json = @($out -split "`n" | Where-Object { $_.Trim().StartsWith('{') }) | Select-Object -Last 1
    if (-not $json) { return $null }
    try { return (ConvertFrom-Json $json).appServerVersion } catch { return $null }
}

function Get-DaemonProcesses {
    return @(Get-Process -Name codex -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -like '*app-server-daemon*' })
}

function Set-CurrentLink([string]$Target) {
    # Junction 不能用 Remove-Item（PS 5.1 会抛 NullReferenceException）——
    # 用 .NET 的 Directory.Delete(path, recursive:false) 只摘联接、不碰目标。
    if (Test-Path -LiteralPath $current) {
        [System.IO.Directory]::Delete($current, $false)
    }
    New-Item -ItemType Junction -Path $current -Target $Target | Out-Null
    Write-Log ("  current -> {0}" -f ((Get-Item -LiteralPath $current).Target -join ', '))
}

Add-Content -LiteralPath $logPath -Value '' -Encoding UTF8
Write-Log '=== finalize: app-server daemon 0.161.0 -> 0.162.0 ==='

if (-not (Test-Path -LiteralPath $newDir)) {
    Write-Log ("ABORT: 预铺的 0.162.0 包不存在：{0}" -f $newDir)
    exit 2
}

if ($DryRun) {
    Write-Log 'DRY RUN：只报现状，不动守护进程。'
    Write-Log ("  运行中的 appServerVersion = {0}" -f (Get-AppServerVersion))
    Write-Log ("  releases\0.161.0 存在 = {0}" -f (Test-Path -LiteralPath $oldDir))
    Write-Log ("  releases\0.162.0 存在 = {0}" -f (Test-Path -LiteralPath $newDir))
    Write-Log ("  current 指向 = {0}" -f ((Get-Item -LiteralPath $current).Target -join ', '))
    Write-Log 'DRY RUN 结束（未做任何更改）。'
    exit 0
}

if ($DelaySeconds -gt 0) {
    Write-Log ("先等 {0} 秒，让拉起本脚本的那一轮会话结束…" -f $DelaySeconds)
    Start-Sleep -Seconds $DelaySeconds
}

Invoke-DaemonCmd 'stop' | Out-Null

$stopped = $false
for ($i = 0; $i -lt 30; $i++) {
    if ((Get-DaemonProcesses).Count -eq 0) { $stopped = $true; break }
    Start-Sleep -Seconds 1
}
if ($stopped) { Write-Log '  守护进程已停止。' } else { Write-Log '  警告：30 秒内仍检测到守护进程，继续尝试。' }

Set-CurrentLink $newDir
Invoke-DaemonCmd 'start' | Out-Null

$ver = $null
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 1
    $ver = Get-AppServerVersion
    if ($ver) { break }
}

if ($ver -eq '0.162.0') {
    Write-Log '切换成功：appServerVersion = 0.162.0'
    if (Test-Path -LiteralPath $oldDir) {
        Remove-Item -LiteralPath $oldDir -Recurse -Force
        Write-Log ('已删除 0.161.0 残留：{0}' -f $oldDir)
    } else {
        Write-Log '0.161.0 目录已不在（无需删除）。'
    }
    Write-Log 'DONE.'
} else {
    Write-Log ('切换失败（appServerVersion = {0}），回滚到 0.161.0…' -f $ver)
    Invoke-DaemonCmd 'stop' | Out-Null
    Start-Sleep -Seconds 3
    if (Test-Path -LiteralPath $oldDir) {
        Set-CurrentLink $oldDir
        Invoke-DaemonCmd 'start' | Out-Null
        Write-Log 'ROLLED BACK.'
    } else {
        Write-Log '无法回滚：0.161.0 目录已不存在，请手工处理。'
    }
}
