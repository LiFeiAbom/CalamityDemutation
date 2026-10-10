# CalamityDemutation

**来自旧日灾厄模组的记忆 —— 你还记得那些失落已久的记忆吗？如今，它们回来了。**
Memories from the old Calamity mod — do you still remember those long-lost memories? Now, they have returned.

一个灾厄扩展模组（tModLoader 1.4.4 / net8.0），主题是「灾厄旧版内容回归」：把旧版灾厄的武器、盔甲、饰品与机制重新带回来。
软依赖 `CalamityMod` 与 `CalamityModClassicPreTrailer`（见 `build.txt` 的 `weakReferences`）；代码里一律用
`ModLoader.TryGetMod` / `TryFind<ModItem|ModTile|ModBuff>` 按名字取内容，不写死类型引用。

## 内容来源规则

- **允许的来源**：灾厄本体（CalamityMod 的公开源码）与 CI（CalamityInheritance）。
- **不再引入**：CWR（CalamityOverhaul）与 CE（CalamityEntropy）来源的内容——已按「灾厄本体有没有同名类」逐个判定并清理过。

## 开发

- 构建：`dotnet build`；工程带 `.sln`，也可以在 Visual Studio 里直接打开。
- 进游戏前跑一遍自查脚本：
  - `Tools/CheckImplicitTextures.ps1` —— 隐式同名贴图（`类名.png`）漏了会在**加载期**抛 `MissingResourceException`
    并把整个模组禁用，`dotnet build` 查不出来。
  - `Tools/CheckResources.ps1` —— 显式资源路径（`"CalamityDemutation/..."` 这类字符串）。
- `.fx` 着色器不会被构建自动编译，新增后要手动产出同名 `.fxc` 并登记；漏了同样会在加载期禁用模组。
- 同一个目录下**不能**出现同名不同扩展名的音效（`X.ogg` 与 `X.wav` 会让模组加载失败）。
- **完整的工程口径、换算表、命名规则、踩过的坑都在 [AGENTS.md](AGENTS.md)**——那份很长，动手前先读它。

## 致谢

内容源自 Calamity Mod 及其旧版世系（含 CalamityInheritance），版权归灾厄模组原作者与团队；本工程做的是旧版内容的回归移植与适配。
