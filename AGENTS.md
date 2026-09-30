# CalamityDemutation — 工程记忆（新会话先读这份）

> 本文件是给后续会话/协作者的项目记忆：工程口径、已确立的换算表、踩过的坑、当前状态。
> 内容按"能直接照做"的标准写；改动工程时请同步更新它。

## 1. 工程是什么

灾厄扩展 mod（tModLoader 1.4.4 / net8.0，显示名 `CalamityDemutation`，作者 LF），主题是"灾厄旧版内容回归"。
软依赖：`weakReferences = CalamityMod, CalamityModClassicPreTrailer`（见 `build.txt`），代码里一律用
`ModLoader.TryGetMod` / `TryFind<ModItem|ModTile|ModBuff>` 按名字取内容，不写死类型引用。

### 内容来源规则（重要）

- **允许的来源**：灾厄本体（CalamityMod 2.0.x / 2.2.2 / 1.4.4-release 的公开源码）、
  CI（CalamityInheritance，`CalamityInheritance-Beta1.12`）。
- **已全部删除**：CWR（灾厄大修 = CalamityOverhaul）与 CE（灾厄：熵 = CalamityEntropy）来源的内容。
  删除依据：物品在本机灾厄源码里是否有**同名类**——没有的删（含其专属弹幕/粒子/减益/贴图/音效/文案），
  有的保留（"灾厄已有、但经大修重制"的版本）。
- **保留的 CWR 重制件**（灾厄本体有同名物）：`Excelsus`、`Terratomere`、`DragonRage`、`AnarchyBlade`、
  `CometQuasher`、`EntropicClaymore`、`GreatswordofJudgement`、`StellarStriker`，以及魔影套的职业头盔变体
  （`DemonshadeHelmMagic/Ranged/Summon`）。
- 若日后要恢复被删内容，用 git 历史（删除提交 `19ad483` / `f8c48ab`；`git show <sha>^:<path>` 可取回）。
  **不要把 CE/CWR 内容再引进来**。

## 2. 本机参考源码（侦察用）

