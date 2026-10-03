# 资源路径自检脚本（配合 AGENTS.md 第 4/6 节的「资源路径（大坑）」使用）
# 背景：`Texture =>` / `SoundStyle` 的路径写错时，编译期不报错，进游戏才会抛
#       MissingResourceException 并把**整个模组禁用**（不是"贴图不显示"）。
#
# 检查两类写法：
#   1) 字符串字面量：  "CalamityDemutation/Content/..."
#   2) 常量拼接：      CalamityDemutationConstant.UI + "FrightEnergyChargeBar"
#      （常量前缀从 CalamityDemutationConstant.cs 解析：UI / Masking / ColorBar / ExtraTextures 等）
#
# 用法（在工程根目录）：
#   powershell -ExecutionPolicy Bypass -File .\Tools\CheckResources.ps1
# 退出码 0 = 全部命中；1 = 有缺失（逐条列出 文件:行 与路径）。

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exts = @('.png', '.ogg', '.wav', '.mp3', '.fxc', '.fx')

# 1. 解析 CalamityDemutationConstant 里的路径常量
$constants = @{}
$constFile = Get-ChildItem -Path $root -Recurse -Filter 'CalamityDemutationConstant.cs' -File |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } | Select-Object -First 1
if ($constFile) {
    $constText = Get-Content -Encoding UTF8 $constFile.FullName -Raw
    foreach ($m in [regex]::Matches($constText, 'public const string (\w+)\s*=\s*"([^"]+)"')) {
        $constants[$m.Groups[1].Value] = $m.Groups[2].Value.TrimEnd('/')
    }
}

function Test-ResPath([string] $relative) {
    # 去掉可能残留的模组名前缀：常量里的值形如 "CalamityDemutation/Assets/Masking"，
    # 而磁盘路径是 <工程根>/Assets/Masking/...，直接拼会多一层目录
    if ($relative.StartsWith('CalamityDemutation/')) { $relative = $relative.Substring('CalamityDemutation/'.Length) }
    if ($relative.EndsWith('/')) { return $true }   # 纯目录前缀（如 "Assets/ColorBar/"），不是单个资源
    $base = Join-Path $root ($relative -replace '/', '\')
    foreach ($ext in $exts) {
        if (Test-Path -LiteralPath ($base + $ext)) { return $true }
    }
    return $false
}

$missing = @()
$checked = 0
$files = Get-ChildItem -Path $root -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' }

foreach ($file in $files) {
    $rel = $file.FullName.Substring($root.Length + 1)
    $lines = Get-Content -Encoding UTF8 $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNo = $i + 1
        foreach ($m in [regex]::Matches($lines[$i], '"CalamityDemutation/([^"]+)"')) {
            $checked++
            $p = $m.Groups[1].Value
            if (-not (Test-ResPath $p)) { $missing += ('{0}:{1}  缺失 -> {2}' -f $rel, $lineNo, $p) }
        }
        foreach ($m in [regex]::Matches($lines[$i], 'CalamityDemutationConstant\.(\w+)\s*\+\s*"([^"]+)"')) {
            $name = $m.Groups[1].Value
            if (-not $constants.ContainsKey($name)) { continue }
            $checked++
            $p = $constants[$name] + '/' + $m.Groups[2].Value
            if (-not (Test-ResPath $p)) { $missing += ('{0}:{1}  缺失 -> {2}' -f $rel, $lineNo, $p) }
        }
    }
}

if ($missing.Count -eq 0) {
    Write-Output ('资源路径自检通过：{0} 条引用全部命中。' -f $checked)
    exit 0
}
Write-Output ('发现 {0} 条缺失的资源引用（编译不报错，进游戏会禁用整个模组）：' -f $missing.Count)
$missing | ForEach-Object { Write-Output ('  ' + $_) }
exit 1
