#requires -version 5.1
<#
  类同名隐式贴图自检（2026-10-09 新增，起因：CosmicEnergyBuff 忘了拷图标、
  进游戏时抛 MissingResourceException 并把整个模组禁用）。

  规则：对每个继承 ModItem / ModProjectile / ModBuff / ModNPC / ModTile / ModDust 的类文件，
  如果它**没有**写 `override string Texture`，就要求同目录下存在"与类文件同名的 .png"。

  为什么需要它：`dotnet build` 不报、`Tools/CheckResources.ps1` 只核显式字符串路径，
  两者都覆盖不到隐式同名贴图——只有进游戏加载期才会炸。

  用法：powershell -ExecutionPolicy Bypass -File .\Tools\CheckImplicitTextures.ps1
  退出码：0 = 全部命中；1 = 有缺失（会列出文件）。
#>
param([string]$Root = (Split-Path -Parent $PSScriptRoot))

$targets = @(':ModItem', ':ModProjectile', ':ModBuff', ':ModNPC', ':ModTile', ':ModDust')
$missing = New-Object System.Collections.Generic.List[string]
$checked = 0

$files = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' }

foreach ($f in $files) {
    $text = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8
    $isTarget = $false
    foreach ($t in $targets) {
        if ($text.Contains($t)) { $isTarget = $true; break }
    }
    if (-not $isTarget) { continue }
    if ($text -match 'override\s+string\s+Texture') { continue }

    $checked++
    $png = [System.IO.Path]::ChangeExtension($f.FullName, '.png')
    if (-not (Test-Path -LiteralPath $png)) {
        $missing.Add($f.FullName.Substring($Root.Length + 1))
    }
}

Write-Output ("走隐式同名贴图的类文件：{0} 个" -f $checked)
if ($missing.Count -gt 0) {
    Write-Output ("缺少同名 .png 的：{0} 个" -f $missing.Count)
    foreach ($m in $missing) { Write-Output ("  " + $m) }
    exit 1
}
Write-Output '全部命中：没有类同名贴图缺失。'
exit 0
