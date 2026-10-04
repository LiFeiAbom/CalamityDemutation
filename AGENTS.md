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
- **音效**：全部登记在 `Sounds/CalamityDemutationSounds.cs` 的 `SoundStyle` 字段。目录只有两层：`Sounds/` 下开
  `Item/` 与 `Custom/` 两个平面目录，**里面不再套子目录**——物品/武器自身的挥砍·蓄力·使用音放
  `Sounds/Item/<原名>.<ogg|wav|mp3>`，借用的灾厄 Boss / 特效冲击音（冲刺、爆炸、死亡音）与其他自备音放
  `Sounds/Custom/<原名>`。音高按"CE 值 − 1"的既有口径换算。
- **粒子**：本工程自研 `Content/Particles`（基类 `BaseParticle`，注册与驱动在 `Content/Particles/Core/DRKLoader.cs`，
  `DRKLoader.NewParticle(particle, pos, vel, color, scale)` + 粒子自己的 `Configure(...)`）。
- **弹幕基类**：手持类武器走 `Content/Projectiles/BaseProjectiles/BaseHeldProjCO`、CWR 系走
  `Content/Projectiles/Melee/Core/BaseSwingCO`（+`SwingSystem`）。
- **贴图**：物品/弹幕贴图同目录同名；复用别人贴图时用显式 `Texture => "CalamityDemutation/..."`；
  通用素材放 `Assets/ExtraTextures/`。
- **资源路径（大坑，2026-10-03 踩过）**：`Texture => "CalamityDemutation/..."` 与 `SoundStyle` 的路径
  写错时，tModLoader 在**加载期**就抛 `MissingResourceException` 并**把整个模组禁用**
  （玩家端只看到"该模组加载时发生错误 / 已被禁用"，**不是**"贴图不显示"）。它还会顺手往日志写
  「Marked tModLoader installation files as corrupt in Steam / On Next Launch, User will have
  'Verify Local Files' ran」——那只是 tML 对"加载失败"的标准反应，不用管，下次启动让它校验即可。
  本次实例：女妖之爪的 `BansheeHookBoom` 把共用隐形贴图写成 `Content/Projectiles/Melee/InvisibleProj`，
  实际在 **`Content/Projectiles/InvisibleProj.png`**（工程里八个隐形弹幕都引这一张，共用占位贴图在
  `Projectiles` **根目录**、不在 `Melee/` 子目录）。改对路径即恢复。
  **进游戏前的必做自查**：新增任何资源后，把该文件的（a）所有 `"CalamityDemutation/..."` 字面量、
  （b）类同名隐式贴图（`Content/.../类名.png`）、（c）用 `CalamityDemutationConstant.UI/Masking/ColorBar`
  拼出来的路径，逐个 `Test-Path` 对盘核一遍（别靠脑补目录层级——本次就是凭印象补了 `Melee/` 才翻车）。
  一句话范式：`Test-Path .\Content\Projectiles\Melee\X.png`，X 换成引用里的相对路径（去掉扩展名）。
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
- 资源自查（**编译不会报、只有进游戏才炸**，且会禁用整个模组）：跑现成脚本
  `powershell -ExecutionPolicy Bypass -File .\Tools\CheckResources.ps1`（退出码 0 = 全命中；
  它会核 `"CalamityDemutation/..."` 字面量与 `CalamityDemutationConstant.X + "名字"` 两类引用）。
  脚本覆盖不到的两类再手工看一眼：类同名**隐式**贴图（`Content/.../类名.png`）与
  `Texture + "Glow"` 这类**后缀拼接**。细节见第 4 节「资源路径（大坑）」。
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
     （故召唤杖的面板膨胀会带动召唤物伤害）。
  2. `Projectile.minionSlots` 是**每帧实时汇总**的：`Player.Update` 每帧把 `slotsMinions` 归零，
     各仆从在 `Projectile.Update` 里累加自己的 `minionSlots`（判据 `slotsMinions + minionSlots > maxMinions`）。
     所以在仆从 AI 里每帧改写它就能让开关即时生效，**已召唤的仆从不必重召**。
- **开关不只管伤害**：同一开关也可以门控「数量 / 栏位」这类非伤害项——焚灭天惩的每次洒落火球数 10→15
  （`ProjectilesPerBarrage` 由 `const` 改成运行时属性）。以后遇到类似点照此办理
  （同样运行时读配置，别写进 `SetDefaults`）。
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
| 暴政 | 890 | 2200 | 同一开关额外把每次挥砍的火焰 6→10、单枚火焰伤害 25%→75% |

跳过（用户明确点名，不接入）：混乱之刃 150、霜火之刃 125、禁忌誓约之刃 110。

- **档位倍率参考**（Lilac-Arcane-Pack 的分档膨胀表，可用来核对「掉落源 → 倍率」）：
  月后 1.3× / 亵渎后 2.2× / 三使者后 2.4× / 噬魂幽花后 2.4× / 老公爵后 2.5× /
  神吞后 3× / 犽戎后 5× / 星流巨械后 7× / 至尊灾厄后 8× / 魔影 10×。
  该包 `/D:\Game\Terraria\ModModel\Lilac-Arcane-Pack-master` 的 `LAPGlobalItemModifyDamage.cs`
  还提供了一个「拿目标面板反推倍率」的 `SetCustomMult_Int` 写法，思路可借鉴。
- **进度**：工程内 34 把武器（全为近战）已全部过筛——**31 把已接入**、
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
  另外被 `310c3cf` 同步削过的盔甲 tooltip（中英文）原先仍写削弱后的数，膨胀开启时对不上——
  现已落地按开关切文案的机制（2026-10-02，**目前只接了魔影 6 件**）：装备在 `ModifyTooltips` 里调
  `CDUtil.ApplyInflatedTooltip`（`Utilities/CDUtil_Tooltip.cs`），膨胀开启时用本地化键
  `Items.<内部名>.TooltipInflated` 整段替换原版正文（只替换名字以 `Tooltip` 开头的行，SetBonus 原样保留），
  并按传入的 `defenseBonus` 把原版自动生成的「防御」行数字改成实际生效值
  （膨胀下防御只能在 `UpdateEquip` 补差值，`Item.defense` 运行期改不了，故 tooltip 与实现靠这里对齐）。
  欧米茄蓝只有胸甲的「禁止正面生命再生」随开关变化（膨胀时该限制整体失效），已接；
  其头盔/护腿没有任何随膨胀变化的数字，无需文案。
  其余 5 件盔甲（弑神者/席尔瓦/龙蒿/血炎/金源）尚未照此补文案，待续。

