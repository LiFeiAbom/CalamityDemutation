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
  **写完任何文件都要顺手做本地化**（用户明确要求）：只要新增/改动的是玩家可见文本——物品、弹幕、增益/减益的
  `DisplayName` 与 `Tooltip`、配置项、界面文案——就必须同步补两个语言文件。配置项的键是
  `Configs.<配置类名>.<选项名>.Label` / `.Tooltip`（例：`Configs.ConfigSystem.StatInflation`）。
  忘了补时 `dotnet build` 会自动追加缺失键，但**只补英文默认值**；zh-Hans 那边追加的是 `//` 注释占位，
  中文必须自己填。补完同样要把行尾整回 CRLF。
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

## 7. 数值膨胀（StatInflation：武器 34 把 + 盔甲 12 件，2026-10-01 全量接入完毕）

用户逐把点名「关态 → 开态」的数值，武器侧统一按下面的模板落地。**新会话若要继续，直接照此模板加即可。**

- **开关**：`ConfigSystem.StatInflation`（ClientSide，默认关），静态入口 `ConfigSystem.StatInflationEnabled`。
  该配置项**没有** `ReloadRequired`，且武器走的是运行时回调，所以游戏内切换即时生效；
  不要写死在 `SetDefaults` 的 `Item.damage` 里。
- **单把武器的落地模板**（工程统一写法，别用别的花样）：

```csharp
/// <summary>数值膨胀后的面板伤害（用户 YYYY-MM-DD 指定：X → Y）。</summary>
private const float InflatedDamage = Yf;
/// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 Item.damage 的源值 X。</summary>
private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
```

- **最容易漏的一环**：凡是**按基础伤害比例派生**的伤害，必须把 `Item.damage` 换成 `BaseDamage`，
  否则会出现「面板涨了、派生弹幕没涨」。已按此处理过的点：禅心剑的真近战爆发（70%）、
  破灭魔王剑的裸面板 ×4 追加伤害、月炎之锋的月炎陨石雨、庇护之刃命中生成的 `DefenseBlast`、
  彗星陨刃的两处陨石、宙宇波能刃命中召唤的 `LaserFountains`、凤凰之刃的两处日耀爆炸 + 四枚治疗火焰。
  走 `Shoot(...)` 的 `damage` 参数派生的弹幕本来就会跟随，不用动。
- **两条已查清的机制**（照第 5 节的 IL 口径从 `tModLoader.dll` 读的，别再重复考古）：
  1. `Player.GetWeaponDamage(Item, bool)`（该 build 里第二个参数实名为 `forTooltip`）内部会调
     `CombinedHooks.ModifyWeaponDamage`，所以像崇高誓约之刃那样用
     `player.GetWeaponDamage(player.HeldItem) * 2` 算出来的追加伤害**天然跟随膨胀**，不必改。
     `Player.ItemCheck_Inner` 也是从这个入口取面板值，召唤物弹幕的 `originalDamage` 由此而来
     （巨龙七星灯召唤物的伤害因此跟随）。
  2. `Projectile.minionSlots` 是**每帧实时汇总**的：`Player.Update` 每帧把 `slotsMinions` 归零，
     各仆从在 `Projectile.Update` 里累加自己的 `minionSlots`（判据 `slotsMinions + minionSlots > maxMinions`）。
     所以在仆从 AI 里每帧改写它就能让开关即时生效，**已召唤的仆从不必重召**（巨龙七星灯 4→2 就是这么落的）。
- **开关不只管伤害**：同一开关也可以门控「数量 / 栏位」这类非伤害项——焚灭天惩的每次洒落火球数 10→15
  （`ProjectilesPerBarrage` 由 `const` 改成运行时属性）、巨龙七星灯的仆从栏位 4→2。
  以后遇到类似点照此办理（同样运行时读配置，别写进 `SetDefaults`）。
