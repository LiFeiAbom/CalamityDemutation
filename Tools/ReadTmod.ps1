<#
.SYNOPSIS
    直读已安装的 .tmod（tModLoader 的 mod 包）——按工程记忆 §6 的格式手解，不依赖 tML。
.DESCRIPTION
    .tmod 结构：'TMOD' + tML 版本串(长度前缀) + 16 字节哈希 + 明文文件表 + 逐文件原始 deflate 流。
    文件表 = mod 名、mod 版本、int32 文件数，然后每条 = 路径(长度前缀) + int32 未压缩长度 + int32 磁盘长度。
    典型用法：
      # 看 mod 版本 + 文件清单
      powershell -ExecutionPolicy Bypass -File .\Tools\ReadTmod.ps1 -Tmod <x.tmod> -List
      # 只列某类文件
      powershell -ExecutionPolicy Bypass -File .\Tools\ReadTmod.ps1 -Tmod <x.tmod> -List -Match 'hjson$'
      # 解出文件到临时目录
      powershell -ExecutionPolicy Bypass -File .\Tools\ReadTmod.ps1 -Tmod <x.tmod> -Extract 'CalamityMod\.dll$' -OutDir $env:TEMP\tmod_out
      # 用 Cecil 反查程序集里的类型（正则匹配全名）
      powershell -ExecutionPolicy Bypass -File .\Tools\ReadTmod.ps1 -Tmod <x.tmod> -Types 'Items\.Accessories\.Wings\.' 
#>
param(
    [Parameter(Mandatory = $true)][string]$Tmod,
    [switch]$List,
    [string]$Match = '.',
    [string]$Extract,
    [string]$OutDir,
    [string[]]$Types,
    [string]$Cecil = 'D:\Game\Steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll',
    [string]$DumpType,
    [string]$DumpMethods = 'AddRecipes'
)

# 只给了 -Extract 没给 -Match 时，别把整个包都解一遍
if (-not $PSBoundParameters.ContainsKey('Match') -and $Extract) { $Match = $Extract }

function Read-7BitString {
    param($Stream)
    $shift = 0
    $val = 0
    while ($true) {
        $b = $Stream.ReadByte()
        if ($b -lt 0) { throw 'eof' }
        $val = $val -bor (($b -band 0x7F) -shl $shift)
        $shift += 7
        if (($b -band 0x80) -eq 0) { break }
        if ($shift -gt 28) { throw 'badlen' }
    }
    if ($val -lt 0 -or $val -gt 4096) { throw 'badlen' }
    $buf = New-Object byte[] $val
    $read = 0
    while ($read -lt $val) {
        $n = $Stream.Read($buf, $read, $val - $read)
        if ($n -le 0) { throw 'eof' }
        $read += $n
    }
    return [System.Text.Encoding]::UTF8.GetString($buf)
}

function Read-I32 {
    param($Stream)
    $buf = New-Object byte[] 4
    $read = 0
    while ($read -lt 4) {
        $n = $Stream.Read($buf, $read, 4 - $read)
        if ($n -le 0) { throw 'eof' }
        $read += $n
    }
    return [System.BitConverter]::ToInt32($buf, 0)
}

# 头部实测布局（2026-10-14 校准）：'TMOD'(4) + tML 版本串(7 位前缀) + 20 字节哈希 + 一段零填充，
# 之后才是明文文件表 = mod 名(7 位前缀串)、mod 版本(同上)、int32 文件数、每条(路径、int32 未压缩长度、int32 磁盘长度)。
# 零填充长度不固定，所以这里从 35 起逐字节试解析，用「数据区长度 == 文件长度 - 表尾」来校验。
function Read-TmodHeader {
    param([string]$Path)
    $fs = [System.IO.File]::OpenRead($Path)
    $magicBytes = New-Object byte[] 4
    $null = $fs.Read($magicBytes, 0, 4)
    $magic = [System.Text.Encoding]::ASCII.GetString($magicBytes)
    if ($magic -ne 'TMOD') { $fs.Dispose(); throw "不是 .tmod（magic=$magic）" }
    $tmodVersion = Read-7BitString -Stream $fs
    $limit = [Math]::Min($fs.Length, 65536)
    for ($off = 35; $off -lt $limit; $off++) {
        try {
            $fs.Position = $off
            $name = Read-7BitString -Stream $fs
            if ($name -notmatch '^[A-Za-z0-9_\.\- ]{2,80}$') { continue }
            $ver = Read-7BitString -Stream $fs
            if ($ver -notmatch '^[0-9][0-9A-Za-z\.\-]{0,24}$') { continue }
            $count = Read-I32 -Stream $fs
            if ($count -le 0 -or $count -gt 1000000) { continue }
            $entries = New-Object System.Collections.Generic.List[object]
            $bad = $false
            for ($i = 0; $i -lt $count; $i++) {
                $p = Read-7BitString -Stream $fs
                if ($p.Length -eq 0 -or $p -match '[\x00-\x1F]') { $bad = $true; break }
                $u = Read-I32 -Stream $fs
                $c = Read-I32 -Stream $fs
                if ($u -lt 0 -or $c -lt 0 -or $c -gt $u) { $bad = $true; break }
                $entries.Add([pscustomobject]@{ Path = $p; Uncompressed = $u; Stored = $c })
            }
            if ($bad) { continue }
            $dataOffset = $fs.Position
            $sum = 0
            foreach ($e in $entries) { $sum += $e.Stored }
            # 数据区必须恰好铺满文件剩余部分（这是判别“表位置猜对了”的硬条件）
            if ($dataOffset + $sum -ne $fs.Length) { continue }
            return [pscustomobject]@{
                Stream      = $fs
                TmodVersion = $tmodVersion
                ModName     = $name
                ModVersion  = $ver
                Count       = $count
                Entries     = $entries
                DataOffset  = $dataOffset
                TableOffset = $off
            }
        }
        catch {
            if ($fs.Position -ge $fs.Length) { break }
            continue
        }
    }
    $fs.Dispose()
    throw '没能定位文件表（.tmod 版本/格式可能变了）'
}