## 8. 当前状态（截至最后一次会话）

- 最近一批工作（2026-10-04）：**古圣金源射手头（AuricTeslaHoodedFacemask，金兜铁面盔）——「合并」下位四套的射手效果**。
  口径与既有近战头（AuricTeslaHelm）对称：**单件与套装效果都照经典版 1:1**。
  ① 新增 `Content/Items/Armors/AuricTesla/AuricTeslaHoodedFacemask.cs`（贴图两张取自经典版 1.4.2.101）：
  18x18（贴图实际 26×26）、价值 1 铂金 80 金、**防御 40**（近战头是 54，经典版原样）、月后稀有度 20；
  单件远程 30/30 + `auricBoost`。**没有**远程攻速（经典与现代此件都没有；射手侧那 +10% 由置位的 `silvaRanged` 提供）。
  ② 套装 = **把下位四套的射手侧合并进来**：置 `tarraSet+tarraRanged`、`bloodflareSet+bloodflareRanged`、
  `godSlayer+godSlayerRanged`、`silvaSet+silvaRanged`、`auricSet`；外加荆棘 +3、`lavaMax+240`、`ignoreWater`、
  `crimsonRegen`，以及泡岩浆时 +30 防 / +10 回血。按经典版原样**不加 aggro、不置 `godSlayerDamage`**
  （那两项只有近战头有）。于是这一颗头同时吃到：龙蒿树叶爆炸＋生命能量分裂、血炎的 Y 键灵魂爆发＋2% 血液爆炸光球、
  弑神的 2.5 秒破片弹＋溢暴击、林海的 +10% 攻速＋无敌期 +40% 增伤。
  ③ 套装文案 = 经典 3 行（射手四套效果 / 金源光球 / 跑速 +10%）＋ 3 行冲刺说明——**沿用近战头的写法**：
  因 `godSlayer` 已置位、弑神冲刺确实可用，按「让 tooltip 成真」补上；用 `[{0}]` 占位接
  `KeybindsSystem.GodslayerDashKeyDisplay`。
  ④ 配方 = 与近战头**逐字同构**，只把下位头换成射手件（龙蒿面甲 + 血炎角盔 + 始源林海角盔 + 弑神者战盔）：
  现代 `AuricBar`×10 + `PsychoticAmulet` @ 宇宙砧（AuricBar 数量取 1.4.4 的 10）；经典 `AuricOre`×60 + 吸热 10 +
  噩梦 10 + 幻影质 8 + 暗黑碎片 6 + 生命锭 5 + 地狱施法者碎片 5 + 灾厄核心 2 + 银河奇点 1 + `PsychoticAmulet` @ 德雷顿熔炉。
  ⑤ 本地化：中英各补 `Items.AuricTeslaHoodedFacemask`（DisplayName / Tooltip / SetBonus）。
  显示名：en `Auric Tesla Hooded Facemask`、zh **金兜铁面盔**（用户 2026-10-04 指定；近战头仍是「古圣金源头盔」）。
  **未抄**近战头里那两段反射读灾厄 `CalamityPlayer.auricSet` 的无效残留（9.2 已判定是死代码）。
  **四版本差异备查**：防 40 经典/现代一致；现代 2.0.4 已**删掉 silvaSet/silvaRanged 与 lavaMax/lavaWet**、
  `ArmorSetShadows` 改用 `armorEffectDrawOutlines`；CI `AuricTeslaHeadRanged` 额外给持远程武器时 +20% 远程攻速与
  `AuricbloodflareRangedSoul`、沿用 CI 自己的旧金源件——工程走经典。
  验证：编译 0 警告 0 错误，资源自检 143 条全命中。
- 最近一批工作（2026-10-04）：**弑神者射手头两处机制向 CI 靠拢**（用户 2026-10-04 指定「只改这条、其余不动」）。
  ① 破片弹：由经典版「每次射击 5% 概率、伤害 ×2.1（金源 ×3.2）」改为 CI 模型——
  每 **2.5 秒**（150 帧）闸门放行一次，射击时若闸门开启即**必定**追加一枚、随即关闸；
  伤害 = `DamageSoftCap(本次射击 ×2, 1500)`、弹速 ×1.25。
  CI 原实现蹭现代灾厄的 `CalamityPlayer.canFireGodSlayerRangedProjectile`（现代版在 `Player.miscCounter % 150 == 0` 复位）；
  工程不写死灾厄类型，改在 `CalamityDemutationPlayer` 自建跨帧计时器 `godSlayerShrapnelCooldown`
  （字段 ~407、UpdateDead 复位、PostUpdateMiscEffects 递减），并顺手把既有的 `DamageSoftCap`
  由 `private static` 提为 `internal static` 供 `CalamityDemutationGlobalItem.Shoot` 复用。
  ② 远程暴击「再次暴击」：由经典版 `1/max(15, 100 − 远程暴击率)` 单次骰子改为 CI 的**溢暴击**模型——
  总暴击率（`Player.GetTotalCritChance(DamageClass.Ranged)`）> 100% 时按溢出部分（−100 后）的百分比概率触发，
  否则退化为固定 5%（`Main.rand.NextBool(20)`）；触发后 `modifiers.CritDamage *= 2f`（暴击 2 倍 → 4 倍）。
  **备注**：CI 源码该 5% 分支写成 `hitInfo.Damage *= 4`，叠加在已含暴击（2 倍）的伤害上等于 **8 倍**，
  与 CI 自身 tooltip「造成四倍伤害」矛盾，属笔误；工程取 tooltip 口径（两分支都翻倍到 4 倍），只让触发条件照 CI。
  ③ 本地化：中英各把弑神者战盔套装文本的两行改成 CI 口径（「超过 100% 按溢出部分再次暴击」／
  「未超过时 5% 概率 4 倍」／「每 2.5 秒射出一枚破片弹」）。
  **未动**：保命回血 300（9.3 第 3 条悬案照旧）、配方、稀有度、林海射手头及一切其他内容。
  验证：编译 0 警告 0 错误，资源自检 143 条全命中。
