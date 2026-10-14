#requires -version 5.1
<#
  类同名隐式贴图自检（2026-10-09 新增，起因：CosmicEnergyBuff 忘了拷图标、
  进游戏时抛 MissingResourceException 并把整个模组禁用）。

  规则：扫每个 .cs 文件里**每个**继承 ModItem / ModProjectile / ModBuff / ModNPC / ModTile / ModDust
  的类（一个文件里写了多类时逐个查——工程里 CE/CWR 来源的武器常把 物品 + 手持弹幕 + 标记弹幕 写在一个文件里），
  只要它**没有**写 `override string Texture`，就要求同目录下存在"**与类同名**的 .png"。
  抽象类（`abstract class`）不需要贴图，直接跳过。

  为什么需要它：`dotnet build` 不报、`Tools/CheckResources.ps1` 只核显式字符串路径，
  两者都覆盖不到隐式同名贴图——只有进游戏加载期才会炸。

  用法：powershell -ExecutionPolicy Bypass -File .\Tools\CheckImplicitTextures.ps1
  退出码：0 = 全部命中；1 = 有缺失（会列出文件）。

  2026-10-14 修：类识别从 `Contains(':ModItem')` 改成正则 `:\s*(ModItem|...)`。
  旧写法要求冒号**紧跟**基类名，于是 `internal class X : ModItem`（冒号前带空格）这种写法
  会被**静默跳过**、计数里看不见——移植巨龙之息（DragonsBreath/DragonsBreathRound/FuckYou
  三个类当时都带空格）时踩到：类数是 286 而非应有的 289，脚本却没报任何错。
  同一天再升级为**逐类**扫描（旧版按文件计、且一个文件只查文件名那个类）：现在计数是 450 个类
  （旧版口径 286），多出来的部分正是"冒号带空格"与"单文件多类"这两类漏网文件。
  仍保留一条近似：文件里只要出现 `override string Texture`，整个文件按"已显式指定"跳过
  （不区分是哪个类写的）——真要精确到类，得逐类切代码块再判。
#>
param([string]$Root = (Split-Path -Parent $PSScriptRoot))

$classPattern = '(?<mods>(?:abstract\s+|sealed\s+|internal\s+|public\s+|partial\s+|static\s+)*)class\s+(?<name>\w+)\s*:\s*(?<base>ModItem|ModProjectile|ModBuff|ModNPC|ModTile|ModDust)\b'
$missing = New-Object System.Collections.Generic.List[string]
$checked = 0
$abstractSkipped = 0

$files = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' }

foreach ($f in $files) {
    $text = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8
    $classMatches = [regex]::Matches($text, $classPattern)
    if ($classMatches.Count -eq 0) { continue }
    if ($text -match 'override\s+string\s+Texture') { $checked += $classMatches.Count; continue }

    foreach ($m in $classMatches) {
        if ($m.Groups['mods'].Value -match 'abstract') { $abstractSkipped++; continue }
        $checked++
        $png = Join-Path $f.DirectoryName ($m.Groups['name'].Value + '.png')
        if (-not (Test-Path -LiteralPath $png)) {
            $missing.Add($f.FullName.Substring($Root.Length + 1) + '  ->  ' + $m.Groups['name'].Value + '.png')
        }
    }
}

Write-Output ("走隐式同名贴图的类：{0} 个（另有抽象类 {1} 个按规则跳过）" -f $checked, $abstractSkipped)
if ($missing.Count -gt 0) {
    Write-Output ("缺少同名 .png 的：{0} 个" -f $missing.Count)
    foreach ($m in $missing) { Write-Output ("  " + $m) }
    exit 1
}
Write-Output '全部命中：没有类同名贴图缺失。'
exit 0