- **已完成的膨胀表**（关 = 源值 / 开 = 膨胀值，全部受开关控制）：

| 武器 | 关 | 开 | 备注 |
|---|---|---|---|
| 鸿蒙方舟 | 140 | 680 | |
| 禅心剑 | 710 | 3651 | 真近战爆发 70% 已接 |
| 星流之刃 | 900 | 6700 | 等于 2026-09-26 削弱前的原值 |
| 星河之刃 | 84 | 188 | |
| 破灭魔王剑 | 166 | 520 | 关态 2026-10-01 由 230 改为现代版灾厄源值 166 |
| 月炎之锋 | 480 | 640 | |
| 元素方舟 | 126 | 235 | |
| 环境之刃 | 63 | 70 | 用户先报 160 后更正为 70（160 是真·环境之刃的值） |
| 刃冠誓约剑 | 25 | 36 | |
| 灾变斩剑 | 85 | 98 | |
| 彗星陨刃 | 80 | 160 | |
| 庇护之刃 | 110（进度 ×6.00） | 135（进度 ×9.10） | 恢复 `65ba667` 削弱前的旧值；进度表加第三列存旧增量 |
| 灾厄之刃（Devastation） | 114 | 250 | |
| 巨龙之怒 | 1275 | 6375 | 犽戎档 ×5；备选「888×5=4440」已被用户否决 |
| 远古方舟 | 92 | 194 | 关态取 CI 源码值 |
| 死神擢升 | 1200 | 1350 | |
| 元素圣剑 | 4000 | 10000 | |
| 熵之舞 | 92 | 113 | |
| 崇高誓约之刃 | 150 | 200 | 追加伤害走 `GetWeaponDamage`，天然跟随 |
| 宙宇波能刃 | 220 | 660 | LAP 神吞后 ×3（用户先报 2080，后改为按 LAP）；激光喷泉已接 |
| 制裁大剑 | 40 | 310 | |
| 旧日领主誓约剑 | 31 | 144 | |
| 欧米茄环境之刃 | 150 | 400 | |
| 凤凰之刃 | 95 | 160 | 6 处裸面板派生全部改读 `BaseDamage` |
| 宇宙暗流 | 600 | 1800 | LAP 神吞后 ×3（宇宙锭出自神明吞噬者宝藏袋） |
| 泰拉巨刃 | 185 | 370 | |
| 真·远古方舟 | 60 | 194 | 等于现代版灾厄 2.0.4 的同名值 |
| 真·环境之刃 | 160 | 400 | |
| 银河 | 99 | 425 | |
| 焚灭天惩 | 244 | 388 | 同一开关额外把每次洒落的火球数 10→15 |
| 巨龙七星灯 | 120 | 750 | 同一开关额外把仆从栏位 4→2 |

跳过（用户明确点名，不接入）：混乱之刃 150、霜火之刃 125、禁忌誓约之刃 110。

- **档位倍率参考**（Lilac-Arcane-Pack 的分档膨胀表，可用来核对「掉落源 → 倍率」）：
  月后 1.3× / 亵渎后 2.2× / 三使者后 2.4× / 噬魂幽花后 2.4× / 老公爵后 2.5× /
  神吞后 3× / 犽戎后 5× / 星流巨械后 7× / 至尊灾厄后 8× / 魔影 10×。
  该包 `/D:\Game\Terraria\ModModel\Lilac-Arcane-Pack-master` 的 `LAPGlobalItemModifyDamage.cs`
  还提供了一个「拿目标面板反推倍率」的 `SetCustomMult_Int` 写法，思路可借鉴。
- **进度**：工程内 34 把武器（33 近战 + 1 召唤）已全部过筛——**31 把已接入**、
  **3 把按用户口径跳过**，没有剩下的待接入项。