- 最近一批工作（2026-10-04）：**始源林海套装按职业补齐·第四件 = 射手头（SilvaHornedHelm）**。
  口径同前三件：单件照经典版、套装效果走经典版。
  ① 新增 `Content/Items/Armors/Silva/SilvaHornedHelm.cs`（贴图 `SilvaHornedHelm.png` 26×24 /
  `SilvaHornedHelm_Head.png` 40×1120 取自经典版 1.4.2.101）：18x18（经典源码即 18，贴图实际 26×24）、
  价值 90 金、**防御 36**（比近战头的 52 低，经典版原样）、月后稀有度 15；单件远程 13/13。
  显示名：en `Silva Horned Helm`、zh **始源林海角盔**（近战头保持 `Silva Helm` / 始源林海战盔；CI 同名件亦译「始源林海角盔」）。
  ② 玩家侧新增 `silvaRanged`（字段 + 两处复位）；套装里置 `silvaSet` + `silvaRanged`，**不置** `silvaMelee`
  （经典版射手头无近战专属三项，故套装文本相应比近战头少三行）。
  ③ 两条远程向效果：
  (a)「提高所有远程武器射速」→ `PostUpdateMiscEffects`：持远程武器且 `useTime > 3` 时
  `Player.GetAttackSpeed<RangedDamageClass>() += 0.1f`。**口径备注**：经典版把它写成 `UseTimeMultiplier`
  返回 `1.1/1.2`——tML 里该值 >1 表示「更慢」（现代灾厄同类写法一律 <1 表示更快），与 tooltip 相反、属写反；
  CI 还原件 `SilvaHeadRanged.UpdateArmorSet` 按 +10% 远程攻速实现，本工程从 CI（让 tooltip 成真）。
  (b)「始源林海无敌期间远程武器伤害 +40%」→ `ModifyHitNPCWithProj`：
  `silvaRanged && silvaCountdown > 0 && hasSilvaEffect && proj 属远程` 时 `damageMult += 0.4`（判据与数值同经典版）。
  ④ 本地化：中英各补 `Items.SilvaHornedHelm`（DisplayName / Tooltip / SetBonus，套装文本 = 通用 7 行 + 远程 2 行，无占位符）。
  ⑤ 配方与既有近战头**逐字一致**（现代 `PlantyMush`×30 + `EffulgentFeather`×8 + `AscendantSpiritEssence`×2 @ 宇宙砧；
  经典 `DarksunFragment`×5 + `EffulgentFeather`×5 + `CosmiliteBar`×5 + `Tenebris`×6 + `NightmareFuel`×14 +
  `EndothermicEnergy`×14 @ 德雷顿熔炉；两条都另需本模组的 `LeadCore`）。
  **四版本差异备查**：防 36 经典版与 CI（`SilvaHeadRanged`）一致；**现代 2.0.x 已无射手头**（目录只剩 Magic/Summon，与 9.2 表一致）；
  CI 配方是 `PlantyMush`×6 + `EffulgentFeather`×5 + `AscendantSpiritEssence`×2 @ 宇宙砧（工程按「与近战头一致」口径，未采纳）；
  CI 的射速 +10% 与无敌期 +40% 伤害（`SilvaRangedSetLegacy`）均与经典同。
  验证：编译 0 警告 0 错误，资源自检 143 条全命中。
- 最近一批工作（2026-10-04）：**弑神者套装按职业补齐·第三件 = 射手头（GodSlayerHelmet）**。
  口径与前两件同：单件照经典版、套装效果走经典版。
  ① 新增 `Content/Items/Armors/GodSlayer/GodSlayerHelmet.cs`（贴图两张取自经典版 1.4.2.101）：
  18x18、价值 75 金、**防御 35**（比近战头的 48 低，经典版原样）、月后稀有度 14；单件远程 14/14；
  配方与既有近战头一致（现代 `CosmiliteBar`×10 + `AscendantSpiritEssence`×2 @ 宇宙砧，= 1.4.4 公开源码值；
  经典 `CosmiliteBar`×14 + `NightmareFuel`×8 + `EndothermicEnergy`×8 @ 德雷顿熔炉）。
  显示名：en `God Slayer Helmet`、zh **弑神者战盔**（近战头保持 `God Slayer Helm` / 弑神者头盔，避免重名）。
  ② 玩家侧新增 `godSlayerRanged`（字段 + 两处复位），套装里只置 `godSlayer` + `godSlayerRanged`——
  **不置** `godSlayerDamage`（≤80 压制）也**不加荆棘**，因为经典版的射手头就没有这两项（其套装文本也少两行）。
  ③ 两条远程向效果：
  (a) 「远程暴击有几率再次暴击、造成 4 倍伤害」→ 写在 `ModifyHitNPCWithProj`：概率同源（`1/max(15, 100-远程暴击率)`），
  命中即 `modifiers.CritDamage *= 2f`（CritDamage 只对暴击生效，故"每次命中掷概率、暴击时才翻倍"与源概率等价）。
  **注意**：经典版把这段写在 `OnHitNPCWithProj` 里改 `hit.Damage`，而那里的 `NPC.HitInfo` 是按值传递、**改不动伤害**——
  属死代码，故本工程按"让 tooltip 成真"的既有口径（见第 9.3 条第 1 项）改写到了真正生效的位置。
  (b) 「发射远程武器时有几率射出弑神者破片弹」→ `CalamityDemutationGlobalItem.Shoot`：5% 概率追加 `GodSlayerShrapnelRound`，
  伤害 = 本次射击 ×2.1（穿金源套 ×3.2）、弹速 ×1.25（口径照经典版；该版的 `!rogue` 判定因本工程无盗贼职业省略）。
  ④ 新增弹幕 `Content/Projectiles/Typeless/GodSlayerShrapnelRound.cs`（8x8、300 帧、紫色尘尾，
  消散时炸开 4~6 枚破片、每枚 = 本弹幕 ×0.3）与 `GodSlayerShrapnel.cs`（6x12、90 帧、前 5 帧平飞后受重力、
  撞地形不消失——源里 `OnTileCollide` 返回 false）。
  ⑤ 本地化：中英各补 `Items.GodSlayerHelmet`（DisplayName / Tooltip / SetBonus，套装文本用 `[{0}]` 接
  `KeybindsSystem.GodslayerDashKeyDisplay`）与 2 条弹幕名。
  **版本差异备查**：防 35 / 远程 14/14 各版一致；配方 1.4.4 是 `CosmiliteBar`×10（工程采用）、2.0 是 ×14、2.0.3.9 是 ×7；
  破片弹两版差得远——经典是「每次射击 5%、伤害 ×2.1（金源 ×3.2）」，现代 2.0.3.9 改成 `canFireGodSlayerRangedProjectile`
  冷却闸门 + 伤害 = `DamageSoftCap(damage, 800)`；「暴击再暴击」现代已整条删除。
  **（2026-10-04 更新：这两条已按用户口径整条改走 CI 的「2.5 秒闸门 + SoftCap(×2,1500)」与「溢暴击」模型，见本节最新一笔。）**
  **CI 对照**（`GodSlayerHeadRangedold`）：防 35、DeepBlue 稀有度、远程 14/14，**套装内**再给远程暴击 +10%，
  配方 `CosmiliteBar`×7 @ 宇宙砧，并带 `GodSlayerSetBonusesChange` 配置开关切换新旧套装逻辑——与本工程"经典 1:1"口径不同。
  验证：编译 0 警告 0 错误，资源自检 143 条全命中。