function Read-TmodEntryData {
    param($Header, $Entry, [long]$Offset)
    $fs = $Header.Stream
    $fs.Position = $Offset
    $stored = New-Object byte[] $Entry.Stored
    $read = 0
    while ($read -lt $Entry.Stored) {
        $n = $fs.Read($stored, $read, $Entry.Stored - $read)
        if ($n -le 0) { throw "读取 $($Entry.Path) 时提前结束" }
        $read += $n
    }
    if ($Entry.Stored -eq $Entry.Uncompressed) { return $stored }
    $out = New-Object byte[] $Entry.Uncompressed
    $ms = New-Object System.IO.MemoryStream($stored, $false)
    $ds = New-Object System.IO.Compression.DeflateStream($ms, [System.IO.Compression.CompressionMode]::Decompress)
    $off = 0
    while ($off -lt $Entry.Uncompressed) {
        $n = $ds.Read($out, $off, $Entry.Uncompressed - $off)
        if ($n -le 0) { break }
        $off += $n
    }
    $ds.Dispose()
    if ($off -ne $Entry.Uncompressed) { throw "$($Entry.Path) 解压长度不符：$off / $($Entry.Uncompressed)" }
    return $out
}

$header = Read-TmodHeader -Path $Tmod
Write-Output ("mod = {0} {1}  (打包用的 tML 版本 {2}，共 {3} 个文件)" -f $header.ModName, $header.ModVersion, $header.TmodVersion, $header.Count)
Write-Output ("解包位置：{0}" -f $Tmod)

if ($PSBoundParameters.ContainsKey('List')) {
    $header.Entries | Where-Object { $_.Path -match $Match } | ForEach-Object {
        Write-Output ("{0,10}  {1}" -f $_.Uncompressed, $_.Path)
    }
    $header.Stream.Dispose()
    return
}

$needTypes = $Types -and $Types.Count -gt 0
$needDump = [bool]$DumpType
if (-not $Extract -and -not $needTypes -and -not $needDump) {
    $header.Entries | Where-Object { $_.Path -match $Match } | ForEach-Object {
        Write-Output ("{0,10}  {1}" -f $_.Uncompressed, $_.Path)
    }
    $header.Stream.Dispose()
    return
}

# 数据区按文件表顺序紧邻排列：每条的数据起点 = 表尾 + 前面各条磁盘长度之和（可直接定位）
$offsets = New-Object 'long[]' $header.Count
$acc = [long]$header.DataOffset
for ($i = 0; $i -lt $header.Count; $i++) {
    $offsets[$i] = $acc
    $acc += $header.Entries[$i].Stored
}
$wantDll = $needTypes -or $needDump
$dllBytes = $null
$dllName = $null
for ($i = 0; $i -lt $header.Count; $i++) {
    $e = $header.Entries[$i]
    $wanted = ($e.Path -match $Match) -or `
        ($Extract -and $e.Path -match $Extract) -or `
        ($wantDll -and $e.Path -match '\.dll$')
    if (-not $wanted) { continue }
    $data = Read-TmodEntryData -Header $header -Entry $e -Offset $offsets[$i]
    if ($Extract -and $e.Path -match $Extract) {
        $dest = Join-Path $OutDir $e.Path
        if ((Split-Path $dest) -and -not (Test-Path (Split-Path $dest))) { New-Item -ItemType Directory -Path (Split-Path $dest) -Force | Out-Null }
        if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }
        [System.IO.File]::WriteAllBytes($dest, $data)
        Write-Output ("已解出 {0} -> {1} ({2} 字节)" -f $e.Path, $dest, $data.Length)
    }
    if ($wantDll -and $e.Path -match '\.dll$') {
        $dllBytes = $data
        $dllName = $e.Path
    }
}
$header.Stream.Dispose()

if (($needTypes -or $needDump) -and -not $dllBytes) { throw '包内没找到 dll' }

if ($needTypes -or $needDump) {
    Add-Type -Path $Cecil
    $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((New-Object System.IO.MemoryStream($dllBytes, $false)))
    Write-Output ("--- {0}（{1} 个类型）---" -f $dllName, $asm.MainModule.Types.Count)
    $all = New-Object System.Collections.Generic.List[object]
    foreach ($t in $asm.MainModule.Types) {
        foreach ($tt in $t.NestedTypes) { $all.Add($tt) }
        $all.Add($t)
    }
    if ($needTypes) {
        foreach ($t in $all) {
            foreach ($rx in $Types) {
                if ($t.FullName -match $rx) { Write-Output ("{0}   : {1}" -f $t.FullName, $t.BaseType); break }
            }
        }
    }
    if ($needDump) {
        foreach ($t in $all) {
            if ($t.FullName -notmatch $DumpType) { continue }
            Write-Output ("===== {0} : {1}" -f $t.FullName, $t.BaseType)
            foreach ($m in $t.Methods) {
                if (-not $m.HasBody -or $m.Name -notmatch $DumpMethods) { continue }
                Write-Output ("  -- {0}" -f $m.FullName)
                foreach ($i in $m.Body.Instructions) {
                    $op = $i.Operand
                    if ($op -is [string]) { $op = '"' + $op + '"' }
                    Write-Output ("     {0,-6} {1,-12} {2}" -f $i.Offset, $i.OpCode.Name, $op)
                }
            }
        }
    }
}