- **盔甲（同一开关，2026-10-01 接入）**：口径是「按 `310c3cf`（第三轮削弱，2026-09-27）逐件回滚」——
  膨胀开启时恢复那次削弱前的值。单件加成写在各自的 `UpdateEquip` 里；**防御**因为 `Item.defense` 只能在
  `SetDefaults` 里一次性赋值（运行中切配置不会刷新），统一改走 `UpdateEquip` 里的 `player.statDefense += 差值`
  （所以 tooltip 显示的仍是源值）。清单（常态 → 膨胀）：

  | 部件 | 常态 | 膨胀（削弱前旧值） |
  |---|---|---|
  | 奥瑞克特斯拉胸甲 | 生命 +100、通用伤/暴 22% | 生命 +400、通用伤/暴 30%，并补回魔力 +400 |
  | 血炎胸甲 | 生命 +40 | 生命 +100，并补回魔力 +100 |
  | 恶魔之影胸甲 | 防御 50、生命/魔力 +300、反伤 100 | 防御 62、生命/魔力 +1000、反伤 200 |
  | 恶魔之影护腿 | 防御 50 | 防御 57 |
  | 恶魔之影头盔（近战/法师/远程） | 暴击 25% | 暴击 50% |
  | 恶魔之影召唤头 | 召唤伤 70%、哨兵 +1、鞭 30%/30% | 召唤伤 80%、哨兵 +5、鞭 45%/45% |
  | 弑神者胸甲 | 生命 +60 | 生命 +250，并补回魔力 +150 |
  | 席尔瓦胸甲 | 生命 +80 | 生命 +300，并补回魔力 +200 |
  | 龙蒿胸甲 | 生命 +40 | 生命 +150、魔力 +100、生命回复 +6 |
  | 欧米茄蓝胸甲 | 禁止正面生命再生 | **该限制失效**（`OmegaBlueNoLifeRegen` 开头按开关直接返回） |

  「补回」的几条（魔力上限、生命回复）是 2026-09-27 削弱时被**整条删除**的，现在按开关加回来。
  更早的两次改动**没有**回滚（用户未要求）：`52bbe34` 恶魔之影套近战化（近战头删掉召唤/哨兵栏、胸甲去近战攻速）、
  `3f2bcc5` 魔影胸甲魔力上限 500→1000。
- **饰品的例外口径**：星云之核（`NebulousCore`）的星云之星伤害**不参与数值膨胀**——固定 300 基准
  再乘玩家通用伤害加成（`player.GetTotalDamage<GenericDamageClass>().ApplyTo(300)`，原先是固定 1500、
  且完全不吃玩家的伤害加成）；其 tooltip 的免死几率已按实现从 20% 改成 **10%**。
- **待办**：`ConfigSystem.StatInflation` 的 tooltip 文案仍写着「按旧版（灾厄 2.0 之前）口径抬高」，
  与现在的「逐把点名 + 档位倍率」口径不一致，待用户决定是否改。
  另外被 `310c3cf` 同步削过的盔甲 tooltip（中英文）目前仍写削弱后的数，膨胀开启时对不上——
  已问过用户是否加 `TooltipInflated` 按开关切文案，尚未拍板。

## 8. 当前状态（截至最后一次会话）

- 最近一批工作：① **盔甲数值膨胀**（12 件，口径 = 回滚 `310c3cf` 第三轮削弱前的值，清单见第 7 节）；
  ② **星云之核修正**：星云之星伤害由固定 1500 改成 300 基准 × 玩家通用伤害加成（不吃膨胀档），
  tooltip 免死几率 20% → 10%（对齐代码里的 `Main.rand.NextBool(10)`）。