- 最近一批工作（2026-10-04）：**血炎套装按职业补齐·第二件 = 射手头（BloodflareHornedHelm）**。
  用户口径同射手头那一轮：单件照经典版、不吸收其它版本差异、套装效果走经典版。
  ① 新增 `Content/Items/Armors/Bloodflare/BloodflareHornedHelm.cs`（贴图两张取自经典版 1.4.2.101）：
  18x18、价值 60 金、防御 34、月后稀有度 13；单件 `lavaMax += 240` + `ignoreWater` + 远程 10/10；
  配方与既有近战头**逐字一致**（现代 `Bloodstone`×25 + `BloodOrb`×10 + `RuinousSoul`×2，经典 `BloodstoneCore`×11 + `RuinousSoul`×2，
  均 `TileID.LunarCraftingStation`）。显示名按用户给的名字写作 **血弑魔颅盔 / Bloodflare Demon Helm**
  （经典版原名是 Bloodflare Horned Helm、现代版类名 `BloodflareHeadRanged`，若要改回经典名只需动本地化两行）。
  ② 玩家侧新增 `bloodflareRanged` 与 `bloodflareRangedCooldown`（字段 + 两处复位；冷却在 `PostUpdateMiscEffects` 逐帧递减）
  ，并在既有的 `KeybindsSystem.TarragonHotKey`（默认 Y，与龙蒿近战防御共用，与经典版同键）按键块里加了经典版口径的灵魂爆发：
  30 秒冷却 → 64 颗 + 36 颗环形血尘 → 一次 8 组、左右对称共 **16 枚 `BloodflareSoul`**、每枚固定 **800** 伤害，
  朝向以玩家速度为准（源写法），生成带 `owner == Main.myPlayer` 判据。
  ③ 新增弹幕：`Content/Projectiles/Ranged/BloodflareSoul.cs`（4 帧动画 + 血尘尾；离主人 >600 折返，
  否则 400 曼哈顿距离内索敌、速度 11、惯性 20；消散时判定框撑到 110×110 结算一次范围伤害——
  即 `Projectile.Damage()`）与 `Content/Projectiles/Typeless/BloodBomb.cs` / `BloodBombExplosion.cs`
  （血液爆炸光球：20×20 飞行体命中后原地生成 250×250、60 帧、本地无敌帧 2 帧的爆炸）。
  ④ `CalamityDemutationGlobalItem` 新增全物品 `Shoot` 钩子：血炎射手套装下远程武器 **2%** 概率追加 `BloodBomb`，
  伤害 = 本次射击 ×1.6（穿金源套 ×2.2），口径照经典版（该版的 `!rogue` 判定因本工程无盗贼职业省略）。
  ⑤ 本地化：中英各补 `Items.BloodflareHornedHelm`（DisplayName / Tooltip / SetBonus，套装文本用 `[{0}]` 占位，
  代码里用新加的 `KeybindsSystem.TarragonKeyDisplay` 格式化）与 3 条弹幕名。
  **四版本差异备查**：防 34 / 远程 10/10 各版一致（1.4.4 也是 34）；配方 2.0 与 2.0.3.9 是 `BloodstoneCore`×11 + `RuinousSoul`×2
  （与经典同），1.4.4 换成 `Bloodstone`×25 + `BloodOrb`×10 + `RuinousSoul`×2 且站台是秘银砧——工程沿用近战头既有写法
  （1.4.4 的材料 + 保留月球工作台）；**血液爆炸光球两版差得远**：经典是「每次射击 2%、伤害 ×1.6（金源 ×2.2）」，
  现代 2.0.3.9 改成由 `canFireBloodflareRangedProjectile` 冷却闸门控制、伤害 = `DamageSoftCap(damage × 0.8, 120)`
  （注释里写明是怕狙击枪之类超标）；本工程按用户口径取经典那套。现代版套装激活音 `BloodflareRangerActivation`
  本工程早已有该音效（现被女妖之爪借用）。
  验证：编译 0 警告 0 错误，资源自检 143 条全命中。