`D:\Game\Terraria\ModModel\` 下：`CalamityModPublic-2.0.4`（缩放后内有同名子目录）、`CalamityModPublic-2.0.3.9`、
`CalamityModPublic-1.4.4-release`、`CalamityInheritance-Beta1.12`、`CalamityOverhaul-*`、
`CalamityEntropy-master`、`InnoVault-main`。灾厄中文名对照可查
`C:\Users\28155\Documents\My Games\Terraria\tModLoader\ModLocalization\CalamityMod\Localization\zh-Hans\`。

## 3. 已确立的换算/口径

| 中文名 | 实际内容 | 备注 |
|---|---|---|
| 猎魂鲨牙 | 灾厄 `ReaperTooth` | 与 `Ystralyn.cs` 的既有写法一致 |
| 魔影锭 | 灾厄 `ShadowspecBar` | |
| 嘉登熔炉 | `DraedonsForge`（两版都有） | 经典版常用站台 |
| 宇宙砧 | `CosmicAnvil`（现代版） | |
| 月后稀有度 | `Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = N` | 12~22 档；12 青绿 / 13 荧光绿 / 14 蓝(=CI DeepBlue) / 15 紫(=CE AbyssalBlue) / **16 品红(=灾厄 HotPink，即"魔影档")** / 17 传奇 / 18 闪烁紫 / 19 纯蓝 / 20 彩虹 / 21 暗红 / 22 橙；基础稀有度统一填 `ItemRarityID.Red` |
| 价值 | 照源写；同档参考：16 档的 `Nemesis` = 3 铂金 20 金 | |
| 双版本配方 | `ModLoader.TryGetMod("CalamityMod"/"CalamityModClassicPreTrailer")` 各注册一条 | 材料/站台按各自版本取 |
| 减益 | `CalamityDemutationPlayer.ApplyCalamityBuff`（指定版本）/ `ApplyCalamityBuffWithFallback`（双版本自动 + 原版兜底） | |

## 4. 代码与资源约定

- **本地化**：每个语言一份扁平 hjson（`Localization/en-US_Mods.CalamityDemutation.hjson`、`zh-Hans_...`），
  顶层 `Buffs` / `Items` / `Projectiles` / `Systems` / `TextContent`；物品/弹幕默认分类即 "Items"/"Projectiles"，
  新增条目请按字母序插入或追加在段末。改完记得把文件行尾统一成 CRLF（仓库工作区是 CRLF，apply_patch 会写成 LF）。
- **音效**：全部登记在 `Sounds/CalamityDemutationSounds.cs` 的 `SoundStyle` 字段，音频放 `Sounds/Item/<CE 原名>.<ogg|wav|mp3>`；
  音高按"CE 值 − 1"的既有口径换算。
- **粒子**：本工程自研 `Content/Particles`（基类 `BaseParticle`，注册与驱动在 `Content/Particles/Core/DRKLoader.cs`，
  `DRKLoader.NewParticle(particle, pos, vel, color, scale)` + 粒子自己的 `Configure(...)`）。
- **弹幕基类**：手持类武器走 `Content/Projectiles/BaseProjectiles/BaseHeldProjCO`、CWR 系走
  `Content/Projectiles/Melee/Core/BaseSwingCO`（+`SwingSystem`）。
- **贴图**：物品/弹幕贴图同目录同名；复用别人贴图时用显式 `Texture => "CalamityDemutation/..."`；
  通用素材放 `Assets/ExtraTextures/`。
- **着色器（大坑）**：`.fx` **不会**被 tModLoader 构建时编译。新增 `.fx` 后必须手动预编译出同名 `.fxc`：
  `FXC\fxc.exe /nologo /T fx_2_0 /Fo 名字.fxc 名字.fx`（会刷一条 X4717 警告，正常），
  再在 `Common/Effects/EffectLoader.cs` 里登记句柄。漏了这步 → 运行期抛 `MissingResourceException`，
  **整个模组会被 tModLoader 禁用**（不是"贴图不显示"）。同理，删 `.fx` 时记得连 `.fxc` 一起删。
- **单文件装多类**：CE/CWR 来源的武器常把 物品 + 手持弹幕 + 标记弹幕 写在同一个文件里（CI/本体来源的多为分文件），
  移植时随源风格走。

## 5. 联机（多人）约定

- **武器状态不要放 `ModItem` 字段**：ModItem 是全类型共享单例，联机时两名玩家会互相串招式/段位。
  这类"第几式/第几次/连段"计数一律放 ModPlayer：
  `Players/CalamityDemutationPlayer.SwingState.cs`（现有：无政府之刃、巨龙之怒、星流之刃、元素圣剑、禅心剑）
  或按功能单独开 partial。**不需要额外发包**——这些值只决定出手那一刻弹幕的 `ai` 与伤害，随生成包同步。
- **弹幕 AI 在两端都会跑**：AI 里的 `Projectile.NewProjectile` 必须加 `if (Projectile.owner == Main.myPlayer)`；
  写玩家坐标/速度、拉摄像机（`Main.SetCameraLerp`）也只能在主人端做。
- **鼠标坐标**：所有端都会跑的弹幕读鼠标一律用
  `owner.GetModPlayer<CalamityDemutationPlayer>().GetMouseWorld()`（跨端可见），
  不要直读 `Main.MouseWorld`（除非外面已判 `owner == Main.myPlayer`）。
- **自研冲刺表现**（盾牌冲撞 / 弑神者冲刺）都会广播消息让别的端重放粒子；新增大动作表现建议照抄这套。
- **命中回调不用加判据**（已验证，别再挨个查）：`OnHitNPC` / `OnHitPvp` / `OnHitNPCWithProj` / `OnHitPlayer`
  只会在**主人客户端**跑一次。依据是反编译 IL——原版近战走
  `if (whoAmI == Main.myPlayer) ApplyDamageToNPC(...)`，而 `Projectile.Damage()` 的 NPC 结算段开头就是
  `if (owner != Main.myPlayer) 跳过整段`，结算完再 `NetMessage.SendStrikeNPC` 同步给其他端
  （服务端只按包补伤害，不会重跑玩家侧钩子）。上游灾厄在这些回调里同样不写判据。
- **但 `ModItem.UpdateArmorSet` 与 `ModPlayer.PostUpdateMiscEffects` 是"每名玩家 × 每一端"都跑**
  （`Player.Update → UpdateArmorSets`，且整条链上没有 `whoAmI == Main.myPlayer` 守卫）。这两处生成弹幕
  **必须**加 `player.whoAmI == Main.myPlayer`，否则客户端会替别的玩家生成一份 owner 记成本机玩家的弹幕，
  服务端更会以 `Main.myPlayer = 255` 当 owner。（2026-09-30 已按此修好魔影头盔 ×4 与蓝欧米伽触手。）
- **查"某钩子跑在哪一端"的捷径**：tModLoader 安装目录 `D:\Game\Steam\steamapps\common\tModLoader`，
  `tModLoader.dll` 就是把原版类合并进去的程序集；用同目录
  `Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll` 在 PowerShell 里 `ReadAssembly` 后
  dump 目标方法的 IL（`$m.Body.Instructions`），比翻源码快且是唯一权威。用法示例见本节上两条的推导过程。

## 6. 工作流

- 编译/验证：`dotnet build -v q -nologo`（会调用 `tModLoader.dll -server -build`，把 `.tmod` 写进
  `…\tModLoader\Mods\`；该目录在工作区之外，沙箱内会报 Access denied，需提权运行）。
  期望结果：**0 警告 0 错误**（工程警告基线是 0）。
- 沙箱限制（Codex 会话）：`.git` 在工作区内也是只读，`git add/commit` 会报
  `index.lock: Permission denied`；同时沙箱往环境注入 `HTTP(S)_PROXY=http://127.0.0.1:9`（丢弃端口），
  网络一律不通。**git 的写操作与 `push/pull` 都要提权（非沙箱）执行**，提权后走的是系统代理
  （`127.0.0.1:7897`），不再受那对注入变量影响。