- 再往前一批：① **数值膨胀第二批**：按用户逐把点名（清单第 1~20 项）接入 **17 把**武器（`cb26d18`），
  另 3 把（混乱之刃 / 霜火之刃 / 禁忌誓约之刃）用户明确跳过；至此 34 把武器全部过筛、
  31 把已接入（模板与清单见第 7 节）。本批还落地了两类「非伤害联动」：焚灭天惩火球数 10→15、
  巨龙七星灯仆从栏位 4→2。
  ② 顺带用 IL 查清两条机制并写进第 7 节：`GetWeaponDamage` 会跑 `ModifyWeaponDamage`
  （`GetWeaponDamage(HeldItem)` 型派生伤害天然跟随）、`Projectile.minionSlots` 每帧实时汇总。
- 更早一批：① 庇护之刃右键巨刃 / 元素圣剑右键真近战 / 死神擢升左键引导挥砍（`aa117b4`）
  ② 数值膨胀第一批 14 把（`a0859eb`）③ 上游差异核对：判定巨龙之怒移植自 CWR **0.4.0.3.5**
  （判定口径 = 手持体文件名 / useTime / 移植时源伤害 / 粒子 API：0.4.0.3.5 用 `DRK_Spark`、
  0.5.0.1.7 已换 InnoVault 的 `PRT_Spark`），并列出它与源的差异（见下「悬案」）。
- 更早的批次：① 移植泓渊亡铭（已随后被删）② 全工程联机适配修正 ③ **删除全部 CWR/CE 内容**（三笔提交）
  ④ 注释与文案的残留清理 + 一行死注释 ⑤ 补删 zh-Hans 里泓渊亡铭残留的两条弹幕名
  ⑥ **联机隐患复查**（219 处 `NewProjectile` 全量过筛 + 上游同名文件逐个体检）：
  修掉魔影头盔 ×4 的召唤缺判据、蓝欧米伽触手缺判据（对照上游发现是移植时丢的），
  并修好无政府之刃上一轮被插坏的方法缩进。
- **同步状态**：武器两批（`a0859eb` / `cb26d18`）均已推送到 `origin/master`；
  本笔（盔甲膨胀 + 星云之核修正 + AGENTS.md）紧随其后推送。
- **工作区**：干净。用户自己改的 `TheBurningSky.cs` 稀有度 14→15 与本批膨胀改到同一个文件，
  经用户同意已随本批一起提交。
- **悬案（本次会话查清但都没动）**：
  1. 熵之舞缺 `CanUseItem => player.ownedProjectileCounts[Item.shoot] <= 0` 守卫——0.4.0.3.5 的
     `SetKnifeHeld` 会经全局 `CanUseItem` 补上这条，Terratomere / DragonRage 都有、唯独它漏了；
     它是 28/38 帧，挥砍途中可能再生成一个手持体。用户已明确「不加任何东西」，别再提。
  2. 巨龙之怒与 CWR 0.4.0.3.5 的差异：近战 `localNPCHitCooldown` 15→5；物品侧 `coolWorld` 判定被去掉
     （`newLevel == 6` 的 0.6× 伤害变成无条件生效），而手持体侧又内联了 coolWorld 的一部分
     （少了 `worldName == "HoCha113"`）；价值由 `sellPrice(gold: 75)` 变成 `buyPrice(1, 80, 0, 0)`；
     爆炸弹幕 `FireBall` 没设 `DamageType`（源对 `FuckYou` 会补 `DamageClass.Melee`）；
     突刺（`ai[0] == 3`）的 `DefenseEffectiveness *= 0f` 保留了，但源里那道
     「超级护甲 / 防御 > 999 / DR ≥ 95% / 不可破 DR 就不破防」的豁免被裁掉了。
  3. 灾厄之刃（`Devastation`）的本地化显示名是「毁灭」、代码注释却叫「灾厄之刃」，口径待统一。
- 验证状态：编译 **0 警告 0 错误**；被删 CWR/CE 类名在工程内残留提及 **0**；音效字段与素材路径存在性检查通过；
  联机复查结论：ModItem 可变状态字段 0 处、鼠标读取全部有守卫、摄像机 0 处、命中回调类生成点无需判据
  （理由见第 5 节）。