- 最近一批工作（2026-10-04）：**龙蒿套装按职业补齐·第一件 = 射手头（TarragonVisage）**。
  用户口径：单件照经典版、**不吸收** 1.4.4 的差异、套装效果**走经典版**。
  ① 新增 `Content/Items/Armors/Tarragon/TarragonVisage.cs`（贴图 `TarragonVisage.png` / `TarragonVisage_Head.png`
  取自经典版灾厄 1.4.2.101）：18x18、价值 50 金、防御 21、月后稀有度 12；单件远程 10/10 + 减伤 5% +
  `lavaMax += 240` + `ignoreWater` + 免疫诅咒地狱/着火/诅咒/冷冻；配方双版本（现代 `UelibloomBar`×12 + `DivineGeode`×6，
  经典 `UeliaceBar`×7 + `DivineGeode`×6，均用 `TileID.LunarCraftingStation`）——与近战头同规矩，现代分支取 1.4.4 的数量。
  ② 玩家侧新增 `tarraRanged` 标记（字段 + `ResetEffects`/`UpdateDead` 复位），落地经典版两条远程套效：
  远程**暴击**命中 → 敌人处炸出 2~3 枚 `ProjectileID.Leaf`（伤害 = 本弹幕 ×0.25），写在
  `CalamityDemutationPlayer.OnHitNPCWithProj`（命中回调只在主人端跑，无需判据）；
  远程**弹幕消失**时 12% 概率分裂 2~3 枚生命能量（伤害 = min(本弹幕 ×0.33, 65)），写在
  `CalamityDemutationGlobalProjectile.OnKill`——**OnKill 每个端都跑，故加了 `owner == Main.myPlayer` 判据**。
  ③ 新弹幕 `Content/Projectiles/Typeless/TarraEnergy.cs`：直线飞行、穿透 1、120 帧、本体全透明
  （引共用隐形贴图 `Content/Projectiles/InvisibleProj.png`，不新增素材）+ 每帧 3 颗 `DustID.TerraBlade` 尘尾。
  ④ 本地化：中英各补 `Items.TarragonVisage`（DisplayName / Tooltip / SetBonus）与 `Projectiles.TarraEnergy.DisplayName`。
  **两条口径备注**：(a) 源里的裸数字已用 Mono.Cecil 反查实名——尘 107 = `DustID.TerraBlade`、弹幕 206 = `ProjectileID.Leaf`；
  (b) 经典版 TarraEnergy 写了 `dust.alpha = Projectile.alpha`（=255），会把本体与尘一起设成全透明、整套特效看不见，
  现代版已删该行——本工程取**可见**的那一版，机械行为仍按经典（直线、不追踪）；要严格复刻只需补回那一行。
  **射手头四版本对照**（其余职业头可照此比对）：经典 `TarragonVisage` 防 21 / 远程 10/10 / 减伤 5% / lavaMax / ignoreWater / 四减益免疫；
  2.0 `TarragonHeadRanged` 与之相同但**无**那四项减益免疫；2.0.3.9 再删 `lavaMax`/`ignoreWater`；
  2.0.4 改成远程 +10%（**无暴击**）+ 减伤 10%；1.4.4 是防 28 / 暴击 +7 / 弹药消耗 -25%；
  **CI 没有龙蒿职业头**（那边只有 `AncientTarragon` 三件套、单头、盗贼/召唤混合体）。
  验证：编译 0 警告 0 错误，资源自检 143 条全命中。
- 最近一批工作（2026-10-04）：**音效目录整理** —— 删掉 `Sounds/Custom/SCalSounds/` 这一层，5 个文件平铺进
  `Sounds/Custom/`：`CatastropheResonanceSlash`（巨龙之怒挥砍）、`DevourerDeath` / `DevourerDeathImpact`
  （弑神者冲刺起手与命中）、`DevourerSegmentBreak1`（阿斯加德之庇护冲刺撞击）、`ProvidenceHolyBlastImpact`
  （极乐之庇护冲刺撞击）。`CalamityDemutationSounds` 里 5 条 `SoundStyle` 路径同步去掉 `SCalSounds/` 段，
  代码其他位置都经由字段引用、无需改动。编译 0 警告 0 错误。
- 最近一批工作（2026-10-04）：**按用户要求删除渎神魂晶（ProfanedSoulCrystal）及其下位渎魂神物（ProfanedSoulArtifact）的全部内容**。
  这两件是同一套召唤向综合饰品（神器 → 水晶），删除共 83 个文件＋一批既有代码，清单：
  ① 物品与变身装备贴图：`Content/Items/Accessories/Comprehensive/` 下的 `ProfanedSoulCrystal` / `ProfanedSoulArtifact`
  （各 +png）与 7 张 `ProfanedSoulTrans{,Night}_Head/Body/Legs/Wings.png`（EquipLoader 注册的昼夜两套变身外观）。
  ② 三守护者与其从属弹幕：`Content/Projectiles/Summon/MiniGuardian{Attack,Defense,Healer,Fireball,FireballSplit,HolyRay,Rock,Spear,Stars,Targeting}`
  （含 Rock1~6、HolyRay 的 Night/Mid/End 等贴图）；转化弹幕 `ProfanedCrystal{MeleeSpear,RangedHuges,RangedSmalls,MageFireball,MageFireballSplit,Whip}`
  与变身动画 `PscTransformAnimation` / `PscTransformRocks`（+Rocks1~6.png）。
  ③ 增益：`Content/Buffs/SummonBuffs/ProfanedCrystalBuff`、`ProfanedCrystalWhipBuff`、`ProfanedCrystalWhipDebuff`、`ProfanedSoulGuardians`（各 +png）。
  ④ 专属冷却与音效/着色器：`Systems/Cooldowns/ProfanedSoulShield.cs`（两个 CooldownHandler：耐久条＋回充条，含 4 张贴图）、
  `Sounds/Custom/ProfanedGuardians/GuardianShieldDeactivate.ogg`、`Sounds/NPCHit/ProvidenceHurt.ogg` 与两个 `SoundStyle` 字段、
  `Effects/RoverDriveShield.fx`＋`.fxc` 与 `EffectLoader.RoverDriveShieldShader`（护罩气泡着色器，只有它用）。
  ⑤ 代码：玩家类删掉 15 段（字段块、ResetEffects/UpdateDead 复位、守护者召唤与治疗、水晶四态属性块、护盾耐久/回充、
  冷却条同步、`FrameEffects`＋`PostUpdate` 变身外观与腿部动画帧（含 `AnimationType`/`OnSolidGround`/`IsValidTransitionFrame`/`HandlePscAnimationFrames`）、
  `FreeDodge` 的护盾分支、`ModifyHurtInfo_ProfanedShield`、`rollBabSpears`、`PostItemCheck` 转化派发，
  以及圣焰施加、召唤跨职业 nerf 豁免、变身死亡文案三处引用），共 −565 行；
  `CalamityDemutationGlobalProjectile` 删掉鞭痕 tag `ModifyHitNPC` 与圣焰分支。
  ⑥ 本地化：中英各删 4 个增益块、2 个物品块、17 条弹幕名与整个 `Systems.UI.Cooldowns`（删后成空段）；`Systems` 段整体移除。
  **保留（不属于本线）**：亵渎之怒药水 `ProfanedRagePotion` 与其增益 `ProfanedRage`；
  通用冷却机架（`Systems/Cooldowns` 的 Cooldown/CooldownInstance/CooldownHandler/CooldownRegistry、
  `Utilities/CDUtil_Cooldown`、`Systems/UI/CooldownRack*`）也保留——它是泛用基础设施，只是目前没有别的使用者。
  验证：编译 0 警告 0 错误，资源自检 142 条全命中（原 169 条）。