- 提交：仓库未配置 user.name/email，命令行提交需带
  `git -c user.name='LiFeiAbom' -c user.email='LiFeiAbom@users.noreply.github.com' commit …`。
  提交信息风格：`类型: 描述`（移植 / 适配 / 修正 / 平衡 / 删除 / 清理 / 回退），可带要点正文。
- 推送：`git push origin master`（网络偶发 `Connection was reset`，重试即可）。
- **本机 `rg` 必须显式带路径**：`rg -n '关键字' .`（结尾那个 `.` 不能省）。不带路径时它在本机会**静默**
  搜不到任何东西、直接返回空，看起来像"全工程 0 命中"——曾因此误判过好几轮。同理，读无 BOM 的
  UTF-8 源文件要用 `Get-Content -Encoding UTF8`，否则中文注释会花屏。
- 联网受限时的绕道：`raw.githubusercontent.com` 不通，改用 jsDelivr
  （`https://cdn.jsdelivr.net/gh/<owner>/<repo>@<branch>/<path>`）或 GitHub API 均可正常访问。

## 7. 当前状态（截至最后一次会话）

- 上一批工作：① 移植泓渊亡铭（已随后被删）② 全工程联机适配修正 ③ **删除全部 CWR/CE 内容**（三笔提交）
  ④ 注释与文案的残留清理 + 一行死注释 ⑤ 补删 zh-Hans 里泓渊亡铭残留的两条弹幕名
  ⑥ **联机隐患复查**（219 处 `NewProjectile` 全量过筛 + 上游同名文件逐个体检）：
  修掉魔影头盔 ×4 的召唤缺判据、蓝欧米伽触手缺判据（对照上游发现是移植时丢的），
  并修好无政府之刃上一轮被插坏的方法缩进。
- **本地与 `origin/master` 已同步**（远端现为 `fa6e794`）；此前积压的提交已全部推上去，
  包括 `19ad483`/`f8c48ab`（删除第一、二批）、`b501d9a`（残留清理）、`47e7131`（死注释）、
  `d86862f`（AGENTS.md）、`fa6e794`（补删中文本地化两行）。
- 验证状态：被删类名在工程内残留提及 **0**；编译 **0 警告 0 错误**；音效字段与素材路径存在性检查通过；
  联机复查结论：ModItem 可变状态字段 0 处、鼠标读取全部有守卫、摄像机 0 处、命中回调类生成点无需判据
  （理由见第 5 节）。