- 最近一批工作（2026-10-04）：**按用户要求删除巨龙七星灯（YharonSonStaff）**。连同其全套牵连一并清除：
  物品 `Content/Items/Weapons/Summon/YharonSonStaff.cs`（+png）、召唤物
  `Content/Projectiles/Summon/SonYharon.cs`（+png）、增益 `Content/Buffs/SummonBuffs/SonYharonBuff.cs`（+png）、
  玩家侧 partial `Players/CalamityDemutationPlayer.SonYharon.cs`（`ownSonYharon` 标记，主类两处复位语句同步删）、
  音效 `Sounds/Item/YharonInfernado.ogg` 与其 `SoundStyle` 字段、两版犽戎宝袋的各一条掉落、
  以及中英文本地化条目（`SonYharonBuff` / `YharonSonStaff` / `SonYharon.DisplayName`）。
  该武器原属第 7 节膨胀表 → 表内条目与"35 把武器（34 近战 + 1 召唤）"的统计同步修正为 34 把全近战。
- 最近一批工作（2026-10-03）：① **移植暴政（TheEnforcer）**，源 = 灾厄 **2.0.3.9** 的
  `Items/Weapons/Melee/TheEnforcer.cs`，落地为 `Content/Items/Weapons/Melee/TheEnforcer.cs`
  （+`TheEnforcer.png`/`TheEnforcerGlow.png`）与 `Content/Projectiles/Melee/EssenceFlame2.cs`
  （贴图取自同版本 `Projectiles/Healing/EssenceFlame.png`，源也是复用它）。
  **效果按用户口径改写**：源把喷火挂在 `OnHitNPC`/`OnHitPvp`（要先打中，才在玩家附近随机撒 5 枚）；
  本工程改成**发射逻辑**——每次挥砍 (`Shoot`) 不求命中，直接在**鼠标处**按圆周炸开 6 枚追踪火焰
  （`FlamesPerBarrage`，伤害 = 面板 25%，`SoundID.Item73` 由命中时前移到出火时）。
  其余照源：890 伤害 / 17 帧 / 击退 9 / 缩放 1.5 / 1 铂 40 金（源 `RarityDarkBlueBuyPrice`）/
  月后稀有度 **14**（源 `Rarities/DarkBlue` 的 (43,96,222) 与本工程 14 档一致）/ 世界发光蒙版 /
  `MeleeEffects` 洒 173 号尘。配方两版各一条：现代 宇宙锭×12 @ 宇宙铁砧；经典 宇宙锭×15 @ 嘉登熔炉。
  **数值膨胀已接入**（用户 2026-10-03 点名，三项联动）：面板 890 → **2200**、每次挥砍火焰 6 → **10**、
  单枚火焰伤害 25% → **75%**；tooltip 走 `CDUtil.ApplyInflatedTooltip`，膨胀文案键 `Items.TheEnforcer.TooltipInflated`
  （这是**第一把**用该机制的武器，此前只有 6 件魔影甲在用）。编译 0 警告 0 错误，资源自检 170 条全命中。
- 最近一批工作：① **盔甲数值膨胀**（12 件，口径 = 回滚 `310c3cf` 第三轮削弱前的值，清单见第 7 节）；
  ② **星云之核修正**：星云之星伤害由固定 1500 改成 300 基准 × 玩家通用伤害加成（不吃膨胀档），
  tooltip 免死几率 20% → 10%（对齐代码里的 `Main.rand.NextBool(10)`）。
- 再往前一批：① **数值膨胀第二批**：按用户逐把点名（清单第 1~20 项）接入 **17 把**武器（`cb26d18`），
  另 3 把（混乱之刃 / 霜火之刃 / 禁忌誓约之刃）用户明确跳过；至此 34 把武器全部过筛、
  31 把已接入（模板与清单见第 7 节）。本批还落地了「非伤害联动」：焚灭天惩火球数 10→15。
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

## 9. 古圣金源套（AuricTesla）与其四套下位 —— 对照结论与待办

> 2026-10-01 侦察完成（用户点名「古圣金源套装及其下位」，要求与灾厄本体 / CI 对比，越详细越好）。
> **本次只做了对照，一行代码没改**；下面 9.3 是明天要动手的清单，9.4 是查清后不必重复考古的机制结论。

### 9.1 源流判定（一句话）

工程里的 5 套 12 件（金源 3 + 龙蒿 3 + 血炎 3 + 弑神者 3 + 始源林海 3）是
**`CalamityModClassicPreTrailer`（预发布经典版）的逐行移植**，再叠三层外来物：

1. **近战攻速**取自现代灾厄（经典版近战头都没有攻速）：金源头 28%、血炎面具 18%、龙蒿头 15%、弑神头 20%。
2. **从 CI 借的件**：现代版配方（四件坯料 + AuricBar + CosmicAnvil）、`GodSlayerDMGprotect`
   （≤阈值完全免伤、触发后阈值跌回 20 每帧 +1）、CI 式两段飞镖 `GodSlayerDart`、以及"近战命中每 60 帧放一枚飞镖"。
3. **工程自调**：五职业分列加成合并成 `GenericDamageClass` 并把伤害/暴击抬高（**暴击一律抬到与伤害同值**）＋
   `StatInflation` 回滚（但只覆盖 5 件胸甲）。

本机源的完整路径（侦察用，别再去翻别的版本）：

| 版本 | 路径 |
|---|---|
| 灾厄经典（= 本工程的实际源） | `D:\Game\Terraria\ModModel\CalamityModClassic-cal-1.4.2.101\CalamityModClassic-cal-1.4.2.101\Items\Armor\AuricTesla*.cs`＋同目录 `Tarragon*/Bloodflare*/GodSlayer*/Silva*`；玩家侧逻辑在同级 `CalamityPlayerPreTrailer.cs` |
| 灾厄本体 2.0.4（对照） | `…\CalamityModPublic-2.0.4\CalamityModPublic-2.0.4\Items\Armor\{Auric,Tarragon,Bloodflare,GodSlayer,Silva}\` |
| 灾厄本体 1.4.4 公开源码（对照，攻速/AuricBar 数量取自它） | `…\CalamityModPublic-1.4.4-release\…\Items\Armor\…` |
| CI Beta1.12（对照） | `…\CalamityInheritance-Beta1.12\…\Content\Items\Armor\{AuricTesla,Silva,GodSlayerOld,Ancient*}\`＋`CIPlayer\CalamityInheritancePlayer*.cs` |

移植判据（想复核时照这个比对即可）：经典版的 12 条套装副作用、3 条 AuricOre 长材料配方、
`tarraMelee` 的 25%/`rand(90,180)`、`fBarrier` 的 `rand(200+dmg/2, 301+dmg*2)` 与 500/700/900 三段衰减、
`silvaCountdown=600`＋`silvaHitCounter×100` 扣上限＋400 下限、`auricBoost` 潜行 `0.2f`/`10`、
跑速 `auricSet?0.1 : silvaSet?0.05` —— 工程全部原样搬运。

### 9.2 对照速查表（工程 / 灾厄 2.0.4 / CI / 灾厄经典）

形制类数值（防御、生命、移速、价值、稀有度）**一致**；下表只列有差异的地方。

| 部件 | 工程 | 灾厄 2.0.4 | CI Beta1.12 | 灾厄经典 |
|---|---|---|---|---|
| 金源头 | 防 54；近战 20/20；**攻速 28% 在单件** | 防 54；近战 20/10；攻速 28% 在套装；套装另给 aggro+1200 | 防 54；20/20/28 | 防 54；20/20；**无攻速** |
| 金源胸 | 移速 +25%；通用 **22/22** | **无移速**；通用 8/5 | 移速 +25%；8/5；+GodSlayerDMGprotect | 移速 +25%；8/5 |
| 金源腿 | 通用 **14/14** | 移速仅 +10%；12/5 | 移速 +50%；12/10 | 移速 +50%；12/5 |
| 龙蒿胸/腿 | 通用 10/10（腿半血再 +15% 移速） | `lifeRegen=3`；胸 10/5；腿移速仅 10%、8/8 | `AncientTarragon` 是另一套召唤/盗贼混合体，**与工程无关** | `lifeRegen=2`；胸 10/5；腿 6/6 |
| 血炎面具 | 保留 lavaMax240/ignoreWater；10/10＋攻速 18% | 10/5；攻速在套装；**无 lavaMax/ignoreWater** | CI 直接用灾厄本体那件 | 无攻速 |
| 血炎胸/腿 | 通用 14/14；腿移速 30% | 12/8；腿移速 17%、10/7 | — | 12/8；腿 30%、10/7 |
| 弑神头 | 14/14＋攻速 20% | 14/**7**；攻速在套装 | old：48 防、14/14/20%、aggro+1000 | 14/14；无攻速 |
| 弑神胸 | 反伤 **+0.9**；通用 **15/15** | 反伤 +0.5；**无移速**；11/6 | old：反伤 +0.5、移速 15%、10/6 | 反伤 +0.5、移速 15%、11/6 |
| 弑神腿 | 通用 11/11 | 移速仅 +5%；10/10 | old：移速 35%、10/10 | 移速 35%、10/6 |
| 林海头 | 13/13＋攻速 19% | 2.0.4 **没有近战头**（只有 Magic/Summon） | `SilvaHeadMelee` 13/13 | 13/13；无攻速 |
| 林海胸/腿 | 通用 **18/18**／12/12 | 12/8、**无移速**／移速仅 10%、12/12 | old：移速 20%、12/8／移速 45%、12/7 | 移速 20%、12/8／45%、12/7 |

- 工程**每套只有一个近战头**；灾厄现代与 CI 每套都有 5 个头（近战/远程/法师/召唤/盗贼）。现代版 Auric
  头还改了名（`PlumedHelm/HoodedFacemask/WireHemmedVisage/SpaceHelmet/RoyalHelm`）。
- 金源配方：经典分支 1:1；现代分支＝CI 的坯料清单（四件套＋妄想护符），但 AuricBar 数量取 **1.4.4 公开源码**
  的 10/20/15（CI 与 2.0.4 都是 12/18/15）；且**漏了 CI 现代胸甲要求的霜冻屏障**（工程只放在经典分支）。
- `ArmorSetShadows`：工程/经典＝`armorEffectDrawShadow`；现代与 CI＝`armorEffectDrawOutlines`。
- 金源头里那两段反射读灾厄 `CalamityPlayer.auricSet` 的代码是**无效残留**（读进局部变量就丢，没写回）。

### 9.3 明天要处理的清单（建议按此顺序）

1. **【行为 bug】「5% 完全无效」是空操作，中英语 tooltip 在骗人。**
   位置：`Players/CalamityDemutationPlayer.cs` 的 `ModifyHurt` 里 `if (godSlayerReflect && Main.rand.NextBool(20))`
   → 只写了 `Player.immune/immuneNoBlink`。IL 已证实（见 9.4）这**不会**取消本次伤害，等于白设标记。
   修法：把随机判定挪进 `FreeDodge` 里 `return true`（CI 是置 `freeDodgeFromShieldAbsorption` 再被 FreeDodge 吃掉，等价）。
   涉及 tooltip：`AuricTeslaBodyArmor`（中英各一行"你受到的攻击有 5% 的几率完全无效"）、
   `GodSlayerChestplate` 同款；`GodSlayerChestplate` 的类注释也要跟着改。
   顺带定口径：经典版想要 2%（`rand.Next(50)==0` 且函数体是空的死代码）、CI 是 1/50 真生效，工程现在是 1/20。
2. **【语义】"≤80 伤害削为 1" 的置位从近战头搬到了胸甲。**
   工程由 `GodSlayerChestplate` 置 `godSlayerReflect`，经典版由近战头的 `godSlayerDamage` 置。
   结果：只穿胸甲不戴头也能吃这个减伤，反之不能。要么搬回头，要么确认这是有意为之。
3. **【数值】弑神保命回复 300 ≠ 源的 150。**
   `PreKill` 里 `int heal = draconicSurge ? statLifeMax2 : 300;`（经典版是 150；CI 那行是 `statLife = +100` 的笔误；
   现代灾厄**整套保命机制已删除**，只剩冲刺）。开关关态该不该回到 150，等你拍板。
4. **【数值方向】12 件的通用伤害/暴击全部高于所有源，且召唤/盗贼吃满。**
   根因是五职业分列 → `GenericDamageClass` 合并时选了更强的一档（例：金源胸 8%/5%→22%/22%）。
   需要你先给方向：**"复刻经典版"** 还是 **"以工程现在这套强度为准"**。定了我再逐件过。
   另注：`StatInflation` 目前只覆盖这 5 套的**胸甲**（龙蒿/血炎/弑神/席尔瓦/金源），其余 7 件没有回滚档。
5. **【功能缺口】每套只有近战头，且配方直接引用自己的近战头。**
   CI 用 `RecipeGroup("CalamityInheritance:AnyGodSlayerHeadMelee")` 之类接受任意职业头，工程接受不到 ——
   戴法师/远程/召唤头的玩家无法升阶。要么补 5 头（大工程），要么至少在配方注释里写明。
   **进度（2026-10-04）**：射手头已补齐龙蒿 / 血炎 / 弑神者 / 始源林海 / 金源五套（见第 8 节逐笔；
   金源那颗是「合并下位四套」的 AuricTeslaHoodedFacemask）；只剩欧米茄蓝（OmegaBlue）待补。
   法师头 / 召唤头尚未开始。
6. **【小口径】迁移细节复核项**：林海近战头的现代配方（PlantyMush 30/羽毛 8/精魂 2）在任何源里都没有对应物
   （CI 同名头只要 6/5/2）；现代金源胸甲漏了霜冻屏障；`Devastation` 那类命名口径见 8 节悬案 3。

> 改任何 tooltip 都要同步 `Localization/en-US_…hjson` 与 `zh-Hans_…hjson` 两处，并把行尾整回 CRLF（第 4 节口径）。

### 9.4 已查清的机制结论（别再重复考古）

1. **`Player.Hurt` 里 `ModifyHurt` 只在 `!immune` 时被调用，取消本次伤害的唯一出口是 `FreeDodge`。**
   用 Mono.Cecil dump `tModLoader.dll` 的 `Terraria.Player::Hurt`（11 参数那版）：偏移 79–144 求
   `bool flag = !Player.immune` 并按 `cooldownCounter` 分支，随后 `IL_008f: ldloc.0; brfalse IL_0295` 把
   整个「构造 `HurtModifiers` → `PlayerLoader.ModifyHurt` → 结算」段包在 `if (!immune)` 里；
   全文只有一处 `ldfld Player::immune`（偏移 80），之后再无第二次读取；而偏移 422 有
   `PlayerLoader::FreeDodge`，其后是原版忍者/混乱之脑/暗影闪避与 `ConsumableDodge`。
   ⇒ 在 `ModifyHurt` 里 `Player.immune = true` 只给标记，**伤害照吃**；要真正作废本次命中必须 `return true` from `FreeDodge`。
2. 现代灾厄 2.0.4 **没有** `GodSlayerCooldown`，弑神"保命回复 + 45 秒冷却 + 冷却期 +10% 伤害"整套已删除
   （`rg GodSlayerCooldown` 命中 0；`godSlayerDamage` 只剩 >80 放飞镖与注释掉的 ≤80 判定）；
   林海复活也改成 480 帧重置 + 5 分钟冷却，**没有**经典版的扣最大生命惩罚。
   CI 补回来的是 `GodSlayerDMGprotect` / `GodSlayerReborn` / `AuricSilvaSet`（600、900 两档 + 3 分钟冷却）。
3. 工程的 `PreKill`/`ModifyHurt` 里那一票经典版逻辑（保命、25% 回血、fBarrier 数值、潜行）都能在
   `CalamityModClassic-cal-1.4.2.101\CalamityPlayerPreTrailer.cs` 里逐行对上，
   行号锚点：`tarraMelee` 7813–7820 / `fBarrier` 7830 / 弑神保命 5598–5629 / 林海复活 5631–5661 /
   潜行 4440–4473 / 跑速 5477–5525。
4. 弹幕侧：工程 `Content/Projectiles/Healing/AuricOrb.cs` 是经典版 `Projectiles/Healing/AuricOrb.cs` 的 1:1 搬运
   （只有 `Main.player[Main.myPlayer]`→`Main.LocalPlayer`、`SendData(66)`→`MessageID.SpiritHeal` 两处现代化）；
   现代灾厄**已经没有 AuricOrb**（只有 SilvaOrb，走 `HealingProjectile` 体系）。
   `Content/Projectiles/Melee/GodSlayerDart.cs` 则来自 CI 的 `ArmorProj/GodslayerDart.cs`（两段式），
   不是灾厄本体的 `GodKiller`（一代只有"被打 80+ 触发的单段直飞"）。
