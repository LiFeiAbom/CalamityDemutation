using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Buffs.PositiveBuffs;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Projectiles.Magic;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Content.Projectiles.Ranged;
using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Content.Tiles;
using CalamityDemutation.Enums;
using CalamityDemutation.Sounds;
using CalamityDemutation.Systems.Cooldowns;
using CalamityDemutation.Particles;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tModPorter;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 本模组的玩家数据类（ModPlayer）
    /// 通过布尔字段记录各饰品是否已装备，并实现装备效果的数值结算、
    /// 命中 debuff、PvP 命中 debuff、闪避等逻辑。
    /// 另含 6 个**永久解锁标志**——天界洋葱（extraAccessoryML）/ 拜月契约（extraWingSlot）
    /// 与四件永久增益消耗品（sugarheartCitrus / organicPod / freshBlueberry / moltenMagmaFruit），
    /// 由 SaveData / LoadData 持久化、CopyClientState / SendClientChanges 负责联机同步。
    /// </summary>
    internal partial class CalamityDemutationPlayer : ModPlayer
    {
        // ── 常量 ──
        /// <summary>
        /// 地狱火齐射的总扇形散开角度（单位：度），120 度均分给 FireProjectiles 发弹幕
        /// </summary>
        public const float FireAngleSpread = 120;
        /// <summary>
        /// 单次地狱火齐射的弹幕数量（4 发），与 FireAngleSpread 配合铺开扇形
        /// </summary>
        public const int FireProjectiles = 4;
        // 各属性成长端点：初始值（击败石巨人时）→ 满配值（18 档全清）
        // 归一心元石的成长端点：初始档照搬 1.4.4 版同名饰品，满配档按用户 2026-09-27 点名
        private const float InitDamage = 0.025f, MaxDamage = 0.20f;         // 通用增伤
        private const float InitCrit = 1f, MaxCrit = 10f;                   // 暴击（百分点：1 = +1%）
        private const float InitMeleeSpeed = 0.025f, MaxMeleeSpeed = 0.20f; // 近战攻速
        private const float InitEndurance = 0.0125f, MaxEndurance = 0.10f;  // 伤害减免
        private const float InitMoveSpeed = 0.025f, MaxMoveSpeed = 0.10f;   // 移速
        private const float InitLifePct = 0.02f, MaxLifePct = 0.10f;        // 最大生命（百分比）
        private const int InitLifeRegen = 1, MaxLifeRegen = 3;              // 回血
        private const int InitDefense = 2, MaxDefense = 10;                 // 防御
        private const float InitKnockback = 0.05f, MaxKnockback = 0.2f;     // 召唤击退
        /// <summary>
        /// 【（古）链】无暇粹魂晶对持续伤害减益的抵消量（近似值，理由见 UpdateBadLifeRegen 里的长注释）。
        /// 单列成常量就是为了方便拍板调整——源里各减益的扣量其实不同，4~50 不等
        /// </summary>
        private const int PurityDoTOffset = 24;
        /// <summary>
        /// 自定义网络消息码：客户端上报"洋葱永久解锁状态变更"（见 SendClientChanges），
        /// 由主类 CalamityDemutation.HandlePacket 处理。
        /// </summary>
        public const byte MsgPermanentUnlock = 0;
        /// <summary>
        /// 自定义网络消息码：盾牌冲刺的开始/结束广播（见 CalamityDemutationPlayer.ShieldSlamDash.cs），
        /// 由主类 CalamityDemutation.HandlePacket 处理。载荷 = 玩家索引 + 冲刺种类（None 表示本次冲刺结束）。
        /// </summary>
        public const byte MsgShieldSlamDash = 1;
        /// <summary>
        /// 自定义网络消息码：盾牌冲刺的撞击广播（同上文件），让其他客户端也刷一遍撞击粒子。
        /// 载荷 = 玩家索引 + 冲刺种类 + 被打的敌人索引（short）。
        /// </summary>
        public const byte MsgShieldSlamDashHit = 2;
        /// <summary>
        /// 自定义网络消息码：弑神者冲刺的开始广播（同上文件）。载荷 = 玩家索引（冲刺固定 25 帧，故不需要结束包）。
        /// </summary>
        public const byte MsgGodSlayerDash = 3;
        /// <summary>
        /// 自定义网络消息码：弑神者冲刺的命中广播（同上文件）。载荷 = 玩家索引（命中尘喷在冲刺者身上，与敌人无关）。
        /// </summary>
        public const byte MsgGodSlayerDashHit = 4;
        // ── 静态字段 ──
        /// <summary>
        /// 按"模组名/技能名"缓存 buff type，避免每次命中都 TryGetMod + Find。
        /// Mod.BuffType 找不到返回 0（不抛异常），比 Find&lt;ModBuff&gt; 更抗灾厄版本变化。
        /// </summary>
        private static readonly Dictionary<(string mod, string buff), int> buffTypeCache = new();
        /// <summary>
        /// The Community 进度 Boss 清单（石巨人 → 至尊灾厄，18 档），与庇护之刃 LegendaryBosses 同序。
        /// 仅用于统计"已击败几档"来驱动各属性的线性成长；召唤栏/飞行等一次性加成另行按 Boss 判定。
        /// </summary>
        private static readonly Func<bool>[] CommunityBosses =
        [
            () => NPC.downedGolemBoss,                               // 石巨人 Golem
            () => BossSystem.Plaguebringer,        // 瘟疫使者歌莉娅 Plaguebringer Goliath
            () => BossSystem.Ravager,              // 毁灭魔像（掠夺者）Ravager
            () => NPC.downedAncientCultist,                          // 拜月教邪教徒 Lunatic Cultist
            () => BossSystem.AstrumDeus,           // 星神游龙 Astrum Deus
            () => NPC.downedMoonlord,                                // 月球领主 Moon Lord
            () => BossSystem.Guardians,            // 亵渎守卫 Profaned Guardians
            () => BossSystem.Dragonfolly,          // 丛林龙 Dragonfolly
            () => BossSystem.Providence,           // 亵渎天神 Providence
            () => BossSystem.CeaselessVoid || ClassicSentinelsDowned, // 无尽虚空 Ceaseless Void
            () => BossSystem.StormWeaver || ClassicSentinelsDowned,   // 风暴编织者 Storm Weaver
            () => BossSystem.Signus || ClassicSentinelsDowned,        // 西格纳斯 Signus
            () => BossSystem.Polterghast,          // 噬魂幽花 Polterghast
            () => BossSystem.OldDuke,              // 硫海遗爵（老公爵）Old Duke
            () => BossSystem.DevourerOfGods,       // 噬神者 Devourer of Gods
            () => BossSystem.Yharon,               // 犽戎 Yharon
            () => BossSystem.ExoMechs,             // 星流巨械 Exo Mechs
            () => BossSystem.SupremeCalamitas,     // 至尊灾厄 Supreme Calamitas
        ];
        /// <summary>
        /// The Community 的 Debuff 缩减黑名单缓存：懒加载一次后复用。
        /// 这些 buff 虽被标为 debuff（Main.debuff=true），实为增益/特殊状态，不应被缩短持续时间。
        /// </summary>
        private static HashSet<int> communityDebuffBlacklist;
        /// <summary>
        /// 本模组认作"Boss 战进行中"的额外 NPC 类型（灾厄未置 boss 标记的史莱姆神分身），
        /// 由 AnyBossNPCS 首次调用时按名字软依赖解析并缓存
        /// </summary>
        private static HashSet<int> extraBossTypes;
        // ── 属性 ──
        /// <summary>
        /// 经典版三使者是否全部倒下（经典版无单个使者标记，只有 Sentinel1/2/3）。
        /// </summary>
        private static bool ClassicSentinelsDowned =>
            BossSystem.Sentinel1 && BossSystem.Sentinel2 && BossSystem.Sentinel3;
        // ── 实例字段 ──
        /// <summary>
        /// 【（古）链】已装备甘露安瓿（古）：按「缺失生命比例」给再生 + 置位蜂蜜系三标记（对应灾厄 aAmpoule）。
        /// 与旧件的 <c>ambrosialAmpoule</c> 是不同标记
        /// </summary>
        public bool aAmpoule = false;
        /// <summary>
        /// 已装备阿巴顿（Abaddon）：+8% 通用暴击，暴击命中时炸出硫磺爆炸，并免疫硫磺火减益。
        /// 灾厄原版第三条是"大幅降低硫磺火 DoT 伤害"（30 → 10），本工程按用户口径改成完全免疫。
        /// </summary>
        public bool abaddon = false;
        /// <summary>
        /// 阿巴顿暴击爆炸的冷却（帧）：触发一次置 15，每帧递减（跨帧计时器，只在 UpdateDead 复位）
        /// </summary>
        public int abaddonCritCooldown = 0;
        /// <summary>
        /// 已装备风之石：+10% 移速、+2 跳跃力、+3% 通用增伤，青色照明
        /// </summary>
        public bool aeroStone = false;
        /// <summary>
        /// 苦难（Afflicted）正面增益激活标记：由 Afflicted.cs 每帧置位，
        /// 与 affliction 一同结算通用增伤/防御/减伤/生命上限/生命回复
        /// </summary>
        public bool afflicted = false;
        /// <summary>
        /// 已装备苦难饰品（Affliction）：独享 afflicted 的全部加成，
        /// 并每 10 帧向同队玩家传播 Afflicted 增益
        /// </summary>
        public bool affliction = false;
        /// <summary>
        /// "全体老婆"合集开关：由元素之心等合集饰品置位，
        /// 使所有娘化召唤物同时在场且不被存活检查销毁
        /// </summary>
        public bool allWaifus = false;
        /// <summary>
        /// 【（古）链】永远享有蜂蜜式回血：即使不在蜂蜜里也按原版蜂蜜规则回血（对应灾厄 alwaysHoneyRegen）。
        /// 由 2.0.3.9 口径的蜜露系三件（HoneyDew2 / LivingDew2 / AmbrosialAmpoule2 + 后续的 Purity）置位，
        /// 与旧件的同族标记完全独立、互不影响
        /// </summary>
        public bool alwaysHoneyRegen = false;
        /// <summary>
        /// 已装备聚合大脑：+10% 通用伤害/+5% 暴击，受击时范围困惑敌人并召唤 AuraRain，
        /// 另有 1/8 概率完全闪避伤害
        /// </summary>
        public bool amalgamatedBrain = false;
        /// <summary>
        /// 大杂烩（The Amalgam）"地狱火流星雨"发射倒计时（帧，600 帧 ≈ 10 秒一轮）
        /// </summary>
        public int amalgamFireCountdown = 0;
        /// <summary>
        /// 已装备仙馐药瓶（Ambrosial Ampoule）：减伤/挖掘提速/生命回复，
        /// 免疫冰冻、霜火、毒等 debuff，并附加蜂蜜式回复
        /// </summary>
        public bool ambrosialAmpoule = false;
        /// <summary>
        /// 已装备亚米迪亚斯火花：受伤后向四周迸发多发电火花弹幕（困难模式伤害更高）
        /// </summary>
        public bool amidiasSpark = false;
        /// <summary>
        /// 已装备远古化石：身处地下/洞穴层时挖掘速度 +35%
        /// </summary>
        public bool ancientFossil = false;
        /// <summary>
        /// 已装备远古粉末：身处地下/洞穴/地狱层时获得 +3 防御、+7% 减伤与挖掘提速
        /// </summary>
        public bool archaicPowder = false;
        public bool armorCrumbling = false;
        public bool armorShattering = false;
        public bool asgardianAegis = false;
        public bool asgardsValor = false;
        public bool auricBoost = false;
        public bool auricSet = false;
        /// <summary>
        /// 勇士徽章已装备（近战伤害/暴击/破甲加成，龙蒿套额外攻速）
        /// </summary>
        public bool badgeOfBravery = false;
        /// <summary>
        /// 已装备蜜蜂抗性饰品：被蜜蜂类弹幕命中时伤害减半（蜜蜂弹幕清单见 beeProjectileList）
        /// </summary>
        public bool beeResist = false;
        /// <summary>
        /// 血耀核心已装备（低生命/低防御时获得增伤与减伤）
        /// </summary>
        public bool bloodflareCore = false;
        public int bloodflareFrenzyTimer = 0;
        public int bloodflareFrenzyCooldown = 0;
        public int bloodflareHeartTimer = 180;
        public int bloodflareManaTimer = 180;
        public bool bloodflareMelee = false;
        public int bloodflareMeleeHits = 0;
        /// <summary>
        /// 血炎套装·远程向（BloodflareHornedHelm 的套装标记）：按 [键] 释放波尔特加斯特的迷失灵魂
        /// （见按键块），远程武器射击时有几率追加血液爆炸光球（见 CalamityDemutationGlobalItem.Shoot）
        /// </summary>
        public bool bloodflareRanged = false;
        /// <summary>血炎射手套装灵魂爆发的冷却（帧，1800 = 30 秒；跨帧计时器，只在死亡时复位）</summary>
        public int bloodflareRangedCooldown = 0;
        public bool bloodflareSet = false;
        /// <summary>
        /// 血炎套装·召唤向（BloodflareHelmet 的套装标记）：生命 ≥90% 时 +10% 召唤伤害、
        /// ≤50% 时 +20 防御与 +2 生命再生，并每 900 帧召唤 3 枚环绕自身的 GhostlyMine，
        /// 三条均在 PostUpdateMiscEffects 里结算
        /// </summary>
        public bool bloodflareSummon = false;
        /// <summary>血炎召唤套装召唤地雷的冷却（帧，900 = 15 秒；跨帧计时器，只在死亡时复位）</summary>
        public int bloodflareSummonTimer = 0;
        /// <summary>
        /// 血契已装备（最大生命翻倍，代价是有 25% 概率被暴击）
        /// </summary>
        public bool bloodPact = false;
        /// <summary>
        /// 血蠕虫围巾已装备（近战伤害/攻速 + 伤害减免）
        /// </summary>
        public bool bloodyWormScarf = false;
        /// <summary>
        /// 血蠕虫牙已装备（低血时近战伤害/攻速/减伤额外提升）
        /// </summary>
        public bool bloodyWormTooth = false;
        /// <summary>
        /// 已装备绽放之石（BloomStone）：+2% 通用伤害/+2 暴击，周期性使附近敌人染上固定 debuff
        /// 并造成小额伤害，且脚底会自动生长草/花/染料植物
        /// </summary>
        public bool bloomStone = false;
        public bool bounding = false;
        /// <summary>
        /// 硫磺娘（大胸玫瑰 BigBustyRose）仆从在场标记：由 BrimstoneWaifu 召唤增益每帧置位
        /// </summary>
        public bool brimstoneWaifu = false;
        public bool cadence = false;
        public bool calcium = false;
        /// <summary>
        /// 已装备灾厄之戒：+15% 通用伤害，免疫受击期间概率在玩家附近降下站火弹幕
        /// </summary>
        public bool calamityRing = false;
        public bool ceaselessHunger = false;
        /// <summary>
        /// 已装备混沌石：+50 魔力上限、魔力消耗 ×0.95、+3% 通用伤害，红色照明
        /// </summary>
        public bool chaosStone = false;
        /// <summary>
        /// 云娘（CloudyWaifu）仆从在场标记：由 CloudyWaifu 召唤增益每帧置位，受风暴之眼驱动
        /// </summary>
        public bool cloudWaifu = false;
        /// <summary>
        /// 血神核心已装备（综合增益 + 生命虹吸光环 + 减伤）
        /// </summary>
        public bool coreOfTheBloodGod = false;
        /// <summary>
        /// 已装备腐化烧瓶：身处腐化之地时获得 +3 防御与 +7% 减伤
        /// </summary>
        public bool corruptFlask = false;
        /// <summary>
        /// 已装备爬虫甲壳：+5% 减伤与 +25% 荆棘反伤
        /// </summary>
        public bool crawCarapace = false;
        /// <summary>
        /// 已装备猩红烧瓶：身处猩红之地时获得 +3 防御与 +7% 减伤
        /// </summary>
        public bool crimsonFlask = false;
        /// <summary>
        /// 【（古）链】已装备王冠宝石：基础 +2 再生，带减益时再 +3 并抬 lifeRegenTime（对应灾厄 crownJewel）
        /// </summary>
        public bool crownJewel = false;
        /// <summary>
        /// 已装备寒晶石（CryoStone）：+5% 减伤、+3% 通用增伤，蓝色照明
        /// </summary>
        public bool cryoStone = false;
        /// <summary>
        /// 已装备代达罗斯纹章：+15% 远程伤害/+10% 远程暴击/生命回复/击退、挖掘提速，
        /// 且 1/5 概率不消耗弹药
        /// </summary>
        public bool daedalusEmblem = false;
        /// <summary>
        /// 已装备暗日之戒：+2 召唤栏、+12% 通用增伤与近战攻速、+5% 暴击与挖掘提速；
        /// 白昼额外回血，夜晚额外 +30 防御
        /// </summary>
        public bool darkSunRing = false;
        public bool deificAmulet = false;
        /// <summary>
        /// 当前魔影头部件对应的伤害职业（Melee / Ranged / …），由各职业头盔在 UpdateEquip 中置位。
        /// 供红魔三叉戟决定伤害类型与暴击档位，以及远程件的不消耗弹药判定使用；未穿戴时为 null。
        /// </summary>
        public DamageClass demonshadeClass = null;
        public bool demonshadeSetBonus = false;
        public bool draconicSurge = false;
        public int draconicSurgeCooldown = 0;
        /// <summary>
        /// 德鲁沙之娘（DrewsSandyWaifu）仆从在场标记：由对应召唤增益每帧置位，受瓶中波霸妻子驱动
        /// </summary>
        public bool drewsSandyWaifu = false;
        /// <summary>
        /// 元素手套已装备（近战攻速/伤害/暴击加成，附带自动挥舞、烈火手套等效果）
        /// </summary>
        public bool elementalGauntlet = false;
        /// <summary>
        /// 已装备元素箭袋：+20% 远程伤害/+20% 远程暴击/生命回复/击退/挖掘提速，
        /// 且 40% 概率不消耗弹药
        /// </summary>
        public bool elementalQuiver = false;
        public bool elysianAegis = false;
        public bool elysianAegispower = false;
        public bool elysianGuard = false;
        public bool enraged = false;
        /// <summary>
        /// 已装备虚灵护符：+20% 魔法伤害/+20% 魔法暴击、+150 魔力上限、魔力消耗 ×0.8，
        /// 附带寻宝/药剂/魔力花效果
        /// </summary>
        public bool etherealTalisman = false;
        /// <summary>
        /// 天界洋葱已使用（永久开启一个额外饰品栏）
        /// </summary>
        public bool extraAccessoryML = false;
        /// <summary>糖心柑橘：永久 +4% 近战攻击速度（商人处 1 金，击败血肉之墙后上架）</summary>
        public bool sugarheartCitrus = false;
        /// <summary>有机豆荚：永久 +4% 伤害减免（商人处 1 金，击败血肉之墙后上架）</summary>
        public bool organicPod = false;
        /// <summary>新鲜蓝莓：永久 +4 点护甲穿透（商人处 1 金，击败血肉之墙后上架）</summary>
        public bool freshBlueberry = false;
        /// <summary>熔岩浆果：永久 +4% 伤害 / +4% 暴击率 / +4 点护甲穿透（商人处 4 金，击败月球领主后上架）</summary>
        public bool moltenMagmaFruit = false;
        /// <summary>
        /// 拜月契约已使用（永久开启一个专用翅膀饰品栏）
        /// </summary>
        public bool extraWingSlot = false;
        /// <summary>
        /// 已装备风暴之眼（召唤饰品）：置位后维持云娘仆从存在并允许其存活
        /// </summary>
        public bool eyeoftheStorm = false;
        /// <summary>
        /// 血肉图腾已装备（减半敌人接触伤害，20 秒冷却）
        /// </summary>
        public bool fleshTotem = false;
        /// <summary>
        /// 血肉图腾冷却计时（单位：帧，1 秒 = 60 帧，1200 = 20 秒）
        /// </summary>
        public int fleshTotemCooldown = 0;
        public bool frigidBulwark = false;
        public bool frostBarrier = false;
        /// <summary>
        /// 已装备真菌甲壳：受伤后向四周迸发 8 颗松露孢子弹幕
        /// </summary>
        public bool fungalCarapace = false;
        /// <summary>
        /// 已装备真菌团块（召唤饰品）或其上位大杂烩（The Amalgam），即"召唤源在身"标记。
        /// 每帧由上述饰品的 UpdateAccessory 置位、ResetEffects/UpdateDead 清零，
        /// 兼作真菌团块仆从的存活判据（Projectiles.Summon.FungalClump.AI 未持有时自动消散），
        /// 以及真菌团块/大杂烩饰品间的互斥判定。
        /// 团块"是否在场"不再用第二个 ModPlayer 字段表示，改由召唤增益 Update
        /// 直接读取 ownedProjectileCounts 判定（见 Content.Buffs.SummonBuffs.FungalClump.Update）。
        /// </summary>
        public bool fungalClump = false;
        /// <summary>
        /// 已装备炼狱（Gehenna）：每 10 秒从高空向瞄准方向降下扇形地狱火流星雨
        /// </summary>
        public bool gehenna = false;
        /// <summary>
        /// 炼狱流星雨发射倒计时（帧）：归零时齐射一轮并重置为 600
        /// </summary>
        public int gehennaFireCountdown = 0;
        /// <summary>
        /// 已装备巨龟壳：-15% 移速，受伤时触发龟壳爆发（panic）增益
        /// </summary>
        public bool giantShell = false;
        /// <summary>
        /// 已装备巨型陆龟壳：-10% 移速与 +25% 荆棘反伤
        /// </summary>
        public bool giantTortoiseShell = false;
        public bool godSlayer = false;
        public bool godSlayerCooldown = false;
        public bool godSlayerMelee = false;
        public int godSlayerMeleefireCD = 0;
        /// <summary>
        /// 弑神者射手套装的破片弹闸门（帧，150 = 2.5 秒；跨帧计时器，只在死亡时复位）。
        /// CI/现代灾厄用 <c>CalamityPlayer.canFireGodSlayerRangedProjectile</c>（<c>Player.miscCounter % 150 == 0</c> 复位）
        /// 每 2.5 秒放行一次；本工程按同样节奏自建闸门，射击时若闸门开启则必定追加破片弹并关闸 150 帧。
        /// 见 CalamityDemutationGlobalItem.Shoot。
        /// </summary>
        public int godSlayerShrapnelCooldown = 0;
        /// <summary>
        /// 弑神者套装·远程向（GodSlayerHelmet 的套装标记）：远程暴击有几率再次暴击造成 4 倍伤害
        /// （见 ModifyHitNPCWithProj，2026-10-04 起走 CI 的「溢暴击」模型），
        /// 发射远程武器时每 2.5 秒射出一枚弑神者破片弹（见 CalamityDemutationGlobalItem.Shoot）
        /// </summary>
        public bool godSlayerRanged = false;
        public bool godSlayerReflect = false;
        /// <summary>
        /// 弑神者套装·召唤向（GodSlayerHornedHelm 的套装标记）：命中敌人时召出弑神幻影
        /// （见 CalamityDemutationGlobalProjectile.OnHitNPC），并每帧维护噬神机械蠕虫
        ///（见 <see cref="UpdateGodSlayerMechworm"/>）
        /// </summary>
        public bool godSlayerSummon = false;
        /// <summary>
        /// 弑神幻影的节流预算：每召出一枚按"本次命中伤害的一半"累加，每帧衰减 2.5，归零后才允许再召
        ///（经典版 CalamityPlayerPreTrailer.cs:3806-3809 原样；源不在此处复位，靠衰减自清）
        /// </summary>
        public float godSlayerDmg = 0f;
        /// <summary>噬神机械蠕虫是否在场（由 MechwormHead.AI 与 Mechworm buff 每帧置位，供蠕虫各段续命）</summary>
        public bool mWorm = false;
        /// <summary>
        /// 已穿弑神者套 / 金源套：把单次不超过 80 的基础伤害压到 1，由套装方法置位、<see cref="ModifyHurt"/> 消费。
        /// 对应经典版的同名标记（源里同样写在近战头的 <c>UpdateArmorSet</c> 而非单件 UpdateEquip）。
        /// 与胸甲的 <see cref="godSlayerReflect"/>（概率完全免伤）分工：低伤压制看整套，闪避看胸甲。
        /// </summary>
        public bool godSlayerDamage = false;
        public bool godSlayerDamageProtect = false;
        public int godSlayerDamageProtectMax = 80;
        /// <summary>
        /// 已装备大凝胶：移速/跳跃提升、+20 生命与魔力上限，静止时额外回血回蓝
        /// </summary>
        public bool grandGelatin = false;
        public bool hasSilvaEffect = false;
        /// <summary>
        /// 带冷却回血的剩余帧数（对应源的 <c>EModPlayer.HealingCd</c>）：玩家级、跨弹幕共享，
        /// 每帧在 <see cref="PostUpdateMiscEffects"/> 里递减；<see cref="TryHealMeWithCd"/> 读它。
        /// ⚠️ 跨帧计时器，**不能放进 ResetEffects**（否则冷却永远归零、形同虚设）
        /// </summary>
        public int HealingCd = 0;
        /// <summary>
        /// 已装备元素之心：综合生命/魔力/移速/减伤/通用增伤/暴击增益，
        /// 脚底自动生长草/花，并作为五娘化饰品的合集核心（置 allWaifus）
        /// </summary>
        public bool heartoftheElements = false;
        /// <summary>
        /// 元素之心"隐藏视觉"版标记（数值约为完整版一半）：由 HeartoftheElements.cs 依据配置置位
        /// </summary>
        public bool heartoftheElementshideVisual = false;
        public bool hellfireExplosion = false;
        public bool holyWrath = false;
        /// <summary>
        /// 已装备蜜露：丛林区获得回血/防御/减伤，免疫毒液与中毒，并附加蜂蜜式生命回复
        /// </summary>
        public bool honeyDew = false;
        /// <summary>
        /// 【（古）链】病症/中毒类减益时长减半（对应灾厄 honeyDewHalveDebuffs）；
        /// 后续的 LivingDew2 会追加火系、Purity 会追加整张 debuffList
        /// </summary>
        public bool honeyDewHalveDebuffs = false;
        /// <summary>
        /// 【（古）链】蜂蜜系站桩加速回复：泡在蜂蜜里且静止时按 turbo 档回血（对应灾厄 honeyTurboRegen）
        /// </summary>
        public bool honeyTurboRegen = false;
        /// <summary>
        /// 【（古）链】已装备感染宝石：+2 再生、带减益时 +4 并抬 lifeRegenTime，另给动态防御（对应灾厄 infectedJewel）
        /// </summary>
        public bool infectedJewel = false;
        /// <summary>
        /// 【（古）链】宝石系动态防御存量（对应灾厄 jewelBonusDefense）：
        /// 随身上减益数上涨（感染宝石 16+(N−1)×5、无暇粹魂晶 20+(N−1)×8），减益消失后每 60 帧回落 1 点。
        /// ⚠️ **跨帧累积，绝不能放进 ResetEffects**（放进去会每帧清零、永远涨不起来）
        /// </summary>
        public int jewelBonusDefense = 0;
        /// <summary>
        /// 已装备利维坦龙涎香：免疫溺水，浸水时高额增伤/防御/移速，
        /// 移动时产生毒海水弹幕，并周期性对近身敌人施加毒液 debuff
        /// </summary>
        public bool levianthanAmbergris = false;
        /// <summary>
        /// 已装备生命凝胶：+20 生命上限，静止不动时额外生命回复
        /// </summary>
        public bool lifeJelly = false;
        /// <summary>
        /// 已装备活露：身处丛林时获得生命回复、+5 防御与 +10% 减伤
        /// </summary>
        public bool livingDew = false;
        /// <summary>
        /// 【（古）链】在病症类之外，再把火系/燃烧类减益的时长也减半（对应灾厄 livingDewHalveDebuffs）
        /// </summary>
        public bool livingDewHalveDebuffs = false;
        /// <summary>
        /// 已装备魅惑之饵（召唤饰品）：置位后维持塞壬娘仆从存在并允许其存活
        /// </summary>
        public bool lureofEnthrallment = false;
        /// <summary>
        /// 已装备魔力凝胶：+20 魔力上限，静止不动时额外魔力回复
        /// </summary>
        public bool manaJelly = false;
        public float modStealth = 1f;
        public int modStealthTimer;
        /// <summary>
        /// 星云核心已装备（+20%通用伤害/暴击，20%概率免死并回复100生命）
        /// </summary>
        public bool nebulousCore = false;
        /// <summary>
        /// 星云核心是否"显示外观"（对应饰品的可见性开关，与元素之心的 heartoftheElementshideVisual 同义）：
        /// 仅在装上且未隐藏时为 true。隐藏时不生成星云之星、场上已有的也立刻消散，但 nebulousCore 的属性照常生效
        /// </summary>
        public bool nebulousCoreVisible = false;
        /// <summary>
        /// 烦恼项链已装备（通用伤害+5%，半血以下额外+15%）
        /// </summary>
        public bool necklaceOfVexation = false;
        public bool omegaBlueChestplate = false;
        public bool omegaBlueSet = false;
        public bool omegaBlueHentai = false;
        public int omegaBlueCooldown = 0;
        public bool ornateShield = false;
        public bool photosynthesis = false;
        public bool psychoticAmulet = false;
        public bool profanedRage = false;
        /// <summary>
        /// 【（古）链】已装备无暇粹魂晶：减益免疫/减半、动态防御、再生等一大套（对应灾厄 purity）。
        /// 本标记优先级高于 infectedJewel 与 crownJewel（三者互斥，源的 else-if 链）
        /// </summary>
        public bool purity = false;
        /// <summary>
        /// 【（古）链】无暇粹魂晶「带减益回血节拍惩罚」的累计帧数（对应灾厄 PurityHealSlowdownFrames）：
        /// 带减益越久，直接回血的节拍从 12 帧逐步拉长（上限 180 帧）；减益清空后逐帧回落。
        /// ⚠️ 跨帧累积，**绝不能放进 ResetEffects**；源只在死亡时清零，本工程同（放 UpdateDead）
        /// </summary>
        public int purityHealSlowdownFrames = 0;
        /// <summary>
        /// 已装备辐射软泥：夜间发出暖黄光并提供生命回复
        /// </summary>
        public bool radiantOoze = false;
        public bool rampartofDeities = false;
        public bool redDevil = false;
        public bool redDevil2 = false;
        public bool revivify = false;
        /// <summary>
        /// 【（古）链】已装备光辉软泥（古）：按「缺失生命比例」给再生（对应灾厄 rOoze）。
        /// 注意与旧件的 <c>radiantOoze</c> 是**不同的标记**，两者互不影响
        /// </summary>
        public bool rOoze = false;
        /// <summary>
        /// 已装备玫瑰石：生命回复/上限、+3% 通用增伤与粉色照明（同时驱动玫瑰娘召唤物）
        /// </summary>
        public bool roseStone = false;
        /// <summary>
        /// 玫瑰石是否"显示外观"：仅在装上且未隐藏时为 true。隐藏时不召唤玫瑰娘、场上已有的立刻消散、
        /// 粉色照明也一并关掉，但 roseStone 的属性与互斥判定照常生效
        /// </summary>
        public bool roseStoneVisible = false;
        /// <summary>
        /// 已装备腐坏大脑：75% 血以下 +15% 通用增伤，免疫受击期间概率降下 AuraRain
        /// </summary>
        public bool rottenBrain = false;
        /// <summary>
        /// 沙之娘（SandyWaifu）仆从在场标记：由对应召唤增益每帧置位，受瓶中妻子驱动
        /// </summary>
        public bool sandyWaifu = false;
        /// <summary>
        /// 已装备海贝壳：浸水时获得防御/减伤/移速并可水中呼吸
        /// </summary>
        public bool seaShell = false;
        public bool shadeRegen = false;
        /// <summary>
        /// 身上挂着增强版暗影焰（本工程移植的 <c>Shadowflame</c> 减益）：掉血在 UpdateBadLifeRegen 中结算
        /// </summary>
        public bool shadowflame = false;
        public bool shadowSpeed = false;
        /// <summary>
        /// 龟壳爆发（ShellBoost 正面增益）激活标记：受击后置位，提供 +90% 移速
        /// </summary>
        public bool shellBoost = false;
        public float shieldInvinc = 5f;
        public bool shieldoftheOcean = false;
        /// <summary>
        /// 已装备灾厄符印：+15% 魔法伤害/+10% 魔法暴击、+100 魔力上限、魔力消耗 ×0.85，
        /// 附带寻宝与药剂效果
        /// </summary>
        public bool sigilofCalamitas = false;
        public int silvaCountdown = 600;
        public int silvaHitCounter = 0;
        public bool silvaMelee = false;
        public bool silvaRanged = false;
        public bool silvaSet = false;
        /// <summary>
        /// 塞壬娘（SirenLure）仆从在场标记：由 SirenLure 召唤增益每帧置位，受魅惑之饵驱动
        /// </summary>
        public bool sirenLureWaifu = false;
        public bool soaring = false;
        /// <summary>
        /// 已装备海绵：大量生存属性、静止回复与溺水免疫，受击时回血并迸发电火花与孢子弹幕
        /// </summary>
        public bool sponge = false;
        /// <summary>
        /// 已装备时滞诅咒腰带：召唤增伤/栏位/鞭子范围提升，外加跳跃/闪避/冲刺/自动跳跃
        /// </summary>
        public bool statisBeltOfCurses = false;
        /// <summary>
        /// 已装备时滞祝福：召唤栏 +3、召唤增伤/击退，召唤物命中时施加"时滞悲伤" debuff
        /// </summary>
        public bool statisBlessing = false;
        /// <summary>
        /// 已装备时滞诅咒：召唤栏 +3、召唤增伤/击退、鞭子范围与召唤近战攻速提升，
        /// 召唤物命中时施加"时滞悲伤"与暗影焰 debuff
        /// </summary>
        public bool statisCurse = false;
        /// <summary>
        /// 挥舞索引（BaseSwingCO 挥砍系统使用）
        /// </summary>
        public int SwingIndex;
        public int tarraCooldown = 0;
        public bool tarraDefense = false;
        public int tarraDefenseTime = 600;
        public bool tarraLifeRegen = false;
        public bool tarraMelee = false;
        /// <summary>
        /// 龙蒿套装·远程向（TarragonVisage 的套装标记）：远程暴击命中引发树叶爆炸
        /// （见 OnHitNPCWithProj），远程弹幕消失时概率分裂出生命能量
        /// （见 CalamityDemutationGlobalProjectile.OnKill）
        /// </summary>
        public bool tarraRanged = false;
        public bool tarraSet = false;
        /// <summary>
        /// 龙蒿套装·召唤向（TarragonHornedHelm 的套装标记）：绿色光照、生命光环（每 80 帧结算一次）
        /// 与满血时的 +2 仆从上限 / +10% 召唤伤害，均在 PostUpdateMiscEffects 里结算
        /// </summary>
        public bool tarraSummon = false;
        /// <summary>
        /// 生命光环的跨帧计时器。照现代灾厄的做法：字段持久、模 80 触发，**不随 ResetEffects 复位**
        /// （经典版把计时器写成方法内局部变量、闸门恒真，等于每帧都打，属上游 bug，未照搬）
        /// </summary>
        public int tarraLifeAuraTimer = 0;
        /// <summary>
        /// 已装备吞噬者（The Absorber）：综合生命/魔力/移速/荆棘/减伤/静止回复，
        /// 浸水增益、受击回血并触发龟壳爆发
        /// </summary>
        public bool theAbsorber = false;
        /// <summary>
        /// 灾厄饰品类型缓存（元素手套/核生成及其下位饰品），供"还原灾厄内容削弱"回调扫描饰品栏用。
        /// 首次调用 RevertCalamityContentNerfs 时 TryFind 填充（-1 未初始化，0 表示灾厄未安装或未找到）
        /// </summary>
        private static int eGauntletType = -1;
        private static int nucleogenesisType = -1;
        private static int starTaintedGeneratorType = -1;
        private static int statisCurseType = -1;
        private static int statisBlessingType = -1;
        private static int theFirstShadowflameType = -1;
        private static int starbusterCoreType = -1;
        private static int voltaicJellyType = -1;
        private static int jellyChargedBatteryType = -1;
        /// <summary>
        /// 蜡烛/塑像增益共存的快照：玩家当前持有的灾厄 4 根蜡烛 + 2 座塑像增益的位掩码
        /// （0..3 = Purple/Blue/Pink/YellowCandleBuff，4..5 = Corruption/CrimsonEffigyBuff，与
        /// Content/Tiles/CandleEffigyCoexistence 的类型缓存同序）。灾厄方块右键会 ClearBuff 掉本组同类增益，
        /// 该掩码供方块钩子把被清掉的那几个补回来。每帧在 PostUpdateMiscEffects 末尾刷新：
        /// 写入点永远早于下一次点击，故右键时读到的必然是"点击前"的状态。未启用开关或无灾厄时恒为 0
        /// </summary>
        public int candleEffigyBuffMask = 0;
        /// <summary>
        /// 冷却机架数据：按字符串 ID 索引的全部冷却实例（对应灾厄 CalamityPlayer.cooldowns），
        /// 每帧在 PostUpdateMiscEffects 末尾统一 tick，到期后执行 OnCompleted 并移除
        /// </summary>
        public Dictionary<string, CooldownInstance> cooldowns = new Dictionary<string, CooldownInstance>(16);
        /// <summary>
        /// 已装备大杂烩（The Amalgam）：聚合大脑、灾厄之戒、吞噬者、炼狱、虚空之烬、
        /// 利维坦龙涎香、真菌团块等多个高级饰品的"全都要"终极形态
        /// </summary>
        public bool theAmalgam = false;
        public bool theCommunity = false;
        /// <summary>
        /// The Community 的 Debuff 时间缩减计时器（帧）：每 60 帧（1 秒）触发一轮缩减，
        /// 同时处理治疗冷却（药水病）/魔力病/一般 Debuff。仅在装备期间递增。
        /// </summary>
        private int communityDebuffTickCounter = 0;   // Debuff 缩减统一计时
        /// <summary>
        /// 已装备最初暗影焰（召唤饰品）：召唤物命中敌人时施加 5 秒暗影焰
        /// </summary>
        public bool theFirstShadowflame = false;
        public bool titanScale = false;
        public bool triumph = false;
        /// <summary>
        /// 已装备活力凝胶：+10% 移速与 +1 跳跃力
        /// </summary>
        public bool vitalJelly = false;
        /// <summary>
        /// 虚空之烬流星雨发射倒计时（帧）：归零时齐射一轮并重置为 600
        /// </summary>
        public int voidFireCountdown = 0;
        /// <summary>
        /// 已装备虚空之烬：通用增伤/熔岩免疫，每 10 秒降下地狱火流星雨，
        /// 免疫受击期间概率降下高伤站火
        /// </summary>
        public bool voidofExtinction = false;
        /// <summary>
        /// 已装备瓶中妻子（召唤饰品）：生成沙之娘（SandyWaifu）仆从
        /// </summary>
        public bool wifeinaBottle = false;
        /// <summary>
        /// 已装备瓶中波霸妻子（召唤饰品）：生成德鲁沙之娘（DrewsSandyWaifu）仆从
        /// </summary>
        public bool wifeinaBottlewithBoobs = false;
        /// <summary>
        /// 已装备亚利姆之力：+22% 通用增伤/+10% 暴击/+12% 近战攻速，+10 防御、
        /// 生命回复、减伤、召唤击退与 +50% 移速
        /// </summary>
        public bool yharimPower = false;
        /// <summary>
        /// 亚利姆徽章已装备（近战加成+圣焰debuff）
        /// </summary>
        public bool yharimsInsignia = false;
        // ── 生命周期方法 ──
        /// <summary>
        /// 每帧重置所有饰品开关标记（由各饰品的 UpdateAccessory 重新置位）
        /// </summary>
        public override void ResetEffects()
        {
            aAmpoule = false;
            abaddon = false;
            aeroStone = false;
            afflicted = false;
            affliction = false;
            allWaifus = false;
            alwaysHoneyRegen = false;
            amalgamatedBrain = false;
            ambrosialAmpoule = false;
            amidiasSpark = false;
            ancientFossil = false;
            archaicPowder = false;
            armorCrumbling = false;
            armorShattering = false;
            asgardianAegis = false;
            asgardsValor = false;
            auricBoost = false;
            auricSet = false;
            badgeOfBravery = false;
            beeResist = false;
            bloodflareCore = false;
            bloodflareMelee = false;
            bloodflareRanged = false;
            bloodflareSet = false;
            bloodflareSummon = false;
            bloomStone = false;
            bloodPact = false;
            bloodyWormScarf = false;
            bloodyWormTooth = false;
            bounding = false;
            brimstoneWaifu = false;
            cadence = false;
            calamityRing = false;
            calcium = false;
            ceaselessHunger = false;
            chaosStone = false;
            cloudWaifu = false;
            coreOfTheBloodGod = false;
            corruptFlask = false;
            crawCarapace = false;
            crimsonFlask = false;
            crownJewel = false;
            cryoStone = false;
            daedalusEmblem = false;
            darkSunRing = false;
            deificAmulet = false;
            demonshadeClass = null;
            demonshadeSetBonus = false;
            draconicSurge = false;
            drewsSandyWaifu = false;
            elementalGauntlet = false;
            elementalQuiver = false;
            elysianAegis = false;
            elysianAegispower = false;
            enraged = false;
            etherealTalisman = false;
            eyeoftheStorm = false;
            fleshTotem = false;
            frigidBulwark = false;
            frostBarrier = false;
            fungalCarapace = false;
            fungalClump = false;
            gehenna = false;
            giantShell = false;
            giantTortoiseShell = false;
            godSlayer = false;
            godSlayerCooldown = false;
            godSlayerDamage = false;
            godSlayerDamageProtect = false;
            godSlayerMelee = false;
            godSlayerRanged = false;
            godSlayerReflect = false;
            godSlayerSummon = false;
            mWorm = false;
            grandGelatin = false;
            heartoftheElements = false;
            heartoftheElementshideVisual = false;
            hellfireExplosion = false;
            holyWrath = false;
            honeyDew = false;
            honeyDewHalveDebuffs = false;
            honeyTurboRegen = false;
            infectedJewel = false;
            levianthanAmbergris = false;
            lifeJelly = false;
            livingDew = false;
            livingDewHalveDebuffs = false;
            lureofEnthrallment = false;
            manaJelly = false;
            nebulousCore = false;
            nebulousCoreVisible = false;
            necklaceOfVexation = false;
            omegaBlueChestplate = false;
            omegaBlueSet = false;
            omegaBlueHentai = false;
            ornateShield = false;
            photosynthesis = false;
            psychoticAmulet = false;
            profanedRage = false;
            purity = false;
            radiantOoze = false;
            rampartofDeities = false;
            redDevil = false;
            redDevil2 = false;
            revivify = false;
            rOoze = false;
            roseStone = false;
            roseStoneVisible = false;
            rottenBrain = false;
            sandyWaifu = false;
            seaShell = false;
            shellBoost = false;
            shadeRegen = false;
            shadowflame = false;
            shadowSpeed = false;
            shieldoftheOcean = false;
            shieldSlamDash = ShieldSlamDash.None;
            sigilofCalamitas = false;
            silvaMelee = false;
            silvaRanged = false;
            silvaSet = false;
            sirenLureWaifu = false;
            soaring = false;
            sponge = false;
            statisBlessing = false;
            statisBeltOfCurses = false;
            statisCurse = false;
            tarraLifeRegen = false;
            tarraMelee = false;
            tarraRanged = false;
            tarraSet = false;
            tarraSummon = false;
            theAmalgam = false;
            theAbsorber = false;
            theCommunity = false;
            theFirstShadowflame = false;
            titanScale = false;
            triumph = false;
            vitalJelly = false;
            voidofExtinction = false;
            wifeinaBottle = false;
            wifeinaBottlewithBoobs = false;
            yharimsInsignia = false;
            yharimPower = false;
            UpdateMouseWorldSync();
        }
        /// <summary>
        /// 玩家死亡时清空所有饰品标记与冷却
        /// </summary>
        public override void UpdateDead()
        {
            aAmpoule = false;
            abaddon = false;
            abaddonCritCooldown = 0;
            aeroStone = false;
            afflicted = false;
            affliction = false;
            allWaifus = false;
            alwaysHoneyRegen = false;
            amalgamatedBrain = false;
            ambrosialAmpoule = false;
            amidiasSpark = false;
            ancientFossil = false;
            archaicPowder = false;
            armorCrumbling = false;
            armorShattering = false;
            asgardianAegis = false;
            asgardsValor = false;
            auricBoost = false;
            auricSet = false;
            beeResist = false;
            badgeOfBravery = false;
            bloodflareCore = false;
            bloodflareFrenzyTimer = 0;
            bloodflareFrenzyCooldown = 0;
            bloodflareHeartTimer = 0;
            bloodflareMelee = false;
            bloodflareManaTimer = 0;
            bloodflareMeleeHits = 0;
            bloodflareRanged = false;
            bloodflareRangedCooldown = 0;
            bloodflareSet = false;
            bloodflareSummon = false;
            bloodflareSummonTimer = 0;
            bloomStone = false;
            bloodPact = false;
            bloodyWormScarf = false;
            bloodyWormTooth = false;
            bounding = false;
            brimstoneWaifu = false;
            cadence = false;
            calamityRing = false;
            calcium = false;
            ceaselessHunger = false;
            chaosStone = false;
            cloudWaifu = false;
            coreOfTheBloodGod = false;
            corruptFlask = false;
            crawCarapace = false;
            crimsonFlask = false;
            crownJewel = false;
            cryoStone = false;
            daedalusEmblem = false;
            darkSunRing = false;
            deificAmulet = false;
            demonshadeClass = null;
            demonshadeSetBonus = false;
            draconicSurge = false;
            draconicSurgeCooldown = 0;
            drewsSandyWaifu = false;
            elementalGauntlet = false;
            elementalQuiver = false;
            elysianAegis = false;
            elysianAegispower = false;
            elysianGuard = false;
            etherealTalisman = false;
            eyeoftheStorm = false;
            fleshTotem = false;
            fleshTotemCooldown = 0;
            frigidBulwark = false;
            frostBarrier = false;
            fungalCarapace = false;
            fungalClump = false;
            gehenna = false;
            giantShell = false;
            giantTortoiseShell = false;
            godSlayer = false;
            godSlayerCooldown = false;
            godSlayerDamage = false;
            godSlayerDamageProtect = false;
            godSlayerMelee = false;
            godSlayerRanged = false;
            godSlayerReflect = false;
            godSlayerShrapnelCooldown = 0;
            godSlayerSummon = false;
            godSlayerDmg = 0f;
            mWorm = false;
            grandGelatin = false;
            hasSilvaEffect = false;
            heartoftheElements = false;
            heartoftheElementshideVisual = false;
            hellfireExplosion = false;
            holyWrath = false;
            honeyDew = false;
            honeyDewHalveDebuffs = false;
            honeyTurboRegen = false;
            infectedJewel = false;
            levianthanAmbergris = false;
            lifeJelly = false;
            livingDew = false;
            livingDewHalveDebuffs = false;
            lureofEnthrallment = false;
            manaJelly = false;
            nebulousCore = false;
            nebulousCoreVisible = false;
            necklaceOfVexation = false;
            omegaBlueChestplate = false;
            omegaBlueSet = false;
            omegaBlueCooldown = 0;
            ornateShield = false;
            photosynthesis = false;
            psychoticAmulet = false;
            profanedRage = false;
            purity = false;
            purityHealSlowdownFrames = 0;   // 跨帧计时器：源只在死亡时清零（见字段注释，不能进 ResetEffects）
            radiantOoze = false;
            rampartofDeities = false;
            redDevil = false;
            redDevil2 = false;
            revivify = false;
            rOoze = false;
            roseStone = false;
            roseStoneVisible = false;
            rottenBrain = false;
            sandyWaifu = false;
            seaShell = false;
            shadeRegen = false;
            shadowflame = false;
            shadowSpeed = false;
            shellBoost = false;
            shieldoftheOcean = false;
            shieldSlamDash = ShieldSlamDash.None;
            shieldSlamDashElapsed = 0;
            shieldSlamDashCooldown = 0;
            sigilofCalamitas = false;
            silvaCountdown = 600;
            silvaHitCounter = 0;
            silvaMelee = false;
            silvaRanged = false;
            silvaSet = false;
            sirenLureWaifu = false;
            soaring = false;
            sponge = false;
            statisBlessing = false;
            statisBeltOfCurses = false;
            statisCurse = false;
            tarraCooldown = 0;
            tarraDefense = false;
            tarraDefenseTime = 0;
            tarraLifeRegen = false;
            tarraMelee = false;
            tarraRanged = false;
            tarraSet = false;
            tarraSummon = false;
            theAmalgam = false;
            theAbsorber = false;
            theCommunity = false;
            theFirstShadowflame = false;
            titanScale = false;
            triumph = false;
            vitalJelly = false;
            voidofExtinction = false;
            wifeinaBottle = false;
            wifeinaBottlewithBoobs = false;
            yharimsInsignia = false;
            yharimPower = false;
        }
        /// <summary>
        /// tModLoader 的 PostUpdateRunSpeeds 钩子：每帧在玩家基础跑动速度/加速度计算完成后调用，
        /// 用于对最终值做乘算修正。本模组在此结算移速类加成——恶魔之影护腿（shadowSpeed）+50%、
        /// 女巫套装（silvaSet）+5%、金之特斯拉套装（auricSet）+10%，同时乘算 runAcceleration 与 maxRunSpeed；
        /// 极乐之庇护守护态（elysianGuard）再整体 ×0.85。
        /// 若开启"回退原版削弱"且安装现代版灾厄，再补偿灾厄对暗影护甲与腾飞徽章移动属性的削弱。
        /// </summary>
        public override void PostUpdateRunSpeeds()
        {
            float runAccMult = 1f + (shadowSpeed ? 0.5f : 0f) + (silvaSet ? 0.05f : 0f) + (auricSet ? 0.1f : 0f);
            float runSpeedMult = 1f + (shadowSpeed ? 0.5f : 0f) + (silvaSet ? 0.05f : 0f) + (auricSet ? 0.1f : 0f);
            if (elysianGuard)
            {
                runAccMult *= 0.85f;
                runSpeedMult *= 0.85f;
            }
            Player.runAcceleration *= runAccMult;
            Player.maxRunSpeed *= runSpeedMult;
            // 回退灾厄对原版移动的削弱（近似补偿）
            if (ConfigSystem.Instance?.RevertVanillaNerfs == true && ModLoader.HasMod("CalamityMod"))
            {
                // 暗影护甲：灾厄把移动加成从 1.75/1.15/1.15/1.75 削弱成 1.25/1.05/1.05/1.5，这里补回
                if (Player.shadowArmor && !(Player.hasMagiluminescence && Player.velocity.Y == 0))
                {
                    Player.runAcceleration *= 1.75f / 1.25f;
                    Player.maxRunSpeed *= 1.15f / 1.05f;
                    Player.accRunSpeed *= 1.15f / 1.05f;
                    Player.runSlowdown *= 1.75f / 1.5f;
                }
                // 腾飞徽章（翱翔徽章）：灾厄把 run acceleration 从 1.75 削弱成 1.25
                if (Player.empressBrooch)
                {
                    Player.runAcceleration *= 1.75f / 1.25f;
                    // 同一件饰品另有两处 IL 削弱，一并回退：
                    // ① FixJumpHeightBoosts（BalancingILChanges.cs:76-83）把跳跃提速 1.8f 压成 0.5f → 补回 1.3f
                    // ② RemoveSoaringInsigniaInfiniteWingTime（:32-46）让 empressBrooch 永不被识别 → 无限飞行失效
                    Player.jumpSpeedBoost += 1.3f;
                    Player.wingTime = Player.wingTimeMax;
                }
            }
            // 弑神者冲刺：状态机与位移（移植自灾厄的 PlayerDashEffect 体系，实现在 partial 文件里）
            GodSlayerDashMovement();
            // 盾牌冲刺（阿斯加德之庇护系列）：同一个钩子，状态机同样在 partial 文件里
            ShieldSlamDashMovement();
        }
        /// <summary>
        /// tModLoader 的 PostUpdateMiscEffects 钩子：每帧在装备更新之后调用。
        /// 本模组绝大多数饰品的"被动数值"都在这里集中结算——依据各饰品
        /// UpdateAccessory 置位的布尔标记，逐项叠加伤害/暴击/攻速/减伤/移速/跳跃/
        /// 生命与魔力上限/区域增益/照明，并驱动各类周期性特效（光环、脚底草花生长的
        /// 装饰、受击反制、流星雨齐射等）。
        /// </summary>
        public override void PostUpdateMiscEffects()
        {
            // 血肉图腾冷却倒计时
            if (fleshTotemCooldown > 0)
                fleshTotemCooldown--;
            // 带冷却回血的剩余帧数（全饰品共用的每帧结算中心，见 HealingCd 字段注释）
            if (HealingCd > 0)
                HealingCd--;
            // 龟壳爆发（ShellBoost）：受击后增益期间 +90% 移速。
            // 原结算于 UpdateBadLifeRegen（该钩子仅在负面生命回复期运行，常漏加），
            // 改到本方法（每帧全饰品结算中心）保证增益期全程生效。
            if (shellBoost)
            {
                Player.panic = false;// 禁用掉恐慌的效果
                Player.moveSpeed += 0.9f;
            }
            // 血耀核心：防御不足 100 时 +15% 通用伤害；低血时获得额外减伤与增伤
            if (bloodflareCore)
            {
                // 灵魂虹吸弹幕只在本地客户端生成，多人下避免服务器与各远端重复生成同一光环
                if (Player.whoAmI == Main.myPlayer)
                {
                    int drain = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center.X, Player.Center.Y, 0f, 0f, ProjectileID.SoulDrain, 40, 0f, Main.myPlayer, 0f, 0f);
                    Main.projectile[drain].usesLocalNPCImmunity = true;
                    Main.projectile[drain].localNPCHitCooldown = 5;
                }
                if (Player.statDefense < 100)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.15f;
                }
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.15))
                {
                    Player.endurance += 0.3f;
                    Player.GetDamage<GenericDamageClass>() += 0.2f;
                    Player.GetCritChance<GenericDamageClass>() += 20;
                }
                else if (Player.statLife <= (int)(Player.statLifeMax2 * 0.5))
                {
                    Player.endurance += 0.15f;
                    Player.GetDamage<GenericDamageClass>() += 0.1f;
                    Player.GetCritChance<GenericDamageClass>() += 10;
                }
            }
            // 血契：最大生命值翻倍（每帧结算，引擎会先重置 statLifeMax2，故结果为稳定的 2 倍）
            if (bloodPact)
            {
                Player.statLifeMax2 += Player.statLifeMax2;
            }
            // 勇士徽章：+10%近战伤害、+10%近战暴击率、+5近战穿透
            if (badgeOfBravery)
            {
                Player.GetDamage<MeleeDamageClass>() += 0.1f;
                Player.GetCritChance<MeleeDamageClass>() += 10;
                Player.GetArmorPenetration<MeleeDamageClass>() += 5;
            }
            // 血蠕虫围巾：+10% 近战伤害、+10% 近战攻速、+15% 伤害减免
            if (bloodyWormScarf)
            {
                Player.GetDamage<MeleeDamageClass>() += 0.1f;
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
                Player.endurance += 0.15f;
            }
            // 血蠕虫牙：半血以下效果翻倍（+10% vs +5%）
            if (bloodyWormTooth)
            {
                if (Player.statLife < (int)(Player.statLifeMax2 * 0.5))
                {
                    Player.GetDamage<MeleeDamageClass>() += 0.1f;
                    Player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
                    Player.endurance += 0.1f;
                }
                else
                {
                    Player.GetDamage<MeleeDamageClass>() += 0.05f;
                    Player.GetAttackSpeed<MeleeDamageClass>() += 0.05f;
                    Player.endurance += 0.05f;
                }
            }
            // 元素手套：+20%近战伤害/暴击/攻速，自动挥舞，烈火手套效果，+240熔岩免疫时间
            if (elementalGauntlet)
            {
                Player.GetDamage<MeleeDamageClass>() += 0.2f;
                Player.GetCritChance<MeleeDamageClass>() += 20;
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.2f;
                Player.autoReuseGlove = true;
                Player.meleeScaleGlove = true;
                Player.kbGlove = true;
                Player.longInvince = true;
                Player.magmaStone = true;
                Player.lavaMax += 240;
            }
            // 烦恼项链：+5%通用伤害，半血以下额外+15%
            if (necklaceOfVexation)
            {
                Player.GetDamage<GenericDamageClass>() += 0.1f;
                // 半血判定：statLife 为当前生命，statLifeMax2 为加成后的最大生命
                if (Player.statLife <= 0.5 * Player.statLifeMax2)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.2f;
                }
            }
            // 亚利姆徽章：+14%近战伤害、+14%近战暴击率、+14%近战攻速、烈火手套击退、+240熔岩免疫时间，半血以下额外+10%通用伤害
            if (yharimsInsignia)
            {
                Player.GetDamage<MeleeDamageClass>() += 0.14f;
                Player.GetCritChance<MeleeDamageClass>() += 14;
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.14f;
                Player.longInvince = true;
                Player.kbGlove = true;
                Player.lavaMax += 240;
                // 半血判定：statLife 为当前生命，statLifeMax2 为加成后的最大生命
                if (Player.statLife <= 0.5 * Player.statLifeMax2)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.1f;
                }
            }
            // 血神核心：+10% 最大生命、+12% 暴击率与伤害、+10% 减伤，
            // 防御不足 100 时再 +15% 伤害，并每帧生成一个灵魂虹吸弹幕形成吸血光环
            if (coreOfTheBloodGod)
            {
                Player.statLifeMax2 += Player.statLifeMax2 / 10;
                Player.GetCritChance<GenericDamageClass>() += 12;
                Player.GetDamage<GenericDamageClass>() += 0.12f;
                Player.endurance += 0.1f;
                if (Player.statDefense < 100)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.15f;
                }
                // 灵魂虹吸弹幕只在本地客户端生成，多人下避免服务器与各远端重复生成同一光环
                if (Player.whoAmI == Main.myPlayer)
                {
                    int drain = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center.X, Player.Center.Y, 0f, 0f, ProjectileID.SoulDrain, 40, 0f, Main.myPlayer, 0f, 0f);
                    Main.projectile[drain].usesLocalNPCImmunity = true;
                    Main.projectile[drain].localNPCHitCooldown = 5;
                }
            }
            // 代达罗斯纹章：远程增伤/暴击/回血/击退/挖速（不耗弹见 CanConsumeAmmo）
            if (daedalusEmblem)
            {
                Player.GetDamage<RangedDamageClass>() += 0.15f;
                Player.GetCritChance<RangedDamageClass>() += 10;
                Player.lifeRegen += 2;
                Player.GetKnockback<SummonDamageClass>().Base += 0.5f;
                Player.pickSpeed -= 0.15f;
            }
            // 元素箭袋：远程增伤/暴击/回血/击退/挖速（不耗弹见 CanConsumeAmmo）
            if (elementalQuiver)
            {
                Player.GetDamage<RangedDamageClass>() += 0.2f;
                Player.GetCritChance<RangedDamageClass>() += 20;
                Player.lifeRegen += 2;
                Player.GetKnockback<SummonDamageClass>().Base += 0.5f;
                Player.pickSpeed -= 0.15f;
            }
            // 灾厄符印：魔法增伤/暴击/魔力上限与减耗，附带寻宝/药剂
            if (sigilofCalamitas)
            {
                Player.GetDamage<MagicDamageClass>() += 0.15f;
                Player.GetCritChance<MagicDamageClass>() += 10;
                Player.statManaMax2 += 100;
                Player.manaCost *= 0.85f;
                Player.findTreasure = true;
                Player.pStone = true;
            }
            // 虚灵护符：魔法增伤/暴击/魔力上限与减耗，附带寻宝/药剂/魔力花
            if (etherealTalisman)
            {
                Player.GetDamage<MagicDamageClass>() += 0.2f;
                Player.GetCritChance<MagicDamageClass>() += 20;
                Player.statManaMax2 += 150;
                Player.manaCost *= 0.8f;
                Player.findTreasure = true;
                Player.pStone = true;
                Player.manaFlower = true;
            }
            // 时滞祝福：召唤增伤/击退 + 3 召唤栏
            if (statisBlessing)
            {
                Player.GetKnockback<SummonDamageClass>().Base += 2.5f;
                Player.GetDamage<SummonDamageClass>() += 0.1f;
                Player.maxMinions += 3;
            }
            // 时滞诅咒：召唤增伤/击退 + 3 召唤栏，鞭子范围与召唤近战攻速提升
            if (statisCurse)
            {
                Player.GetKnockback<SummonDamageClass>().Base += 2.5f;
                Player.GetDamage<SummonDamageClass>() += 0.1f;
                Player.maxMinions += 3;
                Player.whipRangeMultiplier += 0.1f;
                Player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.1f;
            }
            // 时滞诅咒腰带：召唤增伤/栏位 + 鞭子范围，外加移动/跳跃/闪避/冲刺
            if (statisBeltOfCurses)
            {
                Player.GetKnockback<SummonDamageClass>().Base += 2.5f;
                Player.GetDamage<SummonDamageClass>() += 0.2f;
                Player.maxMinions += 4;
                Player.whipRangeMultiplier += 0.2f;
                Player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.2f;
                Player.autoJump = true;
                Player.jumpSpeedBoost += 1.2f;
                Player.extraFall += 50;
                Player.blackBelt = true;
                Player.dash = 1;          // 仅视觉字段，本身不授予冲刺
                Player.dashType = 1;      // 1 = 忍者大师装备式冲刺，真正生效的是这一条
                Player.spikedBoots = 2;
            }
            // 暗日之戒：召唤栏/通用增伤/近战攻速/暴击/挖速；白昼回血、夜晚加防
            if (darkSunRing)
            {
                Player.maxMinions += 2;
                Player.GetDamage<GenericDamageClass>() += 0.12f;
                Player.GetKnockback<SummonDamageClass>().Base += 1.2f;
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
                Player.GetCritChance<GenericDamageClass>() += 5;
                Player.pickSpeed -= 0.15f;
                // 白天回血、夜晚加防；日食算"白天"，按 1.3 的口径两项同时生效
                if (Main.dayTime)
                {
                    Player.lifeRegen += 3;
                }
                if (!Main.dayTime || Main.eclipse)
                {
                    Player.statDefense += 30;
                }
            }
            // 星云核心：+20% 通用增伤/暴击（免死回血见 PreKill）
            if (nebulousCore)
            {
                Player.GetDamage<GenericDamageClass>() += 0.2f;
                Player.GetCritChance<GenericDamageClass>() += 20;
            }
            // 亚利姆之力：通用/近战/召唤全面增伤 + 生存属性大礼包
            if (yharimPower)
            {
                Player.GetDamage<GenericDamageClass>() += 0.22f;
                Player.GetCritChance<GenericDamageClass>() += 10;
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
                Player.statDefense += 10;
                Player.lifeRegen += 4;
                Player.endurance += 0.1f;
                Player.GetKnockback<SummonDamageClass>().Base += 1f;
                Player.moveSpeed += 0.5f;
            }
            // 苦难状态/饰品：大额通用增伤 + 防御/减伤/生命上限/生命回复
            if (afflicted || affliction)
            {
                Player.noKnockback = true;
                Player.GetDamage<GenericDamageClass>() += 0.1f;
                Player.statDefense += 30;
                Player.endurance += 0.05f;
                Player.statLifeMax2 += (int)(Player.statLifeMax2 * 0.1);
                Player.lifeRegen += 2;
            }
            // 苦难饰品附加光环：每 10 帧向同队（非本人）玩家刷一次 Afflicted 增益
            if (affliction && Player.miscCounter % 10 == 0)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player teammate = Main.player[i];
                    if (teammate.active && teammate.whoAmI != Player.whoAmI && teammate.team == Player.team && Player.team != 0)
                    {
                        teammate.AddBuff(ModContent.BuffType<Afflicted>(), 20, true);
                    }
                }
            }
            // 玫瑰石：粉色照明 + 生命回复/上限/3% 通用增伤
            if (roseStone)
            {
                // 粉色照明属于表现层，隐藏外观时关掉（下面的属性照给）
                if (roseStoneVisible)
                {
                    Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0.6f, 0f, 0.25f);
                }
                Player.lifeRegen += 2;
                Player.statLifeMax2 += 20;
                Player.GetDamage<GenericDamageClass>() += 0.03f;
            }
            // 风之石：青色照明 + 移速/跳跃提升 + 3% 通用增伤
            if (aeroStone)
            {
                Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0f, 0.425f, 0.425f);
                Player.moveSpeed += 0.1f;
                Player.jumpSpeedBoost += 2.0f;
                Player.GetDamage<GenericDamageClass>() += 0.03f;
            }
            // 寒晶石：蓝色照明 + 减伤 + 3% 通用增伤
            if (cryoStone)
            {
                Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0f, 0.25f, 0.6f);
                Player.endurance += 0.05f;
                Player.GetDamage<GenericDamageClass>() += 0.03f;
            }
            // 混沌石：红色照明 + 魔力上限/减耗 + 3% 通用增伤
            if (chaosStone)
            {
                Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0.85f, 0f, 0f);
                Player.statManaMax2 += 50;
                Player.manaCost *= 0.95f;
                Player.GetDamage<GenericDamageClass>() += 0.03f;
            }
            // 绽放之石：绿色照明 + 少量通用增伤/暴击；光环给周围敌人上 debuff，
            // 站立地面时还会让脚下空砖长出草/花/染料植物（装饰）
            if (bloomStone)
            {
                Player.GetDamage<GenericDamageClass>() += 0.02f;
                Player.GetCritChance<GenericDamageClass>() += 2;
                Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0.25f, 0.4f, 0.2f);
                // 绽放之石光环：每帧 1/10 概率触发，对 150 像素内的敌对 NPC
                // 补上 debuff(186) 并造成小额伤害（num3=10）
                int bloomCounter = 0;
                int num = 186;
                float num2 = 150f;
                bool flag = bloomCounter % 60 == 0;
                int num3 = 10;
                int random = Main.rand.Next(10);
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (random == 0)
                    {
                        for (int l = 0; l < 200; l++)
                        {
                            NPC nPC = Main.npc[l];
                            if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num] && Vector2.Distance(Player.Center, nPC.Center) <= num2)
                            {
                                if (nPC.FindBuffIndex(num) == -1)
                                {
                                    nPC.AddBuff(num, 120, false);
                                }
                                if (flag)
                                {
                                    nPC.StrikeNPC(nPC.CalculateHitInfo(num3, 0));
                                    if (Main.netMode != NetmodeID.SinglePlayer)
                                    {
                                        NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, l, (float)num3, 0f, 0f, 0, 0, 0);
                                    }
                                }
                            }
                        }
                    }
                }
                bloomCounter++;
                if (bloomCounter >= 180)
                {
                }
                // 站立于实心方块之上且脚底为空格时，按下方地面类型随机长出
                // 染料植物/草花（若地表为泥土、普通草、神圣草或丛林草）
                if (Player.whoAmI == Main.myPlayer && Player.velocity.Y == 0f && Player.grappling[0] == -1)
                {
                    int num4 = (int)Player.Center.X / 16;
                    int num5 = (int)(Player.position.Y + (float)Player.height - 1f) / 16;
                    if (!Main.tile[num4, num5].HasTile && Main.tile[num4, num5].LiquidAmount == 0 && Main.tile[num4, num5 + 1] != null && WorldGen.SolidTile(num4, num5 + 1))
                    {
                        Main.tile[num4, num5].TileFrameY = 0;
                        Main.tile[num4, num5].Get<TileWallWireStateData>().Slope = 0;
                        Main.tile[num4, num5].Get<TileWallWireStateData>().IsHalfBlock = false;
                        if (Main.tile[num4, num5 + 1].TileType == TileID.Dirt)
                        {
                            if (Main.rand.NextBool(1000))
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.DyePlants;
                                Main.tile[num4, num5].TileFrameX = (short)(34 * Main.rand.Next(1, 13));
                                while (Main.tile[num4, num5].TileFrameX == 144)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(34 * Main.rand.Next(1, 13));
                                }
                            }
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendTileSquare(-1, num4, num5, 1, TileChangeType.None);
                            }
                        }
                        if (Main.tile[num4, num5 + 1].TileType == TileID.Grass)
                        {
                            if (Main.rand.NextBool(2))
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.Plants;
                                Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(6, 11));
                                while (Main.tile[num4, num5].TileFrameX == 144)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(6, 11));
                                }
                            }
                            else
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.Plants2;
                                Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(6, 21));
                                while (Main.tile[num4, num5].TileFrameX == 144)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(6, 21));
                                }
                            }
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendTileSquare(-1, num4, num5, 1, TileChangeType.None);
                            }
                        }
                        else if (Main.tile[num4, num5 + 1].TileType == TileID.HallowedGrass)
                        {
                            if (Main.rand.NextBool(2))
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.HallowedPlants;
                                Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(4, 7));
                                while (Main.tile[num4, num5].TileFrameX == 90)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(4, 7));
                                }
                            }
                            else
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.HallowedPlants2;
                                Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(2, 8));
                                while (Main.tile[num4, num5].TileFrameX == 90)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(2, 8));
                                }
                            }
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendTileSquare(-1, num4, num5, 1, TileChangeType.None);
                            }
                        }
                        else if (Main.tile[num4, num5 + 1].TileType == TileID.JungleGrass)
                        {
                            Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                            Main.tile[num4, num5].TileType = TileID.JunglePlants2;
                            Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(9, 17));
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendTileSquare(-1, num4, num5, 1, TileChangeType.None);
                            }
                        }
                    }
                }
            }
            // 元素之心：全属性大礼包；站立地面时脚下长出草/花，并驱动五个娘化召唤物
            if (heartoftheElements)
            {
                // 用户 2026-09-27 点名的修正值：不随元素开关分档，两档都给
                Player.lifeRegen += 4;                            // 2 HP/s
                Player.manaRegenBonus += 2;
                Player.manaCost *= 0.95f;
                Player.GetCritChance<GenericDamageClass>() += 10f;
                // 其余数值按经典版 cal-1.4.2.101 逐项对齐：元素开启（可见）档较低，关闭（隐藏）档略高
                if (!heartoftheElementshideVisual)
                {
                    Player.statLifeMax2 += 20;
                    Player.statManaMax2 += 50;
                    Player.moveSpeed += 0.1f;
                    Player.jumpSpeedBoost += 2.0f;
                    Player.endurance += 0.05f;
                    Player.GetDamage<GenericDamageClass>() += 0.1f;
                }
                else
                {
                    Player.statLifeMax2 += 25;
                    Player.statManaMax2 += 60;
                    Player.moveSpeed += 0.12f;
                    Player.jumpSpeedBoost += 2.2f;
                    Player.endurance += 0.06f;
                    Player.GetDamage<GenericDamageClass>() += 0.12f;
                }
                int bloomCounter = 0;
                int num = 186;
                float num2 = 150f;
                bool flag = bloomCounter % 60 == 0;
                int num3 = 10;
                int random = Main.rand.Next(10);
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (random == 0)
                    {
                        for (int l = 0; l < 200; l++)
                        {
                            NPC nPC = Main.npc[l];
                            if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num] && Vector2.Distance(Player.Center, nPC.Center) <= num2)
                            {
                                if (nPC.FindBuffIndex(num) == -1)
                                {
                                    nPC.AddBuff(num, 120, false);
                                }
                                if (flag)
                                {
                                    nPC.StrikeNPC(nPC.CalculateHitInfo(num3, 0));
                                    if (Main.netMode != NetmodeID.SinglePlayer)
                                    {
                                        NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, l, (float)num3, 0f, 0f, 0, 0, 0);
                                    }
                                }
                            }
                        }
                    }
                }
                bloomCounter++;
                if (bloomCounter >= 180)
                {
                }
                // 站立于实心方块之上且脚底为空格时，按下方地面类型随机长出
                // 染料植物/草花（若地表为泥土、普通草、神圣草或丛林草）
                if (Player.whoAmI == Main.myPlayer && Player.velocity.Y == 0f && Player.grappling[0] == -1)
                {
                    int num4 = (int)Player.Center.X / 16;
                    int num5 = (int)(Player.position.Y + (float)Player.height - 1f) / 16;
                    if (!Main.tile[num4, num5].HasTile && Main.tile[num4, num5].LiquidAmount == 0 && Main.tile[num4, num5 + 1] != null && WorldGen.SolidTile(num4, num5 + 1))
                    {
                        Main.tile[num4, num5].TileFrameY = 0;
                        Main.tile[num4, num5].Get<TileWallWireStateData>().Slope = 0;
                        Main.tile[num4, num5].Get<TileWallWireStateData>().IsHalfBlock = false;
                        if (Main.tile[num4, num5 + 1].TileType == TileID.Dirt)
                        {
                            if (Main.rand.NextBool(1000))
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.DyePlants;
                                Main.tile[num4, num5].TileFrameX = (short)(34 * Main.rand.Next(1, 13));
                                while (Main.tile[num4, num5].TileFrameX == 144)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(34 * Main.rand.Next(1, 13));
                                }
                            }
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendTileSquare(-1, num4, num5, 1, TileChangeType.None);
                            }
                        }
                        if (Main.tile[num4, num5 + 1].TileType == TileID.Grass)
                        {
                            if (Main.rand.NextBool(2))
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.Plants;
                                Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(6, 11));
                                while (Main.tile[num4, num5].TileFrameX == 144)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(6, 11));
                                }
                            }
                            else
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.Plants2;
                                Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(6, 21));
                                while (Main.tile[num4, num5].TileFrameX == 144)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(6, 21));
                                }
                            }
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendTileSquare(-1, num4, num5, 1, TileChangeType.None);
                            }
                        }
                        else if (Main.tile[num4, num5 + 1].TileType == TileID.HallowedGrass)
                        {
                            if (Main.rand.NextBool(2))
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.HallowedPlants;
                                Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(4, 7));
                                while (Main.tile[num4, num5].TileFrameX == 90)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(4, 7));
                                }
                            }
                            else
                            {
                                Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                                Main.tile[num4, num5].TileType = TileID.HallowedPlants2;
                                Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(2, 8));
                                while (Main.tile[num4, num5].TileFrameX == 90)
                                {
                                    Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(2, 8));
                                }
                            }
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendTileSquare(-1, num4, num5, 1, TileChangeType.None);
                            }
                        }
                        else if (Main.tile[num4, num5 + 1].TileType == TileID.JungleGrass)
                        {
                            Main.tile[num4, num5].Get<TileWallWireStateData>().HasTile = true;
                            Main.tile[num4, num5].TileType = TileID.JunglePlants2;
                            Main.tile[num4, num5].TileFrameX = (short)(18 * Main.rand.Next(9, 17));
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendTileSquare(-1, num4, num5, 1, TileChangeType.None);
                            }
                        }
                    }
                }
            }
            // 腐化烧瓶/猩红烧瓶：对应邪恶生态内 +3 防御与 +7% 减伤（两件效果相同，合并结算）
            if (corruptFlask && Player.ZoneCorrupt)
            {
                Player.statDefense += 3;
                Player.endurance += 0.07f;
            }
            if (crimsonFlask && Player.ZoneCrimson)
            {
                Player.statDefense += 3;
                Player.endurance += 0.07f;
            }
            // 远古化石：地下/洞穴层挖掘速度 +35%
            if (ancientFossil)
            {
                if (Player.ZoneDirtLayerHeight || Player.ZoneRockLayerHeight)
                {
                    Player.pickSpeed -= 0.35f;
                }
            }
            // 远古粉末：地下/洞穴/地狱层防御与减伤 + 挖掘提速
            if (archaicPowder)
            {
                if (Player.ZoneDirtLayerHeight || Player.ZoneRockLayerHeight || Player.ZoneUnderworldHeight)
                {
                    Player.statDefense += 3;
                    Player.endurance += 0.07f;
                    Player.pickSpeed -= 0.5f;
                }
            }
            // 辐射软泥：夜间暖黄照明 + 生命回复
            if (radiantOoze)
            {
                if (!Main.dayTime)
                {
                    Lighting.AddLight((int)(Player.position.X + (float)(Player.width / 2)) / 16, (int)(Player.position.Y + (float)(Player.height / 2)) / 16, 1f, 1f, 0.6f);
                    Player.lifeRegen += 2;
                }
            }
            // 蜜露：丛林区回血/防御/减伤；免疫毒液与中毒，额外附加蜂蜜式回复
            if (honeyDew)
            {
                if (Player.ZoneJungle)
                {
                    Player.lifeRegen += 2;
                    Player.statDefense += 5;
                    Player.endurance += 0.1f;
                }
                Player.buffImmune[70] = true;
                Player.buffImmune[20] = true;
                if (!Player.honey && Player.lifeRegen < 0)
                {
                    Player.lifeRegen += 4;
                    if (Player.lifeRegen > 0)
                    {
                        Player.lifeRegen = 0;
                    }
                }
                Player.lifeRegenTime += 2;
                Player.lifeRegen += 2;
            }
            // 活露：身处丛林时获得回血/防御/减伤
            if (livingDew)
            {
                if (Player.ZoneJungle)
                {
                    Player.lifeRegen += 2;
                    Player.statDefense += 5;
                    Player.endurance += 0.1f;
                }
            }
            // 仙馐药瓶：减伤/挖速/回血，免疫冰系与毒系 debuff，蜂蜜式回复
            if (ambrosialAmpoule)
            {
                Player.endurance += 0.12f;
                Player.pickSpeed -= 0.5f;
                Lighting.AddLight((int)(Player.position.X + (float)(Player.width / 2)) / 16, (int)(Player.position.Y + (float)(Player.height / 2)) / 16, 1f, 1f, 0.6f);
                Player.lifeRegen += 4;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Venom] = true;
                if (!Player.honey && Player.lifeRegen < 0)
                {
                    Player.lifeRegen += 4;
                    if (Player.lifeRegen > 0)
                    {
                        Player.lifeRegen = 0;
                    }
                }
                Player.lifeRegenTime += 2;
            }
            // 魔力凝胶：+20 魔力上限，静止时额外回蓝
            if (manaJelly)
            {
                Player.statManaMax2 += 20;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.manaRegenBonus += 2;
                }
            }
            // 生命凝胶：+20 生命上限，静止时额外回血
            if (lifeJelly)
            {
                Player.statLifeMax2 += 20;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.lifeRegen += 2;
                }
            }
            // 活力凝胶：移速/跳跃提升
            if (vitalJelly)
            {
                Player.moveSpeed += 0.1f;
                Player.jumpSpeedBoost += 1.0f;
            }
            // 大凝胶：移速/跳跃/生命/魔力上限，静止时回血回蓝
            if (grandGelatin)
            {
                Player.moveSpeed += 0.1f;
                Player.jumpSpeedBoost += 1.0f;
                Player.statLifeMax2 += 20;
                Player.statManaMax2 += 20;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.lifeRegen += 2;
                    Player.manaRegenBonus += 2;
                }
            }
            // 海贝壳：浸水时加防/减伤/移速并可水中呼吸
            if (seaShell)
            {
                if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                {
                    Player.statDefense += 3;
                    Player.endurance += 0.05f;
                    Player.moveSpeed += 0.15f;
                    Player.ignoreWater = true;
                }
            }
            // 爬虫甲壳：减伤 + 荆棘反伤
            if (crawCarapace)
            {
                Player.endurance += 0.05f;
                Player.thorns = 0.25f;
            }
            // 巨型陆龟壳：减速 + 荆棘反伤
            if (giantTortoiseShell)
            {
                Player.moveSpeed -= 0.1f;
                Player.thorns = 0.25f;
            }
            // 吞噬者：生命/魔力/移速/荆棘/减伤/静止回复 + 浸水增益
            if (theAbsorber)
            {
                Player.statLifeMax2 += 30;
                Player.statManaMax2 += 20;
                Player.moveSpeed += 0.12f;
                Player.jumpSpeedBoost += 1.2f;
                Player.thorns = 1f;
                Player.endurance += 0.06f;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.lifeRegen += 6;
                    Player.manaRegenBonus += 2;
                }
                if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                {
                    Player.statDefense += 5;
                    Player.endurance += 0.05f;
                    Player.moveSpeed += 0.2f;
                    Player.ignoreWater = true;
                }
            }
            // 巨龟壳：常驻减速；受伤后的爆发效果见 OnHurt（触发 ShellBoost）
            if (giantShell)
            {
                Player.moveSpeed -= 0.15f;
            }
            // 海绵：大量生存属性/静止回复/溺水免疫，受击反制见 ModifyHurt/PostHurt
            if (sponge)
            {
                Player.lifeRegen += 6;
                Player.endurance += 0.18f;
                Player.statLifeMax2 += 30;
                Player.statManaMax2 += 30;
                Player.moveSpeed += 0.12f;
                Player.jumpSpeedBoost += 1.2f;
                Player.thorns = 1f;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.lifeRegen += 6;
                    Player.manaRegenBonus += 2;
                }
                if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                {
                    Player.statDefense += 5;
                    Player.endurance += 0.1f;
                    Player.moveSpeed += 0.2f;
                    Player.ignoreWater = true;
                }
                Player.pickSpeed -= 0.5f;
                Lighting.AddLight((int)(Player.position.X + (float)(Player.width / 2)) / 16, (int)(Player.position.Y + (float)(Player.height / 2)) / 16, 1f, 1f, 0.6f);
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Venom] = true;
                if (!Player.honey && Player.lifeRegen < 0)
                {
                    Player.lifeRegen += 4;
                    if (Player.lifeRegen > 0)
                    {
                        Player.lifeRegen = 0;
                    }
                }
                Player.lifeRegenTime += 2;
            }
            // 腐坏大脑：免伤期间 1/8 概率在头顶召唤 AuraRain 落下；
            // 血量低于 75% 时增伤、低于 50% 时减速
            if (rottenBrain)
            {
                if (Player.immune)
                {
                    if (Main.rand.NextBool(8))
                    {
                        if (Player.whoAmI == Main.myPlayer)
                        {
                            for (int l = 0; l < 1; l++)
                            {
                                float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                                float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                                Vector2 vector = new(x, y);
                                float num15 = Player.position.X + (float)(Player.width / 2) - vector.X;
                                float num16 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                                num15 += (float)Main.rand.Next(-100, 101);
                                int num17 = 22;
                                float num18 = (float)Math.Sqrt((double)(num15 * num15 + num16 * num16));
                                num18 = (float)num17 / num18;
                                num15 *= num18;
                                num16 *= num18;
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<AuraRain>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(18), 2f, Player.whoAmI, 0f, 0f);
                                Main.projectile[num19].ai[1] = Player.position.Y;
                                Main.projectile[num19].tileCollide = false;
                            }
                        }
                    }
                }
                if (Player.statLife <= (Player.statLifeMax2 * 0.75f))
                {
                    Player.GetDamage<GenericDamageClass>() += 0.15f;
                }
                if (Player.statLife <= (Player.statLifeMax2 * 0.5f))
                {
                    Player.moveSpeed -= 0.05f;
                }
            }
            // 聚合大脑：常驻增伤/暴击；免伤期间概率降下 AuraRain；受伤困惑敌人见 OnHurt
            if (amalgamatedBrain)
            {
                Player.GetDamage<GenericDamageClass>() += 0.1f;
                Player.GetCritChance<GenericDamageClass>() += 5;
                if (Player.immune)
                {
                    if (Main.rand.NextBool(8))
                    {
                        if (Player.whoAmI == Main.myPlayer)
                        {
                            for (int l = 0; l < 1; l++)
                            {
                                float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                                float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                                Vector2 vector = new(x, y);
                                float num15 = Player.position.X + (float)(Player.width / 2) - vector.X;
                                float num16 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                                num15 += (float)Main.rand.Next(-100, 101);
                                int num17 = 22;
                                float num18 = (float)Math.Sqrt((double)(num15 * num15 + num16 * num16));
                                num18 = (float)num17 / num18;
                                num15 *= num18;
                                num16 *= num18;
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<AuraRain>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(60), 2f, Player.whoAmI, 0f, 0f);
                                Main.projectile[num19].ai[1] = Player.position.Y;
                                Main.projectile[num19].tileCollide = false;
                            }
                        }
                    }
                }
            }
            // 灾厄之戒：常驻增伤；免伤期间概率在头顶召唤站火（StandingFire）
            if (calamityRing)
            {
                Player.GetDamage<GenericDamageClass>() += 0.15f;
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (Player.immune)
                    {
                        if (Main.rand.NextBool(10))
                        {
                            for (int l = 0; l < 1; l++)
                            {
                                float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                                float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                                Vector2 vector = new(x, y);
                                float num15 = Player.position.X + (float)(Player.width / 2) - vector.X;
                                float num16 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                                num15 += (float)Main.rand.Next(-100, 101);
                                int num17 = 22;
                                float num18 = (float)Math.Sqrt((double)(num15 * num15 + num16 * num16));
                                num18 = (float)num17 / num18;
                                num15 *= num18;
                                num16 *= num18;
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<StandingFire>(), 30, 5f, Player.whoAmI, 0f, 0f);
                                Main.projectile[num19].ai[1] = Player.position.Y;
                            }
                        }
                    }
                }
            }
            // 阿巴顿：+8% 通用暴击，免疫硫磺火减益（走双版本软依赖查找）
            if (abaddon)
            {
                Player.GetCritChance<GenericDamageClass>() += 8;
                AddCalamityBuffImmune(Player, "CalamityMod", "BrimstoneFlames");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "BrimstoneFlames");
            }
            // 炼狱：每 600 帧（10 秒）从高空向玩家瞄准方向齐射一轮扇形地狱火流星雨
            if (gehenna)
            {
                // 首次装备将倒计时初始化为 600，之后每帧递减，归零即发射
                if (gehennaFireCountdown == 0)
                {
                    gehennaFireCountdown = 600;
                }
                if (gehennaFireCountdown > 0)
                {
                    gehennaFireCountdown--;
                    if (gehennaFireCountdown == 0)
                    {
                        if (Player.whoAmI == Main.myPlayer)
                        {
                            int speed2 = 25;
                            float spawnX = Main.rand.Next(1000) - 500 + Player.Center.X;
                            float spawnY = -1000 + Player.Center.Y;
                            Vector2 baseSpawn = new(spawnX, spawnY);
                            Vector2 baseVelocity = Player.Center - baseSpawn;
                            baseVelocity.Normalize();
                            baseVelocity = baseVelocity * speed2;
                            for (int i = 0; i < FireProjectiles; i++)
                            {
                                Vector2 spawn = baseSpawn;
                                spawn.X = spawn.X + i * 30 - (FireProjectiles * 15);
                                Vector2 velocity = baseVelocity;
                                velocity = baseVelocity.RotatedBy(MathHelper.ToRadians(-FireAngleSpread / 2 + (FireAngleSpread * i / (float)FireProjectiles)));
                                velocity.X = velocity.X + 3 * Main.rand.NextFloat() - 1.5f;
                                int projectile = Projectile.NewProjectile(Player.GetSource_FromThis(), spawn.X, spawn.Y, velocity.X, velocity.Y, ModContent.ProjectileType<BrimstoneHellfireballFriendly2>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(54), 5f, Main.myPlayer, 0f, 0f);
                                Main.projectile[projectile].tileCollide = false;
                                Main.projectile[projectile].timeLeft = 50;
                            }
                        }
                    }
                }
            }
            // 虚空之烬：常驻增伤/熔岩免疫；每 10 秒地狱火齐射，免伤期间再召唤高伤站火
            if (voidofExtinction)
            {
                Player.GetDamage<GenericDamageClass>() += 0.15f;
                Player.lavaRose = true;
                Player.lavaMax += 240;
                if (Player.lavaWet)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.25f;
                }
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (Player.immune)
                    {
                        if (Main.rand.NextBool(10))
                        {
                            for (int l = 0; l < 1; l++)
                            {
                                float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                                float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                                Vector2 vector = new(x, y);
                                float num15 = Player.position.X + (float)(Player.width / 2) - vector.X;
                                float num16 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                                num15 += (float)Main.rand.Next(-100, 101);
                                int num17 = 22;
                                float num18 = (float)Math.Sqrt((double)(num15 * num15 + num16 * num16));
                                num18 = (float)num17 / num18;
                                num15 *= num18;
                                num16 *= num18;
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<StandingFire>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(40), 5f, Player.whoAmI, 0f, 0f);
                                Main.projectile[num19].ai[1] = Player.position.Y;
                            }
                        }
                    }
                }
                // 虚空之烬的地狱火流星雨：与炼狱同款倒计时机制（600 帧一轮）
                if (voidFireCountdown == 0)
                {
                    voidFireCountdown = 600;
                }
                if (voidFireCountdown > 0)
                {
                    voidFireCountdown--;
                    if (voidFireCountdown == 0)
                    {
                        if (Player.whoAmI == Main.myPlayer)
                        {
                            int speed2 = 25;
                            float spawnX = Main.rand.Next(1000) - 500 + Player.Center.X;
                            float spawnY = -1000 + Player.Center.Y;
                            Vector2 baseSpawn = new(spawnX, spawnY);
                            Vector2 baseVelocity = Player.Center - baseSpawn;
                            baseVelocity.Normalize();
                            baseVelocity = baseVelocity * speed2;
                            for (int i = 0; i < FireProjectiles; i++)
                            {
                                Vector2 spawn = baseSpawn;
                                spawn.X = spawn.X + i * 30 - (FireProjectiles * 15);
                                Vector2 velocity = baseVelocity;
                                velocity = baseVelocity.RotatedBy(MathHelper.ToRadians(-FireAngleSpread / 2 + (FireAngleSpread * i / (float)FireProjectiles)));
                                velocity.X = velocity.X + 3 * Main.rand.NextFloat() - 1.5f;
                                int projectile = Projectile.NewProjectile(Player.GetSource_FromThis(), spawn.X, spawn.Y, velocity.X, velocity.Y, ModContent.ProjectileType<BrimstoneHellfireballFriendly2>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(70), 5f, Main.myPlayer, 0f, 0f);
                                Main.projectile[projectile].tileCollide = false;
                                Main.projectile[projectile].timeLeft = 50;
                            }
                        }
                    }
                }
            }
            // 利维坦龙涎香：免疫溺水、水下作战；移动时制造毒海水，并周期对近身敌人施毒
            if (levianthanAmbergris)
            {
                Player.ignoreWater = true;
                if (!Player.lavaWet && !Player.honeyWet)
                {
                    if (!Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                    {
                        Player.endurance += 0.05f;
                        Player.GetDamage<GenericDamageClass>() += 0.05f;
                    }
                    else
                    {
                        Player.GetDamage<GenericDamageClass>() += 0.1f;
                        Player.statDefense += 20;
                        Player.moveSpeed += 0.75f;
                    }
                }
                if (((double)Player.velocity.X > 0 || (double)Player.velocity.Y > 0 || (double)Player.velocity.X < -0.1 || (double)Player.velocity.Y < -0.1))
                {
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center.X, Player.Center.Y, 0f, 0f, ModContent.ProjectileType<PoisonousSeawater>(), 150, 5f, Player.whoAmI, 0f, 0f);
                    }
                }
                int seaCounter = 0;
                Lighting.AddLight((int)(Player.Center.X / 16f), (int)(Player.Center.Y / 16f), 0f, 0.5f, 1.25f);
                int num = BuffID.Venom;
                float num2 = 200f;
                bool flag = seaCounter % 60 == 0;
                int num3 = 15;
                int random = Main.rand.Next(5);
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (random == 0 && Player.immune && (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir)))
                    {
                        for (int l = 0; l < 200; l++)
                        {
                            NPC nPC = Main.npc[l];
                            if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num] && Vector2.Distance(Player.Center, nPC.Center) <= num2)
                            {
                                if (nPC.FindBuffIndex(num) == -1)
                                {
                                    nPC.AddBuff(num, 300, false);
                                }
                                if (flag)
                                {
                                    nPC.StrikeNPC(nPC.CalculateHitInfo(num3, 0));
                                    if (Main.netMode != NetmodeID.SinglePlayer)
                                    {
                                        NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, l, (float)num3, 0f, 0f, 0, 0, 0);
                                    }
                                }
                            }
                        }
                    }
                }
                seaCounter++;
                if (seaCounter >= 180)
                {
                }
            }
            // 大杂烩（The Amalgam）：聚合上述多种高级饰品的全部结算，属终极饰品（数值更高）
            if (theAmalgam)
            {
                Player.GetDamage<GenericDamageClass>() += 0.2f;
                Player.GetCritChance<GenericDamageClass>() += 10;
                // 大杂烩 = 四件原料（血脑/虚空/龙涎香/菌块）效果集合：补继承虚空之烬的
                // 熔岩免疫与浸岩浆增伤（原只装虚空戒才有，合体后也应一并继承）
                Player.lavaRose = true;
                Player.lavaMax += 240;
                if (Player.lavaWet)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.25f;
                }
                if (Player.immune)
                {
                    // 免伤期间 1/10 概率降下一道天降：灵气雨与烈焰随机二选一
                    if (Main.rand.Next(10) == 0)
                    {
                        // 仅本地端生成：非主机客户端也生成会重复创建弹幕、造成不同步
                        if (Player.whoAmI == Main.myPlayer)
                        {
                            for (int l = 0; l < 1; l++)
                            {
                                float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                                float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                                Vector2 vector = new(x, y);
                                float num15 = Player.position.X + (float)(Player.width / 2) - vector.X;
                                float num16 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                                num15 += (float)Main.rand.Next(-100, 101);
                                int num17 = 22;
                                float num18 = (float)Math.Sqrt((double)(num15 * num15 + num16 * num16));
                                num18 = (float)num17 / num18;
                                num15 *= num18;
                                num16 *= num18;
                                int num19;
                                if (Main.rand.NextBool(2))
                                {
                                    num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<StandingFire>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(500), 5f, Player.whoAmI, 0f, 0f);
                                }
                                else
                                {
                                    num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<AuraRain>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(500), 2f, Player.whoAmI, 0f, 0f);
                                    Main.projectile[num19].tileCollide = false;
                                }
                                Main.projectile[num19].ai[1] = Player.position.Y;
                            }
                        }
                    }
                }
                // 大杂烩的地狱火流星雨：同样 600 帧一轮
                if (amalgamFireCountdown == 0)
                {
                    amalgamFireCountdown = 600;
                }
                if (amalgamFireCountdown > 0)
                {
                    amalgamFireCountdown--;
                    if (amalgamFireCountdown == 0)
                    {
                        if (Player.whoAmI == Main.myPlayer)
                        {
                            int speed2 = 25;
                            float spawnX = Main.rand.Next(1000) - 500 + Player.Center.X;
                            float spawnY = -1000 + Player.Center.Y;
                            Vector2 baseSpawn = new(spawnX, spawnY);
                            Vector2 baseVelocity = Player.Center - baseSpawn;
                            baseVelocity.Normalize();
                            baseVelocity = baseVelocity * speed2;
                            for (int i = 0; i < FireProjectiles; i++)
                            {
                                Vector2 spawn = baseSpawn;
                                spawn.X = spawn.X + i * 30 - (FireProjectiles * 15);
                                Vector2 velocity = baseVelocity;
                                velocity = baseVelocity.RotatedBy(MathHelper.ToRadians(-FireAngleSpread / 2 + (FireAngleSpread * i / (float)FireProjectiles)));
                                velocity.X = velocity.X + 3 * Main.rand.NextFloat() - 1.5f;
                                int projectile = Projectile.NewProjectile(Player.GetSource_FromThis(), spawn.X, spawn.Y, velocity.X, velocity.Y, ModContent.ProjectileType<BrimstoneHellfireballFriendly2>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(500), 5f, Main.myPlayer, 0f, 0f);
                                Main.projectile[projectile].tileCollide = false;
                                Main.projectile[projectile].timeLeft = 50;
                            }
                        }
                    }
                }
                Player.ignoreWater = true;
                if (!Player.lavaWet && !Player.honeyWet)
                {
                    if (!Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                    {
                        Player.endurance += 0.1f;
                        Player.GetDamage<GenericDamageClass>() += 0.1f;
                    }
                    else
                    {
                        Player.GetDamage<GenericDamageClass>() += 0.1f;
                        Player.statDefense += 40;
                        Player.moveSpeed += 0.75f;
                    }
                }
                if (((double)Player.velocity.X > 0 || (double)Player.velocity.Y > 0 || (double)Player.velocity.X < -0.1 || (double)Player.velocity.Y < -0.1))
                {
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center.X, Player.Center.Y, 0f, 0f, ModContent.ProjectileType<PoisonousSeawater>(), (int)Player.GetDamage<GenericDamageClass>().ApplyTo(500), 5f, Player.whoAmI, 0f, 0f);
                    }
                }
                int seaCounter = 0;
                Lighting.AddLight((int)(Player.Center.X / 16f), (int)(Player.Center.Y / 16f), 0f, 0.5f, 1.25f);
                int num = BuffID.Venom;
                float num2 = 200f;
                bool flag = seaCounter % 60 == 0;
                int num3 = 80;
                int random = Main.rand.Next(5);
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (random == 0 && Player.immune && (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir)))
                    {
                        for (int l = 0; l < 200; l++)
                        {
                            NPC nPC = Main.npc[l];
                            if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num] && Vector2.Distance(Player.Center, nPC.Center) <= num2)
                            {
                                if (nPC.FindBuffIndex(num) == -1)
                                {
                                    nPC.AddBuff(num, 300, false);
                                }
                                if (flag)
                                {
                                    nPC.StrikeNPC(nPC.CalculateHitInfo(num3, 0));
                                    if (Main.netMode != NetmodeID.SinglePlayer)
                                    {
                                        NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, l, (float)num3, 0f, 0f, 0, 0, 0);
                                    }
                                }
                            }
                        }
                    }
                }
                seaCounter++;
                if (seaCounter >= 180)
                {
                }
            }
            if (theCommunity)
            {
                int defeated = CommunityBossCount();
                if (defeated > 0)
                {
                    // 进度系数：击败 1 档（石巨人）= 0（初始值），全清 18 档 = 1（满配值）
                    float t = (defeated - 1) / 17f;
                    float damage = InitDamage + (MaxDamage - InitDamage) * t;
                    float crit = InitCrit + (MaxCrit - InitCrit) * t;
                    float meleeSpeed = InitMeleeSpeed + (MaxMeleeSpeed - InitMeleeSpeed) * t;
                    float endurance = InitEndurance + (MaxEndurance - InitEndurance) * t;
                    float moveSpeed = InitMoveSpeed + (MaxMoveSpeed - InitMoveSpeed) * t;
                    float lifePct = InitLifePct + (MaxLifePct - InitLifePct) * t;
                    int lifeRegen = (int)(InitLifeRegen + (MaxLifeRegen - InitLifeRegen) * t);
                    int defense = (int)(InitDefense + (MaxDefense - InitDefense) * t);
                    float knockback = InitKnockback + (MaxKnockback - InitKnockback) * t;
                    Player.GetDamage<GenericDamageClass>() += damage;
                    Player.GetCritChance<GenericDamageClass>() += crit;
                    Player.GetAttackSpeed<MeleeDamageClass>() += meleeSpeed;
                    Player.endurance += endurance;
                    Player.moveSpeed += moveSpeed;
                    Player.statLifeMax2 += (int)(Player.statLifeMax2 * lifePct);
                    Player.lifeRegen += lifeRegen;
                    Player.statDefense += defense;
                    Player.GetKnockback<SummonDamageClass>().Base += knockback;
                    // 最大飞行时间恒 ×1.15（与经典版源一致）
                    if (Player.wingTimeMax > 0)
                    {
                        Player.wingTimeMax = (int)(Player.wingTimeMax * 1.15);
                    }
                }
                // Debuff 时间缩减：始终生效，不使用 Boss 等级解锁。
                // 每 1 秒触发一轮：治疗冷却（药水病）缩减 2%；魔力病缩减 20%；一般 Debuff 缩减 10%。
                if (++communityDebuffTickCounter >= 60)
                {
                    communityDebuffTickCounter = 0;
                    // 治疗冷却（药水病）
                    int idx = Player.FindBuffIndex(BuffID.PotionSickness);
                    if (idx >= 0)
                        Player.buffTime[idx] = (int)(Player.buffTime[idx] * 0.98f);
                    // 魔力病
                    idx = Player.FindBuffIndex(BuffID.ManaSickness);
                    if (idx >= 0)
                        Player.buffTime[idx] = (int)(Player.buffTime[idx] * 0.8f);
                    // 一般 Debuff（黑名单除外）
                    for (int i = 0; i < Player.buffTime.Length; i++)
                    {
                        int buffType = Player.buffType[i];
                        if (buffType > 0 && Player.buffTime[i] > 0 && Main.debuff[buffType] && !IsBuffInCommunityBlacklist(buffType))
                        {
                            Player.buffTime[i] = (int)(Player.buffTime[i] * 0.9f);
                        }
                    }
                }
            }
            if (deificAmulet)
            {
                Player.panic = true;
                Player.manaMagnet = true;
                Player.magicCuffs = true;
                Player.GetArmorPenetration<GenericDamageClass>() += 25;
                if (Player.wet)
                {
                    Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 1.35f, 0.3f, 0.9f);
                }
            }
            if (frigidBulwark)
            {
                Player.noKnockback = true;
                if (Player.statLife > (int)((double)Player.statLifeMax2 * 0.25))
                {
                    Player.hasPaladinShield = true;
                    if (Player.whoAmI != Main.myPlayer && Player.miscCounter % 10 == 0)
                    {
                        int myPlayer = Main.myPlayer;
                        if (Main.player[myPlayer].team == Player.team && Player.team != 0)
                        {
                            float arg = Player.position.X - Main.player[myPlayer].position.X;
                            float num3 = Player.position.Y - Main.player[myPlayer].position.Y;
                            if ((float)Math.Sqrt((double)(arg * arg + num3 * num3)) < 800f)
                            {
                                Main.player[myPlayer].AddBuff(BuffID.PaladinsShield, 20, true);
                            }
                        }
                    }
                }
                if (Player.statLife <= (int)((double)Player.statLifeMax2 * 0.5))
                {
                    Player.AddBuff(BuffID.IceBarrier, 5, true);
                }
                if (Player.statLife <= (int)((double)Player.statLifeMax2 * 0.15))
                {
                    Player.endurance += 0.05f;
                }
            }
            if (rampartofDeities)
            {
                Player.panic = true;
                Player.manaMagnet = true;
                Player.magicCuffs = true;
                Player.GetArmorPenetration<GenericDamageClass>() += 50;
                if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                {
                    Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 1.35f, 0.3f, 0.9f);
                }
                Player.noKnockback = true;
                if (Player.statLife > (int)((double)Player.statLifeMax2 * 0.25))
                {
                    Player.hasPaladinShield = true;
                    if (Player.whoAmI != Main.myPlayer && Player.miscCounter % 10 == 0)
                    {
                        int myPlayer = Main.myPlayer;
                        if (Main.player[myPlayer].team == Player.team && Player.team != 0)
                        {
                            float arg = Player.position.X - Main.player[myPlayer].position.X;
                            float num3 = Player.position.Y - Main.player[myPlayer].position.Y;
                            if ((float)Math.Sqrt((double)(arg * arg + num3 * num3)) < 800f)
                            {
                                Main.player[myPlayer].AddBuff(BuffID.PaladinsShield, 20, true);
                            }
                        }
                    }
                }
                if (Player.statLife <= (int)((double)Player.statLifeMax2 * 0.5))
                {
                    Player.AddBuff(BuffID.IceBarrier, 5, true);
                }
                if (Player.statLife <= (int)((double)Player.statLifeMax2 * 0.15))
                {
                    Player.endurance += 0.05f;
                }
            }
            if (omegaBlueSet) //should apply after rev caps, actually those are gone so AAAAA
            {
                //add tentacles
                // 生成触手只在主人本机做（对齐上游 Calamity 的同名判据）：本钩子对每名玩家、每一端都会跑，
                // 少了后半句时，客户端会替别的玩家生成一套 owner 记成本机玩家的触手，服务端还会以 255 当 owner。
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<OmegaBlueTentacle>()] < 6 && Main.myPlayer == Player.whoAmI)
                {
                    bool[] tentaclesPresent = new bool[6];
                    for (int i = 0; i < 1000; i++)
                    {
                        Projectile projectile = Main.projectile[i];
                        if (projectile.active && projectile.type == ModContent.ProjectileType<OmegaBlueTentacle>() && projectile.owner == Main.myPlayer && projectile.ai[1] >= 0f && projectile.ai[1] < 6f)
                        {
                            tentaclesPresent[(int)projectile.ai[1]] = true;
                        }
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        if (!tentaclesPresent[i])
                        {
                            int damage = (int)Player.GetDamage<GenericDamageClass>().ApplyTo(1500);
                            Vector2 vel = new Vector2(Main.rand.Next(-13, 14), Main.rand.Next(-13, 14)) * 0.25f;
                            Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center, vel, ModContent.ProjectileType<OmegaBlueTentacle>(), damage, 8f, Main.myPlayer, Main.rand.Next(120), i);
                        }
                    }
                }
                float damageUp = 0.1f;
                int critUp = 10;
                if (omegaBlueHentai)
                {
                    damageUp *= 2f;
                    critUp *= 2;
                }
                Player.GetDamage<GenericDamageClass>() += damageUp;
                Player.GetCritChance<GenericDamageClass>() += critUp;
            }
            if (tarraSet)
            {
                Player.calmed = (!tarraMelee);
                Player.lifeMagnet = true;
            }
            // 龙蒿召唤头（TarragonHornedHelm）的 tarraSummon 套装效果（照现代灾厄的实现口径）：
            // ① 玩家中心常驻绿色光照；② 满血时额外 +2 仆从上限与 +10% 召唤伤害；
            // ③ 生命光环：300 像素内每 80 帧对敌对 NPC 结算一次召唤伤害（120 基础 × 召唤伤害加成）。
            // 经典版把计时器写成方法内局部变量、闸门恒真，等于每帧都打（约 60 倍），属上游 bug，故取现代口径。
            // 生成/结算只在主人端做：本钩子每名玩家 × 每一端都会跑。
            if (tarraSummon)
            {
                Lighting.AddLight((int)(Player.Center.X / 16f), (int)(Player.Center.Y / 16f), 0f, 3f, 0f);
                if (Player.statLife >= Player.statLifeMax2)
                {
                    Player.GetDamage<SummonDamageClass>() += 0.1f;
                    Player.maxMinions += 2;
                }
                const int FramesPerHit = 80;
                tarraLifeAuraTimer = (tarraLifeAuraTimer + 1) % FramesPerHit;
                if (tarraLifeAuraTimer == 0 && Player.whoAmI == Main.myPlayer)
                {
                    int auraDamage = (int)Player.GetTotalDamage<SummonDamageClass>().ApplyTo(120f);
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (!npc.active || npc.friendly || npc.dontTakeDamage)
                            continue;
                        if (Vector2.Distance(Player.Center, npc.Center) <= 300f)
                            Player.ApplyDamageToNPC(npc, auraDamage, 0f, 0, false, DamageClass.Summon);
                    }
                }
            }
            if (tarraMelee)
            {
                if (tarraDefense)
                {
                    tarraDefenseTime--;
                    if (tarraDefenseTime <= 0)
                    {
                        tarraDefenseTime = 600;
                        tarraCooldown = 1800;
                        tarraDefense = false;
                    }
                    for (int j = 0; j < 2; j++)
                    {
                        int num = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, DustID.ChlorophyteWeapon, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 2f);
                        Dust expr_A4_cp_0 = Main.dust[num];
                        expr_A4_cp_0.position.X = expr_A4_cp_0.position.X + (float)Main.rand.Next(-20, 21);
                        Dust expr_CB_cp_0 = Main.dust[num];
                        expr_CB_cp_0.position.Y = expr_CB_cp_0.position.Y + (float)Main.rand.Next(-20, 21);
                        Main.dust[num].velocity *= 0.9f;
                        Main.dust[num].noGravity = true;
                        Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                        Main.dust[num].shader = GameShaders.Armor.GetSecondaryShader(Player.cWaist, Player);
                        if (Main.rand.NextBool(2))
                        {
                            Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                        }
                    }
                }
                if (tarraCooldown > 0)
                    tarraCooldown--;
            }
            if (bloodflareSet)
            {
                if (bloodflareHeartTimer > 0)
                    bloodflareHeartTimer--;
                if (bloodflareManaTimer > 0)
                    bloodflareManaTimer--;
            }
            // 血炎召唤头（BloodflareHelmet）的 bloodflareSummon 套装效果（照经典版）：
            // ① 生命 ≥90% 时 +10% 召唤伤害；≤50% 时 +20 防御与 +2 生命再生（两者互斥）；
            // ② 每 900 帧（15 秒）在主人端围绕自身 550 像素生成 3 枚 GhostlyMine（初始角度 ai[0] = I×120）。
            // 本钩子每名玩家 × 每一端都会跑，故生成侧带 whoAmI 判据。
            if (bloodflareSummon)
            {
                if (Player.statLife >= (int)(Player.statLifeMax2 * 0.9))
                {
                    Player.GetDamage<SummonDamageClass>() += 0.1f;
                }
                else if (Player.statLife <= (int)(Player.statLifeMax2 * 0.5))
                {
                    Player.statDefense += 20;
                    Player.lifeRegen += 2;
                }
                if (bloodflareSummonTimer > 0)
                    bloodflareSummonTimer--;
                if (Player.whoAmI == Main.myPlayer && bloodflareSummonTimer <= 0)
                {
                    bloodflareSummonTimer = 900;
                    // 伤害口径：经典版写的是 (auricSet ? 15000 : 5000) × 召唤伤害的 Multiplicative 部分，
                    // 只吃乘算、几乎不随配装增长，属笔误；这里保留金源档位，改按完整召唤伤害加成缩放。
                    int mineBase = auricSet ? 15000 : 5000;
                    int mineDamage = (int)Player.GetTotalDamage<SummonDamageClass>().ApplyTo(mineBase);
                    for (int i = 0; i < 3; i++)
                    {
                        float orbitAngle = i * 120f;
                        int mine = Projectile.NewProjectile(Player.GetSource_FromThis(),
                            Player.Center.X + (float)(Math.Sin(orbitAngle) * 550), Player.Center.Y + (float)(Math.Cos(orbitAngle) * 550),
                            0f, 0f, ModContent.ProjectileType<GhostlyMine>(), mineDamage, 1f, Player.whoAmI, orbitAngle, 0f);
                        Main.projectile[mine].originalDamage = mineBase;         // 供 tML 的仆从伤害缩放口径参照
                        Main.projectile[mine].DamageType = DamageClass.Generic;  // 伤害已在生成方算好，避免再乘一次召唤加成
                    }
                }
            }
            // 弑神者召唤头（GodSlayerHornedHelm）的两件事：
            // ① 幻影节流预算每帧衰减 2.5（经典版 CalamityPlayerPreTrailer.cs:3806-3809 原样）；
            // ② 每帧维护噬神机械蠕虫（生成只在 UpdateGodSlayerMechworm 内做主人端判据）。
            if (godSlayerDmg > 0f)
                godSlayerDmg -= 2.5f;
            if (godSlayerDmg < 0f)
                godSlayerDmg = 0f;
            if (godSlayerSummon)
                UpdateGodSlayerMechworm();
            if (bloodflareMelee)
            {
                if (bloodflareMeleeHits >= 15)
                {
                    bloodflareMeleeHits = 0;
                    bloodflareFrenzyTimer = 300;
                }
                if (bloodflareFrenzyTimer > 0)
                {
                    bloodflareFrenzyTimer--;
                    if (bloodflareFrenzyTimer <= 0)
                    {
                        bloodflareFrenzyCooldown = 1800;
                    }
                    Player.GetCritChance<MeleeDamageClass>() += 25;
                    Player.GetDamage<MeleeDamageClass>() += 0.25f;
                    for (int j = 0; j < 2; j++)
                    {
                        int num = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, DustID.Blood, 0f, 0f, 100, default(Color), 2f);
                        Dust expr_A4_cp_0 = Main.dust[num];
                        expr_A4_cp_0.position.X = expr_A4_cp_0.position.X + (float)Main.rand.Next(-20, 21);
                        Dust expr_CB_cp_0 = Main.dust[num];
                        expr_CB_cp_0.position.Y = expr_CB_cp_0.position.Y + (float)Main.rand.Next(-20, 21);
                        Main.dust[num].velocity *= 0.9f;
                        Main.dust[num].noGravity = true;
                        Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                        Main.dust[num].shader = GameShaders.Armor.GetSecondaryShader(Player.cWaist, Player);
                        if (Main.rand.Next(2) == 0)
                        {
                            Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                        }
                    }
                }
                if (bloodflareFrenzyCooldown > 0)
                    bloodflareFrenzyCooldown--;
            }
            // 血炎射手套装的灵魂爆发冷却：与近战狂怒冷却同为跨帧计时器，只在死亡时复位
            if (bloodflareRangedCooldown > 0)
                bloodflareRangedCooldown--;
            if (silvaSet)
            {
                foreach (int debuff in CalamityDemutation.debuffList)
                    Player.buffImmune[debuff] = true;
            }
            if (silvaCountdown > 0 && hasSilvaEffect && silvaSet)
            {
                for (int i = 0; i < Player.buffTime.Length; i++)
                {
                    int buffType = Player.buffType[i];
                    if (buffType > 0 && Player.buffTime[i] > 0 && Main.debuff[buffType] && !IsBuffInCommunityBlacklist(buffType))
                    {
                        Player.buffImmune[buffType] = true;
                    }
                }
                silvaCountdown--;
                if (silvaCountdown <= 0)
                {
                    SoundEngine.PlaySound(new SoundStyle("CalamityDemutation/Sounds/Custom/SilvaDispel"), Player.position);
                }
                for (int j = 0; j < 2; j++)
                {
                    int num = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, DustID.ChlorophyteWeapon, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 2f);
                    Dust expr_A4_cp_0 = Main.dust[num];
                    expr_A4_cp_0.position.X = expr_A4_cp_0.position.X + (float)Main.rand.Next(-20, 21);
                    Dust expr_CB_cp_0 = Main.dust[num];
                    expr_CB_cp_0.position.Y = expr_CB_cp_0.position.Y + (float)Main.rand.Next(-20, 21);
                    Main.dust[num].velocity *= 0.9f;
                    Main.dust[num].noGravity = true;
                    Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    Main.dust[num].shader = GameShaders.Armor.GetSecondaryShader(Player.cWaist, Player);
                    if (Main.rand.NextBool(2))
                    {
                        Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    }
                }
            }
            if (silvaHitCounter > 0)
            {
                Player.statLifeMax2 -= silvaHitCounter * 100;
                if (Player.statLifeMax2 <= 400)
                {
                    Player.statLifeMax2 = 400;
                    if (silvaCountdown > 0)
                    {
                        if (Player.FindBuffIndex(ModContent.BuffType<SilvaRevival>()) > -1) { Player.ClearBuff(ModContent.BuffType<SilvaRevival>()); }
                        SoundEngine.PlaySound(new SoundStyle("CalamityDemutation/Sounds/Custom/SilvaDispel"), Player.position);
                    }
                    silvaCountdown = 0;
                }
            }
            if (auricSet && silvaMelee)
            {
                double multiplier = (double)Player.statLife / (double)Player.statLifeMax2;
                Player.GetDamage<MeleeDamageClass>() += (float)(multiplier * 0.2); //ranges from 1.2 times to 1 times
            }
            // 始源林海射手套装：持远程武器时射速 +10%（silvaRanged）。
            // 经典版把它写成 UseTimeMultiplier 返回 1.1/1.2——tML 里该值 >1 表示「更慢」，与 tooltip 相反；
            // CI 的还原件（SilvaHeadRanged.UpdateArmorSet）按 +10% 远程攻速实现，本工程从 CI，让 tooltip 成真。
            if (silvaRanged)
            {
                Item heldItem = Player.HeldItem;
                if (heldItem.useTime > 3 && heldItem.CountsAsClass<RangedDamageClass>())
                    Player.GetAttackSpeed<RangedDamageClass>() += 0.1f;
            }
            if (godSlayerDamageProtect)
            {
                if (godSlayerDamageProtectMax < 80)
                    godSlayerDamageProtectMax++;
            }
            if (godSlayerCooldown)
            {
                Player.GetDamage<GenericDamageClass>() += 0.1f;
            }
            if (godSlayerMeleefireCD > 0)
                godSlayerMeleefireCD--;
            // 弑神者射手套装的破片弹闸门：与 abaddonCritCooldown 同为跨帧计时器，只在死亡时复位
            if (godSlayerShrapnelCooldown > 0)
                godSlayerShrapnelCooldown--;
            if (abaddonCritCooldown > 0)
                abaddonCritCooldown--;
            if (frostBarrier)
            {
                Player.buffImmune[46] = true;
            }
            if (psychoticAmulet)
            {
                Player.GetDamage<RangedDamageClass>() += 0.05f;
                Player.GetCritChance<RangedDamageClass>() += 5;
                Player.shroomiteStealth = true;
            }
            if (titanScale)
            {
                Player.endurance += 0.05f;
                Player.statDefense += 5;
                Player.kbBuff = true;
            }
            if (cadence)
            {
                Player.discountAvailable = true;
                Player.lifeMagnet = true;
                Player.calmed = true;
                Player.loveStruck = true;
                Player.lifeRegen += 4;
                Player.statLifeMax2 += (int)(Player.statLifeMax2 * 0.2);
            }
            if (holyWrath)
            {
                Player.GetDamage<GenericDamageClass>() += 0.12f;
                Player.moveSpeed += 0.05f;
            }
            if (armorShattering)
            {
                Player.GetCritChance<GenericDamageClass>() += 8;
                Player.GetDamage<GenericDamageClass>() += 0.08f;
            }
            if (armorCrumbling)
            {
                Player.GetCritChance<GenericDamageClass>() += 5;
            }
            if (bounding)
            {
                Player.jumpSpeedBoost += 0.5f;
                Player.jumpHeight += 10;
                Player.extraFall += 25;
            }
            if (soaring) Player.wingTimeMax = (int)((double)Player.wingTimeMax * 1.1);// 解除处于飞行状态的限制
            if (profanedRage)
            {
                Player.GetCritChance<GenericDamageClass>() += 12;
                Player.moveSpeed += 0.05f;
            }
            if (ceaselessHunger)
            {
                for (int j = 0; j < 400; j++)
                {
                    if (Main.item[j].active && Main.item[j].noGrabDelay == 0 && Main.item[j].playerIndexTheItemIsReservedFor == Player.whoAmI)
                    {
                        int num = Main.maxTilesX;
                        if (new Rectangle((int)Player.position.X - num, (int)Player.position.Y - num, Player.width + num * 2, Player.height + num * 2).Intersects(new Rectangle((int)Main.item[j].position.X, (int)Main.item[j].position.Y, Main.item[j].width, Main.item[j].height)))
                        {
                            Main.item[j].beingGrabbed = true;
                            if ((double)Player.position.X + (double)Player.width * 0.5 > (double)Main.item[j].position.X + (double)Main.item[j].width * 0.5)
                            {
                                if (Main.item[j].velocity.X < 40f + Player.velocity.X)
                                {
                                    Item item = Main.item[j];
                                    item.velocity.X = item.velocity.X + 4.5f;
                                }
                                if (Main.item[j].velocity.X < 0f)
                                {
                                    Item item = Main.item[j];
                                    item.velocity.X = item.velocity.X + 4.5f * 0.75f;
                                }
                            }
                            else
                            {
                                if (Main.item[j].velocity.X > -40f + Player.velocity.X)
                                {
                                    Item item = Main.item[j];
                                    item.velocity.X = item.velocity.X - 4.5f;
                                }
                                if (Main.item[j].velocity.X > 0f)
                                {
                                    Item item = Main.item[j];
                                    item.velocity.X = item.velocity.X - 4.5f * 0.75f;
                                }
                            }
                            if ((double)Player.position.Y + (double)Player.height * 0.5 > (double)Main.item[j].position.Y + (double)Main.item[j].height * 0.5)
                            {
                                if (Main.item[j].velocity.Y < 40f)
                                {
                                    Item item = Main.item[j];
                                    item.velocity.Y = item.velocity.Y + 4.5f;
                                }
                                if (Main.item[j].velocity.Y < 0f)
                                {
                                    Item item = Main.item[j];
                                    item.velocity.Y = item.velocity.Y + 4.5f * 0.75f;
                                }
                            }
                            else
                            {
                                if (Main.item[j].velocity.Y > -40f)
                                {
                                    Item item = Main.item[j];
                                    item.velocity.Y = item.velocity.Y - 4.5f;
                                }
                                if (Main.item[j].velocity.Y > 0f)
                                {
                                    Item item = Main.item[j];
                                    item.velocity.Y = item.velocity.Y - 4.5f * 0.75f;
                                }
                            }
                        }
                    }
                }
            }
            if (draconicSurgeCooldown > 0)
                draconicSurgeCooldown--;
            if (draconicSurge)
            {
                Player.wingTimeMax = (int)((double)Player.wingTimeMax * 1.35);
                Player.statDefense += 16;
            }
            if (calcium)
            {
                Player.noFallDmg = true;
            }
            // 冷却机架：只有本地玩家需要冷却条 UI
            if (Player.whoAmI == Main.myPlayer)
            {
                // 逐帧结算所有冷却：允许倒计时者递减，Tick 恒执行，到期（timeLeft < 0）时执行 OnCompleted、播结束音效并移除
                IList<string> expiredCooldowns = new List<string>(16);
                var cdIterator = cooldowns.GetEnumerator();
                while (cdIterator.MoveNext())
                {
                    KeyValuePair<string, CooldownInstance> kv = cdIterator.Current;
                    string cdID = kv.Key;
                    CooldownInstance cdInstance = kv.Value;
                    CooldownHandler cdHandler = cdInstance.handler;
                    if (cdHandler.CanTickDown)
                        --cdInstance.timeLeft;   // 逐帧递减剩余时间
                    cdHandler.Tick();
                    if (cdInstance.timeLeft < 0)
                    {
                        cdHandler.OnCompleted();
                        if (cdHandler.EndSound != null && cdHandler.ShouldPlayEndSound)
                            SoundEngine.PlaySound(cdHandler.EndSound.GetValueOrDefault(), Player.Center);
                        expiredCooldowns.Add(cdID);
                    }
                }
                cdIterator.Dispose();
                foreach (string expiredID in expiredCooldowns)
                    cooldowns.Remove(expiredID);
            }
            RevertCalamityContentNerfs();
            // 蜡烛/塑像增益共存的快照：下一次方块右键时要拿它把被灾厄清掉的同类增益补回
            candleEffigyBuffMask = CandleEffigyCoexistence.BuildMask(Player);
            if(ornateShield)
            {
                Player.dashType = 0;
                Player.lifeRegen += 2;
                Player.statLifeMax2 += 20;
                if(Player.statLife < (int)(Player.statLifeMax2 * 0.25))
                {
                    Player.statDefense += 4;
                }
            }
            if(shieldoftheOcean)
            {
                if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                {
                    Player.statDefense += 5;
                }
            }
            if(asgardsValor)
            {
                Player.dashType = 0;
                Player.noKnockback = true;
                Player.fireWalk = true;
                Player.statLifeMax2 += 50;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Weak] = true;
                Player.buffImmune[BuffID.BrokenArmor] = true;
                Player.buffImmune[BuffID.Bleeding] = true;
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Slow] = true;
                Player.buffImmune[BuffID.Confused] = true;
                Player.buffImmune[BuffID.Silenced] = true;
                Player.buffImmune[BuffID.Cursed] = true;
                Player.buffImmune[BuffID.Darkness] = true;
                Player.buffImmune[BuffID.WindPushed] = true;
                Player.buffImmune[BuffID.Stoned] = true;
                Player.buffImmune[BuffID.Daybreak] = true;
                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.OnFire3] = true;
                // 走 AddCalamityBuffImmune（内部 TryFind + 缓存），对应模组缺该名时静默跳过而不是抛异常
                AddCalamityBuffImmune(Player, "CalamityMod", "SearingLava");
                AddCalamityBuffImmune(Player, "CalamityMod", "HolyFlames");
                AddCalamityBuffImmune(Player, "CalamityMod", "BrimstoneFlames");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "GlacialState");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "HolyLight");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "BrimstoneFlames");
                if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                {
                    Player.endurance += 0.12f;
                }
            }
            if (elysianAegis)
            {
                Player.dashType = 0;
                Player.noKnockback = true;
                Player.fireWalk = true;
                Player.statLifeMax2 += 30;
                Player.lifeRegen += 3;
                Player.buffImmune[BuffID.CursedInferno] = true;
                Player.buffImmune[BuffID.ShadowFlame] = true;
                Player.buffImmune[BuffID.Daybreak] = true;
                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.OnFire3] = true;
                AddCalamityBuffImmune(Player, "CalamityMod", "HolyFlames");
                AddCalamityBuffImmune(Player, "CalamityMod", "BrimstoneFlames");
                AddCalamityBuffImmune(Player, "CalamityMod", "WeakPetrification");
                AddCalamityBuffImmune(Player, "CalamityMod", "Nightwither");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "GlacialState");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "HolyLight");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "BrimstoneFlames");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "WhisperingDeath");
            }
            if(elysianAegispower)
            {
                bool flag14 = false;
                if (elysianGuard)
                {
                    float num29 = shieldInvinc;
                    shieldInvinc -= 0.08f;
                    if (shieldInvinc < 0f)
                    {
                        shieldInvinc = 0f;
                    }
                    else
                    {
                        flag14 = true;
                    }
                    if (shieldInvinc == 0f && num29 != shieldInvinc && Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                    }
                    float damageBoost = (5f - shieldInvinc) * 0.03f;
                    Player.GetDamage<GenericDamageClass>() += damageBoost;
                    int critBoost = (int)((5f - shieldInvinc) * 2f);
                    Player.GetCritChance<GenericDamageClass>() += critBoost;
                    Player.aggro += (int)((5f - shieldInvinc) * 220f);
                    Player.statDefense += (int)((5f - shieldInvinc) * 4f);
                    Player.moveSpeed *= 0.85f;
                    if (Player.mount.Active)
                    {
                        elysianGuard = false;
                    }
                }
                else
                {
                    float num30 = shieldInvinc;
                    shieldInvinc += 0.08f;
                    if (shieldInvinc > 5f)
                    {
                        shieldInvinc = 5f;
                    }
                    else
                    {
                        flag14 = true;
                    }
                    if (shieldInvinc == 5f && num30 != shieldInvinc && Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                    }
                }
                if (flag14)
                {
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 vector = Vector2.UnitY.RotatedByRandom(6.2831854820251465);
                        Dust dust = Main.dust[Dust.NewDust(Player.Center - vector * 30f, 0, 0, DustID.CopperCoin, 0f, 0f, 0, default(Color), 1f)];
                        dust.noGravity = true;
                        dust.position = Player.Center - vector * (float)Main.rand.Next(5, 11);
                        dust.velocity = vector.RotatedBy(1.5707963705062866, default(Vector2)) * 4f;
                        dust.scale = 0.5f + Main.rand.NextFloat();
                        dust.fadeIn = 0.5f;
                    }
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 vector2 = Vector2.UnitY.RotatedByRandom(6.2831854820251465);
                        Dust dust2 = Main.dust[Dust.NewDust(Player.Center - vector2 * 30f, 0, 0, DustID.GoldCoin, 0f, 0f, 0, default(Color), 1f)];
                        dust2.noGravity = true;
                        dust2.position = Player.Center - vector2 * 12f;
                        dust2.velocity = vector2.RotatedBy(-1.5707963705062866, default(Vector2)) * 2f;
                        dust2.scale = 0.5f + Main.rand.NextFloat();
                        dust2.fadeIn = 0.5f;
                    }
                }
            }
            else
            {
                elysianGuard = false;
            }
            // 金之特斯拉套把飞毯贴图覆写为自定义贴图；脱装时还原为原版默认，否则会残留自定义贴图
            if (auricSet)
            {
                TextureAssets.FlyingCarpet = ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/AuricCarpet");
            }
            else
            {
                TextureAssets.FlyingCarpet = Main.Assets.Request<Texture2D>("Images/FlyingCarpet");
            }
            if(asgardianAegis)
            {
                Player.dashType = 0;
                Player.noKnockback = true;
                Player.fireWalk = true;
                Player.statLifeMax2 += 40;
                Player.lifeRegen += 5;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Weak] = true;
                Player.buffImmune[BuffID.BrokenArmor] = true;
                Player.buffImmune[BuffID.Bleeding] = true;
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Slow] = true;
                Player.buffImmune[BuffID.Confused] = true;
                Player.buffImmune[BuffID.Silenced] = true;
                Player.buffImmune[BuffID.Cursed] = true;
                Player.buffImmune[BuffID.Darkness] = true;
                Player.buffImmune[BuffID.WindPushed] = true;
                Player.buffImmune[BuffID.Stoned] = true;
                Player.buffImmune[BuffID.Daybreak] = true;
                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.OnFire3] = true;
                Player.buffImmune[BuffID.CursedInferno] = true;
                Player.buffImmune[BuffID.ShadowFlame] = true;
                Player.buffImmune[BuffID.Venom] = true;
                Player.buffImmune[BuffID.Webbed] = true;
                Player.buffImmune[BuffID.Blackout] = true;
                AddCalamityBuffImmune(Player, "CalamityMod", "HolyFlames");
                AddCalamityBuffImmune(Player, "CalamityMod", "BrimstoneFlames");
                AddCalamityBuffImmune(Player, "CalamityMod", "WeakPetrification");
                AddCalamityBuffImmune(Player, "CalamityMod", "Nightwither");
                AddCalamityBuffImmune(Player, "CalamityMod", "GodSlayerInferno");
                AddCalamityBuffImmune(Player, "CalamityMod", "ArmorCrunch");
                AddCalamityBuffImmune(Player, "CalamityMod", "SulphuricPoisoning");
                AddCalamityBuffImmune(Player, "CalamityMod", "BrainRot");
                AddCalamityBuffImmune(Player, "CalamityMod", "BurningBlood");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "GlacialState");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "HolyLight");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "BrimstoneFlames");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "WhisperingDeath");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "GodSlayerInferno");
                AddCalamityBuffImmune(Player, "CalamityModClassicPreTrailer", "ArmorCrunch");
                if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                {
                    Player.endurance += 0.12f;
                }
                if (Player.statLife < (int)(Player.statLifeMax2 * 0.25))
                {
                    Player.statDefense += 10;
                }
            }
        }
        // ── 还原灾厄内容削弱 ──
        /// <summary>
        /// 回退灾厄内容削弱（RevertCalamityContentNerfs）：撤销元素手套与核生成"无法与下位饰品叠加属性"的限制。
        /// 扫描玩家饰品栏，检测上位饰品与其下位是否同时装备，补回被灾厄 gloveLevel 取最高 / nucleogenesis 覆盖吞掉的属性：
        /// 元素手套补回下位手套攻速（火手套 14% / 机械手套 12% / 力量·狂战士手套 12% / 野性爪 10%），
        /// 核生成补回下位饰品仆从栏（星染发生器 +2 / 静滞诅咒 +3 / 静滞祝福 +2 / 初影焰 +1 / 星爆核心 +1 / 伏特水母 +1 / 带电水母电池 +1）。
        /// 仅在开启配置且加载现代版灾厄时生效。
        /// </summary>
        private void RevertCalamityContentNerfs()
        {
            if (ConfigSystem.Instance?.RevertCalamityContentNerfs != true || !ModLoader.HasMod("CalamityMod"))
                return;
            // 懒加载灾厄饰品类型（首次调用时 TryFind 一次，之后复用）
            if (eGauntletType == -1)
            {
                eGauntletType = ModContent.TryFind("CalamityMod", "ElementalGauntlet", out ModItem eg) ? eg.Type : 0;
                nucleogenesisType = ModContent.TryFind("CalamityMod", "Nucleogenesis", out ModItem nu) ? nu.Type : 0;
                starTaintedGeneratorType = ModContent.TryFind("CalamityMod", "StarTaintedGenerator", out ModItem st) ? st.Type : 0;
                statisCurseType = ModContent.TryFind("CalamityMod", "StatisCurse", out ModItem sc) ? sc.Type : 0;
                statisBlessingType = ModContent.TryFind("CalamityMod", "StatisBlessing", out ModItem sb) ? sb.Type : 0;
                theFirstShadowflameType = ModContent.TryFind("CalamityMod", "TheFirstShadowflame", out ModItem sf) ? sf.Type : 0;
                starbusterCoreType = ModContent.TryFind("CalamityMod", "StarbusterCore", out ModItem sbc) ? sbc.Type : 0;
                voltaicJellyType = ModContent.TryFind("CalamityMod", "VoltaicJelly", out ModItem vj) ? vj.Type : 0;
                jellyChargedBatteryType = ModContent.TryFind("CalamityMod", "JellyChargedBattery", out ModItem jcb) ? jcb.Type : 0;
            }
            bool hasEGauntlet = false, hasNucleo = false, hasStarTainted = false, hasStatisCurse = false;
            bool hasStatisBlessing = false, hasFirstShadowflame = false, hasStarbusterCore = false, hasVoltaicJelly = false, hasJellyBattery = false;
            bool hasFireGauntlet = false, hasMechanicalGlove = false, hasPowerGlove = false, hasFeralClaws = false;
            for (int i = 3; i < 8 + Player.extraAccessorySlots; i++)
            {
                int type = Player.armor[i].type;
                if (type == eGauntletType) hasEGauntlet = true;
                else if (type == nucleogenesisType) hasNucleo = true;
                else if (type == starTaintedGeneratorType) hasStarTainted = true;
                else if (type == statisCurseType) hasStatisCurse = true;
                else if (type == statisBlessingType) hasStatisBlessing = true;
                else if (type == theFirstShadowflameType) hasFirstShadowflame = true;
                else if (type == starbusterCoreType) hasStarbusterCore = true;
                else if (type == voltaicJellyType) hasVoltaicJelly = true;
                else if (type == jellyChargedBatteryType) hasJellyBattery = true;
                else if (type == ItemID.FireGauntlet) hasFireGauntlet = true;
                else if (type == ItemID.MechanicalGlove) hasMechanicalGlove = true;
                else if (type == ItemID.PowerGlove || type == ItemID.BerserkerGlove) hasPowerGlove = true;
                else if (type == ItemID.FeralClaws) hasFeralClaws = true;
            }
            // 元素手套：补回下位手套被 gloveLevel 取最高覆盖的攻速
            if (hasEGauntlet)
            {
                if (hasFireGauntlet) Player.GetAttackSpeed<MeleeDamageClass>() += 0.14f;
                if (hasMechanicalGlove) Player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
                if (hasPowerGlove) Player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
                if (hasFeralClaws) Player.GetAttackSpeed<MeleeDamageClass>() += 0.10f;
            }
            // 核生成：补回下位饰品被 nucleogenesis 覆盖的仆从栏
            if (hasNucleo)
            {
                if (hasStarTainted) Player.maxMinions += 2;
                if (hasStatisCurse) Player.maxMinions += 3;   // shadowMinions +1 + statisMinions +2
                if (hasStatisBlessing) Player.maxMinions += 2;
                if (hasFirstShadowflame) Player.maxMinions += 1;
                if (hasStarbusterCore) Player.maxMinions += 1;
                if (hasVoltaicJelly) Player.maxMinions += 1;
                if (hasJellyBattery) Player.maxMinions += 1;
            }
        }
        public override void ModifyWeaponKnockback(Item item, ref StatModifier knockback)
        {
            if (auricBoost)
            {
                knockback *= 1f + (1f - modStealth) * 0.5f;
            }
        }
        /// <summary>
        /// tModLoader 的 PostUpdateBuffs 钩子：每帧在增益(buff)结算完成后调用。
        /// 处于女巫套装免死无敌窗口（silvaCountdown &gt; 0 且 hasSilvaEffect 且 silvaSet）时，
        /// 把 Player.lifeRegen 的负值钳为 0，避免无敌期间被 debuff 的负生命回复扣血。
        /// 与 PostUpdateEquips / UpdateBadLifeRegen 的判定互为冗余，覆盖不同结算时机。
        /// </summary>
        public override void PostUpdateBuffs()
        {
            if (silvaCountdown > 0 && hasSilvaEffect && silvaSet)
            {
                if (Player.lifeRegen < 0)
                    Player.lifeRegen = 0;
            }
        }
        /// <summary>
        /// tModLoader 的 PostUpdateEquips 钩子：每帧在装备属性结算完成后调用。
        /// 处理内容与 PostUpdateBuffs 相同——女巫套装免死无敌窗口内把负的 Player.lifeRegen 钳为 0，
        /// 防止无敌期被负生命回复扣血；两者挂钩不同结算阶段，起兜底作用。
        /// </summary>
        public override void PostUpdateEquips()
        {
            // ── 永久增益消耗品（糖心柑橘 / 有机豆荚 / 新鲜蓝莓 / 熔岩浆果）──
            // 一次性解锁、标志随存档持久化（见 SaveData/LoadData），加成每帧按标志叠加。
            // 必须放 PostUpdateEquips 而不是 ResetEffects：ResetEffects 的职责是把这些属性清零，加成要在它之后加
            if (sugarheartCitrus)
            {
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.04f;
            }
            if (organicPod)
            {
                Player.endurance += 0.04f;
            }
            if (freshBlueberry)
            {
                Player.GetArmorPenetration<GenericDamageClass>() += 4;
            }
            if (moltenMagmaFruit)
            {
                Player.GetDamage<GenericDamageClass>() += 0.04f;
                Player.GetCritChance<GenericDamageClass>() += 4;
                Player.GetArmorPenetration<GenericDamageClass>() += 4;
            }
            if (silvaCountdown > 0 && hasSilvaEffect && silvaSet)
            {
                if (Player.lifeRegen < 0)
                    Player.lifeRegen = 0;
            }
            if (auricBoost)
            {
                if (Player.itemAnimation > 0)
                {
                    modStealthTimer = 5;
                }
                if ((double)Math.Abs(Player.velocity.X) < 0.1 && (double)Math.Abs(Player.velocity.Y) < 0.1 && !Player.mount.Active)
                {
                    if (modStealthTimer == 0 && modStealth > 0f)
                    {
                        modStealth -= 0.015f;
                        if ((double)modStealth <= 0.0)
                        {
                            modStealth = 0f;
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                }
                else
                {
                    float num27 = Math.Abs(Player.velocity.X) + Math.Abs(Player.velocity.Y);
                    modStealth += num27 * 0.0075f;
                    if (modStealth > 1f)
                        modStealth = 1f;
                    if (Player.mount.Active)
                        modStealth = 1f;
                }
                float damageBoost = (1f - modStealth) * 0.2f;
                Player.GetDamage<GenericDamageClass>() += damageBoost;
                int critBoost = (int)((1f - modStealth) * 10f);
                Player.GetCritChance<GenericDamageClass>() += critBoost;
                if (modStealthTimer > 0)
                    modStealthTimer--;
            }
            else if (psychoticAmulet)
            {
                if (Player.itemAnimation > 0)
                {
                    modStealthTimer = 5;
                }
                if ((double)Math.Abs(Player.velocity.X) < 0.1 && (double)Math.Abs(Player.velocity.Y) < 0.1 && !Player.mount.Active)
                {
                    if (modStealthTimer == 0 && modStealth > 0f)
                    {
                        modStealth -= 0.015f;
                        if ((double)modStealth <= 0.0)
                        {
                            modStealth = 0f;
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                }
                else
                {
                    float num27 = Math.Abs(Player.velocity.X) + Math.Abs(Player.velocity.Y);
                    modStealth += num27 * 0.0075f;
                    if (modStealth > 1f)
                        modStealth = 1f;
                    if (Player.mount.Active)
                        modStealth = 1f;
                }
                Player.GetDamage<RangedDamageClass>() += (1f - modStealth) * 0.2f;
                Player.GetCritChance<RangedDamageClass>() += (int)((1f - modStealth) * 10f);
                Player.aggro -= (int)((1f - modStealth) * 750f);
                if (modStealthTimer > 0)
                    modStealthTimer--;
            }
            else
                modStealth = 1f;
        }
        /// <summary>
        /// tModLoader 的 UpdateLifeRegen 钩子：每帧在生命回复结算前调用，可直接改 Player.lifeRegen 与 lifeRegenTime。
        /// 影之再生（shadeRegen，由恶魔之影胸甲置位）：玩家完全静止且无使用动画时快速累积回复量，
        /// 速率取决于 <see cref="AnyBossNPCS"/>（有 Boss 存活时回血更慢：时间上限 900、每帧 +2、封顶 16；
        /// 无 Boss 时：上限 3600、每帧 +8、封顶 60），并按 lifeRegenTime 概率生成 ShadowbeamStaff 尘埃。
        /// 塔拉生命回复（tarraLifeRegen）则简单叠加 +10 生命回复。
        /// </summary>
        public override void UpdateLifeRegen()
        {
            bool areThereAnyDamnBosses = AnyBossNPCS();
            int lifeRegenTimeMaxBoost = (areThereAnyDamnBosses ? 900 : 3600);
            int lifeRegenMaxBoost = (areThereAnyDamnBosses ? 2 : 8);
            float lifeRegenLifeRegenTimeMaxBoost = (areThereAnyDamnBosses ? 16 : 60);
            // 解除饰品闪亮石的限制
            if (shadeRegen && (double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
            {
                if (Player.lifeRegenTime > 90 && Player.lifeRegenTime < lifeRegenTimeMaxBoost)
                {
                    Player.lifeRegenTime = lifeRegenTimeMaxBoost;
                }
                Player.lifeRegenTime += lifeRegenMaxBoost;
                Player.lifeRegen += lifeRegenMaxBoost;
                float num3 = (float)((double)Player.lifeRegenTime * 2.5); //lifeRegenTime max is 3600
                num3 /= 300f;
                if (num3 > 0f)
                {
                    if (num3 > lifeRegenLifeRegenTimeMaxBoost)
                    {
                        num3 = lifeRegenLifeRegenTimeMaxBoost;
                    }
                    Player.lifeRegen += (int)num3;
                }
                if (Player.lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
                {
                    Player.lifeRegenCount++;
                    if ((Main.rand.Next(30000) < Player.lifeRegenTime || Main.rand.Next(30) == 0))
                    {
                        int num5 = Dust.NewDust(Player.position, Player.width, Player.height, DustID.ShadowbeamStaff, 0f, 0f, 200, default(Color), 1f);
                        Main.dust[num5].noGravity = true;
                        Main.dust[num5].velocity *= 0.75f;
                        Main.dust[num5].fadeIn = 1.3f;
                        Vector2 vector = new((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        vector.Normalize();
                        vector *= (float)Main.rand.Next(50, 100) * 0.04f;
                        Main.dust[num5].velocity = vector;
                        vector.Normalize();
                        vector *= 34f;
                        Main.dust[num5].position = Player.Center - vector;
                    }
                }
            }
            // （古）链：蜂蜜系站桩加速回复。照抄灾厄 2.0.3.9 站桩区里 honeyTurboRegen 那一档
            // （需泡在蜂蜜里 honeyWet + 静止；阈值 90 帧、turbo 档位 3、尘用 Honey2、负回复先减半）。
            // 注：本方法上面 shadeRegen / photosynthesis 两段当年移植时改用了工程自定的 boss 相关缩放，
            // 这一段按源原样写，不与它们共用那套缩放
            if (honeyTurboRegen && Player.honeyWet && (double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
            {
                if (Player.lifeRegen < 0)
                {
                    Player.lifeRegen /= 2;
                }
                if (Player.lifeRegenTime > 90 && Player.lifeRegenTime < 1800)
                {
                    Player.lifeRegenTime = 1800;
                }
                Player.lifeRegen += 3;
                Player.lifeRegenTime += 3;
                if (Player.lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
                {
                    Player.lifeRegenCount++;
                    if (Main.rand.Next(30000) < Player.lifeRegenTime || Main.rand.Next(30) == 0)
                    {
                        int honeyDust = Dust.NewDust(Player.position, Player.width, Player.height, DustID.Honey2, 0f, 0f, 200, default(Color), 1f);
                        Main.dust[honeyDust].noGravity = true;
                        Main.dust[honeyDust].velocity *= 0.75f;
                        Main.dust[honeyDust].fadeIn = 1.3f;
                        Vector2 honeyVec = new((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        honeyVec.Normalize();
                        honeyVec *= (float)Main.rand.Next(50, 100) * 0.04f;
                        Main.dust[honeyDust].velocity = honeyVec;
                        honeyVec.Normalize();
                        honeyVec *= 34f;
                        Main.dust[honeyDust].position = Player.Center - honeyVec;
                    }
                }
            }
            // （古）链：甘露安瓿（古）的站桩加速回复。源里与蜂蜜同一段，同样阈值 90 帧、档位 3；
            // 差别只在尘：228 号、透明度 80、缩放 0.5、抛得更远（55），且每 1/4 概率就撒（比蜂蜜密）
            if (aAmpoule && (double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
            {
                if (Player.lifeRegen < 0)
                {
                    Player.lifeRegen /= 2;
                }
                if (Player.lifeRegenTime > 90 && Player.lifeRegenTime < 1800)
                {
                    Player.lifeRegenTime = 1800;
                }
                Player.lifeRegen += 3;
                Player.lifeRegenTime += 3;
                if (Player.lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
                {
                    Player.lifeRegenCount++;
                    if (Main.rand.Next(30000) < Player.lifeRegenTime || Main.rand.NextBool(4))
                    {
                        int ampouleDust = Dust.NewDust(Player.position, Player.width, Player.height, DustID.GoldFlame, 0f, 0f, 80, default(Color), 0.5f);
                        Main.dust[ampouleDust].noGravity = true;
                        Main.dust[ampouleDust].fadeIn = 1.3f;
                        Vector2 ampouleVec = new((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        ampouleVec.Normalize();
                        ampouleVec *= (float)Main.rand.Next(50, 100) * 0.04f;
                        Main.dust[ampouleDust].velocity = ampouleVec;
                        ampouleVec.Normalize();
                        ampouleVec *= 55f;
                        Main.dust[ampouleDust].position = Player.Center - ampouleVec;
                    }
                }
            }
            // （古）链：无暇粹魂晶的站桩加速回复。源里与蜂蜜同一段，但阈值更短（60 帧）、档位最高（4）；
            // 尘用 187 号、透明度 80、缩放 0.5、抛距 55，且**必定撒**（源里 purity 时直接走 NextBool()）
            if (purity && (double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
            {
                if (Player.lifeRegen < 0)
                {
                    Player.lifeRegen /= 2;
                }
                if (Player.lifeRegenTime > 60 && Player.lifeRegenTime < 1800)
                {
                    Player.lifeRegenTime = 1800;
                }
                Player.lifeRegen += 4;
                Player.lifeRegenTime += 4;
                if (Player.lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
                {
                    Player.lifeRegenCount++;
                    if (Main.rand.Next(30000) < Player.lifeRegenTime || Main.rand.NextBool())
                    {
                        int purityDust = Dust.NewDust(Player.position, Player.width, Player.height, DustID.BlueFlare, 0f, 0f, 80, default(Color), 0.5f);
                        Main.dust[purityDust].noGravity = true;
                        Main.dust[purityDust].fadeIn = 1.3f;
                        Vector2 purityVec = new((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        purityVec.Normalize();
                        purityVec *= (float)Main.rand.Next(50, 100) * 0.04f;
                        Main.dust[purityDust].velocity = purityVec;
                        purityVec.Normalize();
                        purityVec *= 55f;
                        Main.dust[purityDust].position = Player.Center - purityVec;
                    }
                }
            }
            if(photosynthesis && (double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
            {
                int lifeRegenTimeMaxBoost2 = Main.dayTime ? lifeRegenTimeMaxBoost : (lifeRegenTimeMaxBoost / 5);
                int lifeRegenMaxBoost2 = Main.dayTime ? lifeRegenMaxBoost : (lifeRegenMaxBoost / 5);
                float lifeRegenLifeRegenTimeMaxBoost2 = Main.dayTime ? lifeRegenLifeRegenTimeMaxBoost : (lifeRegenLifeRegenTimeMaxBoost / 5);
                if (Player.lifeRegenTime > 90 && Player.lifeRegenTime < lifeRegenTimeMaxBoost2)
                {
                    Player.lifeRegenTime = lifeRegenTimeMaxBoost2;
                }
                Player.lifeRegenTime += lifeRegenMaxBoost2;
                Player.lifeRegen += lifeRegenMaxBoost2;
                float num3 = (float)((double)Player.lifeRegenTime * 2.5); //lifeRegenTime max is 3600
                num3 /= 300f;
                if (num3 > 0f)
                {
                    if (num3 > lifeRegenLifeRegenTimeMaxBoost2)
                    {
                        num3 = lifeRegenLifeRegenTimeMaxBoost2;
                    }
                    Player.lifeRegen += (int)num3;
                }
                if (Player.lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
                {
                    Player.lifeRegenCount++;
                    if ((Main.rand.Next(30000) < Player.lifeRegenTime || Main.rand.NextBool(2)))
                    {
                        int num5 = Dust.NewDust(Player.position, Player.width, Player.height, DustID.CopperCoin, 0f, 0f, 200, default(Color), 1f);
                        Main.dust[num5].noGravity = true;
                        Main.dust[num5].velocity *= 0.75f;
                        Main.dust[num5].fadeIn = 1.3f;
                        Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        vector.Normalize();
                        vector *= (float)Main.rand.Next(50, 100) * 0.04f;
                        Main.dust[num5].velocity = vector;
                        vector.Normalize();
                        vector *= 34f;
                        Main.dust[num5].position = Player.Center - vector;
                    }
                }
            }
            if (tarraLifeRegen)
            {
                Player.lifeRegen += 10;
            }
            // 欧米茄蓝胸甲：禁止一切正面生命再生（同上，放在本钩子末尾以覆盖本方法内先加上的各项）
            OmegaBlueNoLifeRegen();
        }
        /// <summary>
        /// 欧米茄蓝胸甲（omegaBlueChestplate）的"禁止正面生命再生"：
        /// 把正的 lifeRegen 归零、并清空 lifeRegenTime 与 lifeRegenCount
        /// （与灾厄两版逐字同款：经典版 CalamityPlayerPreTrailer 的 LastDebuffs 段、
        /// 现代版 CalamityPlayerLifeRegen 的 noLifeRegen 段）。
        /// 同时挂在 UpdateLifeRegen 与 UpdateBadLifeRegen 两处，因为本工程观察到
        /// UpdateBadLifeRegen 只在生命回复为负时被调用，而本条要在回复为正时也生效。
        /// <b>数值膨胀开关开启时本限制失效</b>（用户 2026-10-01 指定），故开头直接返回。
        /// </summary>
        private void OmegaBlueNoLifeRegen()
        {
            if (!omegaBlueChestplate)
                return;
            // 数值膨胀开启时解禁：欧米茄蓝胸甲不再禁止正面生命再生（用户 2026-10-01 指定）
            if (ConfigSystem.StatInflationEnabled)
                return;
            if (Player.lifeRegen > 0)
            {
                Player.lifeRegen = 0;
            }
            Player.lifeRegenTime = 0;
            if (Player.lifeRegenCount > 0)
            {
                Player.lifeRegenCount = 0;
            }
        }
        /// <summary>
        /// tModLoader 的 UpdateBadLifeRegen 钩子：仅在生命回复为负时被调用。
        /// 在女巫套装免死无敌窗口（silvaCountdown &gt; 0 且 hasSilvaEffect 且 silvaSet）内，
        /// 把负的 Player.lifeRegen 钳回 0，抵消 debuff 造成的持续掉血；
        /// 与 PostUpdateBuffs / PostUpdateEquips 的同一判定重复，用于覆盖各结算路径。
            /// 地狱火爆炸（hellfireExplosion）会把回复计时清零后按原值扣 lifeRegen。
        /// 增强版暗影焰（本工程移植的 Shadowflame，禅心剑 PvP 命中挂上）按灾厄原值扣 30。
        /// 末尾再调一次 <see cref="OmegaBlueNoLifeRegen"/>，与 UpdateLifeRegen 里那次互为保险
        /// （灾厄两版把欧米茄蓝禁回血写在本钩子里，但本工程观察到本钩子只在回复为负时触发）。
        /// </summary>
        public override void UpdateBadLifeRegen()
        {
            if (silvaCountdown > 0 && hasSilvaEffect && silvaSet)
            {
                if (Player.lifeRegen < 0)
                    Player.lifeRegen = 0;
            }
            if (hellfireExplosion)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= 120;
            }
            // 增强版暗影焰：灾厄原码 ApplyDoTDebuff(shadowflame, 30)，即 lifeRegen -= 30（= 15 HP/s）
            if (shadowflame)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= 30;
            }
            // ── （古）链：蜂蜜式回血 + 减益时长减半 ──
            // 照抄灾厄 2.0.3.9 CalamityPlayerLifeRegen 的「Life Regen That Works Even During DoT Debuffs」区。
            // 与上面的旧件分支（honeyDew / livingDew）完全独立：那两件走的是另一套口径，不要合并
            if (alwaysHoneyRegen)
            {
                // 原版蜂蜜规则的等价物，但真的泡在蜂蜜里时不叠加（灾厄原码同样有这个判定）
                if (!Player.honey)
                {
                    Player.lifeRegen += 2;
                    Player.lifeRegenTime += 1;
                    // 负回复时再补 2，但不会把总回复推成正值
                    if (Player.lifeRegen < 0)
                    {
                        Player.lifeRegen += 2;
                        if (Player.lifeRegen > 0)
                        {
                            Player.lifeRegen = 0;
                        }
                    }
                }
            }
            if (honeyDewHalveDebuffs)
            {
                // 逐帧给命中列表的减益多扣 1 点时长 → 实际到期速度翻倍。
                // 覆盖范围逐级扩大：病症/中毒类（蜜露）→ 火系/燃烧类（活露）→ 整张 debuffList（无暇粹魂晶，后续批次接入）
                for (int l = 0; l < Player.MaxBuffs; ++l)
                {
                    if (Player.buffTime[l] <= 2)
                    {
                        continue;
                    }
                    int buffID = Player.buffType[l];
                    bool shouldHalveDuration = CalamityDemutation.sicknessDebuffList.Contains(buffID);
                    if (livingDewHalveDebuffs)
                    {
                        shouldHalveDuration |= CalamityDemutation.fireDebuffList.Contains(buffID);
                    }
                    if (purity)
                    {
                        shouldHalveDuration |= CalamityDemutation.debuffList.Contains(buffID);
                    }
                    if (shouldHalveDuration)
                    {
                        --Player.buffTime[l];
                    }
                }
            }
            // ── （古）链：按「缺失生命比例」给再生 ──
            // 照抄灾厄 2.0.3.9 CalamityPlayerLifeRegen 的 L420-427。
            // ⚠️ 刻意偏离：源那行是 `(statLifeMax2 - statLife) / statLifeMax2`——两侧都是 int，
            // 整数除法会让 missingLifeRatio 恒为 0，缩放整个失效（永远取下限）。上游 2.0.7.2 已给分母补了
            // `(float)` 转换修掉，本工程照修（与本工程「源自身缺陷就修掉并注明」的既有做法一致）
            if (rOoze || aAmpoule || purity)
            {
                float missingLifeRatio = (Player.statLifeMax2 - Player.statLife) / (float)Player.statLifeMax2;
                float lifeRegenToGive = MathHelper.Lerp(purity ? 6f : 4f, purity ? 14f : 12f, missingLifeRatio);
                Player.lifeRegen += (int)lifeRegenToGive;
            }
            // ── （古）链：无暇粹魂晶对持续伤害减益的「免疫」（本工程自实现） ──
            // 灾厄在 CalamityPlayerLifeRegen 里用 `ApplyDoTDebuff(减益, 扣量, immuneCondition)` 的第三个参数
            // 一次性免疫掉一整张清单：辐照 / 硫磺中毒 / 激流 / 燃烧之血 / 脑腐 / 元素混合 / 汽化 / 硫磺火 /
            // 暗夜业火 / 神圣火焰 / 深海窒息 / 圣焰 / 暗影焰 / 星辉感染。
            // 本工程没有那种集中式入口——那些 DoT 是灾厄自己算的，我们拦不到单条，
            // 故改为「身上带有 debuffList 内的减益时，把负回复按固定值抵消」，等效于大幅削弱持续伤害。
            // ⚠️ 近似处理：源里各条的扣量并不相同（4~50 不等，少数还带上下文条件），这里统一按一个常量抵消
            if (purity && Player.lifeRegen < 0 && Player.buffType.Any(CalamityDemutation.debuffList.Contains))
            {
                Player.lifeRegen += PurityDoTOffset;
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
            }
            // ── （古）链：宝石系三件（王冠宝石 / 感染宝石 / 无暇粹魂晶）──
            // 照抄灾厄 2.0.3.9 CalamityPlayerLifeRegen L477-518 的 **else-if 互斥链**，
            // 顺序即优先级：无暇粹魂晶 > 感染宝石 > 王冠宝石（purity 那一支在 Purity 落地时补到最前面）
            if (purity)
            {
                int intendedPurityDefense = 0;
                int currentDebuffs = Player.buffType.Count(CalamityDemutation.debuffList.Contains);
                if (currentDebuffs > 0)
                {
                    // 直接回血：正常每 12 帧回 1 点；带减益累计超过 300 帧后节拍逐步拉长
                    //（每超出 15 帧多 1 帧节拍，上限 180 帧 —— 即最慢 3 秒才回 1 血）
                    int healFrameCadence = 12;
                    int punishmentFrames = purityHealSlowdownFrames - 300;
                    if (healFrameCadence < 180)
                    {
                        healFrameCadence += (punishmentFrames < 0) ? 0 : punishmentFrames / 15;
                    }
                    if (Player.miscCounter % healFrameCadence == healFrameCadence - 1)
                    {
                        Player.Heal(1);
                    }
                    if (Player.lifeRegenTime < 1800)
                    {
                        Player.lifeRegenTime = 1800;
                    }
                    // 动态防御：首个减益给 20 点，之后每多一种 +8
                    intendedPurityDefense = 20 + (currentDebuffs - 1) * 8;
                    if (jewelBonusDefense < intendedPurityDefense)
                    {
                        jewelBonusDefense = intendedPurityDefense;
                    }
                    ++purityHealSlowdownFrames;
                }
                // 减益消退后动态防御每秒回落 1 点
                if (Player.miscCounter % 60 == 0 && jewelBonusDefense > intendedPurityDefense)
                {
                    --jewelBonusDefense;
                }
                // 完全没减益时，累积的节拍惩罚逐帧回落
                if (currentDebuffs <= 0)
                {
                    --purityHealSlowdownFrames;
                    if (purityHealSlowdownFrames < 0)
                    {
                        purityHealSlowdownFrames = 0;
                    }
                }
                Player.statDefense += jewelBonusDefense;
            }
            else if (infectedJewel)
            {
                Player.lifeRegen += 2;
                int intendedJewelDefense = 0;
                int currentDebuffs = Player.buffType.Count(CalamityDemutation.debuffList.Contains);
                if (currentDebuffs > 0)
                {
                    Player.lifeRegen += 4;
                    if (Player.lifeRegenTime < 1800)
                    {
                        Player.lifeRegenTime = 1800;
                    }
                    intendedJewelDefense = 16 + (currentDebuffs - 1) * 5;
                    if (jewelBonusDefense < intendedJewelDefense)
                    {
                        jewelBonusDefense = intendedJewelDefense;
                    }
                }
                // 减益消退后动态防御每秒回落 1 点（源用 miscCounter % 60 做秒表）
                if (Player.miscCounter % 60 == 0 && jewelBonusDefense > intendedJewelDefense)
                {
                    --jewelBonusDefense;
                }
                Player.statDefense += jewelBonusDefense;
            }
            else if (crownJewel)
            {
                Player.lifeRegen += 2;
                // 源这里用 Any（其余两支用 Count），照抄
                if (Player.buffType.Any(CalamityDemutation.debuffList.Contains))
                {
                    Player.lifeRegen += 3;
                    if (Player.lifeRegenTime < 1800)
                    {
                        Player.lifeRegenTime = 1800;
                    }
                }
            }
            OmegaBlueNoLifeRegen();
        }
        /// <summary>
        /// tModLoader 的 CanConsumeAmmo 钩子：判定本次射击是否消耗弹药。
        /// 代达罗斯纹章 1/5（20%）、元素箭袋 40%、魔影羽笠（射手头部件）50% 概率不消耗弹药。
        /// </summary>
        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            if (demonshadeClass == DamageClass.Ranged && weapon.DamageType == DamageClass.Ranged && Main.rand.NextFloat() < 0.5f)
            {
                return false;
            }
            if(daedalusEmblem && weapon.DamageType == DamageClass.Ranged && Main.rand.NextBool(5))
            {
                return false;
            }
            if (elementalQuiver && weapon.DamageType == DamageClass.Ranged && Main.rand.NextFloat() < 0.2f)
            {
                return false;
            }
            if(omegaBlueChestplate && weapon.DamageType == DamageClass.Ranged && Main.rand.NextFloat() < 0.25f)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// tModLoader 的 DrawEffects 钩子：玩家绘制时调用，通过修改颜色分量 r/g/b/a 与 fullBright 影响外观。
        /// 恶魔之影套装（demonshadeSetBonus）反射读取灾厄的 rogueStealth / rogueStealthMax，
        /// 按潜行比例把角色染成半透明兰紫色（潜行越高越透明）；潜行耗尽或城镇 NPC 过多时转为暗红色并全亮，
        /// 移动时另生成 Shadowflame 尘埃。塔拉生命回复（tarraLifeRegen）则生成 Terra 绿色尘埃，
        /// 并在无潜行值时把角色整体染绿并全亮。现代版与经典版灾厄各处理一次。
        /// </summary>
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (shadowflame)
                Shadowflame.DrawEffects(drawInfo);
            if (demonshadeSetBonus)
            {
                if (((double)Math.Abs(Player.velocity.X) > 0.05 || (double)Math.Abs(Player.velocity.Y) > 0.05) && !Player.mount.Active)
                {
                    if (Main.rand.NextBool(2) && drawInfo.shadow == 0f)
                    {
                        int dust = Dust.NewDust(drawInfo.Position - new Vector2(2f, 2f), Player.width + 4, Player.height + 4, DustID.Shadowflame, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 100, default(Color), 1.5f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 0.5f;
                    }
                    r *= 0.15f;
                    g *= 0.025f;
                    b *= 0.1f;
                    fullBright = true;
                }
                /*
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                {
                    var calamityPlayerType = calamity.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                    if (calamityPlayerType != null)
                    {
                        var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)
                            ?.MakeGenericMethod(calamityPlayerType);
                        if (getModPlayerMethod != null)
                        {
                            if (getModPlayerMethod.Invoke(Player, null) is ModPlayer calPlayer)
                            {
                                var field1 = calamityPlayerType.GetField("rogueStealth",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                var field2 = calamityPlayerType.GetField("rogueStealthMax",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                if (field1 != null && field2 != null)
                                {
                                    float rogueStealth = (float)field1.GetValue(calPlayer);
                                    float rogueStealthMax = (float)field2.GetValue(calPlayer);
                                    bool noRogueStealth = rogueStealth == 0f || Player.townNPCs > 2f;
                                    if (rogueStealth > 0f && rogueStealthMax > 0f && Player.townNPCs < 3f)
                                    {
                                        //A translucent orchid color, the rogue class color
                                        float colorValue = (rogueStealth / rogueStealthMax) * 0.9f; //0 to 0.9
                                        r *= 1f - (colorValue * 0.89f); //255 to 50
                                        g *= 1f - colorValue; //255 to 25
                                        b *= 1f - (colorValue * 0.89f); //255 to 50
                                        a *= 1f - colorValue; //255 to 25
                                        Player.armorEffectDrawOutlines = false;
                                        Player.armorEffectDrawShadow = false;
                                        Player.armorEffectDrawShadowSubtle = false;
                                    }
                                    if (((double)Math.Abs(Player.velocity.X) > 0.05 || (double)Math.Abs(Player.velocity.Y) > 0.05) && !Player.mount.Active)
                                    {
                                        if (Main.rand.NextBool(2) && drawInfo.shadow == 0f)
                                        {
                                            int dust = Dust.NewDust(drawInfo.Position - new Vector2(2f, 2f), Player.width + 4, Player.height + 4, DustID.Shadowflame, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 100, default(Color), 1.5f);
                                            Main.dust[dust].noGravity = true;
                                            Main.dust[dust].velocity *= 0.5f;
                                        }
                                        if (noRogueStealth)
                                        {
                                            r *= 0.15f;
                                            g *= 0.025f;
                                            b *= 0.1f;
                                            fullBright = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
                {
                    var calamityPlayerType = classic.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
                    if (calamityPlayerType != null)
                    {
                        var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)
                            ?.MakeGenericMethod(calamityPlayerType);
                        if (getModPlayerMethod != null)
                        {
                            if (getModPlayerMethod.Invoke(Player, null) is ModPlayer calPlayer)
                            {
                                var field1 = calamityPlayerType.GetField("rogueStealth",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                var field2 = calamityPlayerType.GetField("rogueStealthMax",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                if (field1 != null && field2 != null)
                                {
                                    float rogueStealth = (float)field1.GetValue(calPlayer);
                                    float rogueStealthMax = (float)field2.GetValue(calPlayer);
                                    bool noRogueStealth = rogueStealth == 0f || Player.townNPCs > 2f;
                                    if (rogueStealth > 0f && rogueStealthMax > 0f && Player.townNPCs < 3f)
                                    {
                                        //A translucent orchid color, the rogue class color
                                        float colorValue = (rogueStealth / rogueStealthMax) * 0.9f; //0 to 0.9
                                        r *= 1f - (colorValue * 0.89f); //255 to 50
                                        g *= 1f - colorValue; //255 to 25
                                        b *= 1f - (colorValue * 0.89f); //255 to 50
                                        a *= 1f - colorValue; //255 to 25
                                        Player.armorEffectDrawOutlines = false;
                                        Player.armorEffectDrawShadow = false;
                                        Player.armorEffectDrawShadowSubtle = false;
                                    }
                                    if (((double)Math.Abs(Player.velocity.X) > 0.05 || (double)Math.Abs(Player.velocity.Y) > 0.05) && !Player.mount.Active)
                                    {
                                        if (Main.rand.NextBool(2) && drawInfo.shadow == 0f)
                                        {
                                            int dust = Dust.NewDust(drawInfo.Position - new Vector2(2f, 2f), Player.width + 4, Player.height + 4, DustID.Shadowflame, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 100, default(Color), 1.5f);
                                            Main.dust[dust].noGravity = true;
                                            Main.dust[dust].velocity *= 0.5f;
                                        }
                                        if (noRogueStealth)
                                        {
                                            r *= 0.15f;
                                            g *= 0.025f;
                                            b *= 0.1f;
                                            fullBright = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                */
            }
            if(tarraLifeRegen)
            {
                if (Main.rand.NextBool(10) && drawInfo.shadow == 0f)
                {
                    int dust = Dust.NewDust(drawInfo.Position - new Vector2(2f, 2f), Player.width + 4, Player.height + 4, DustID.Terra, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 100, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.75f;
                    Main.dust[dust].velocity.Y -= 0.35f;
                }
                r *= 0.025f;
                g *= 0.15f;
                b *= 0.035f;
                fullBright = true;
                /*
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                {
                    var calamityPlayerType = calamity.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                    if (calamityPlayerType != null)
                    {
                        var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)
                            ?.MakeGenericMethod(calamityPlayerType);
                        if (getModPlayerMethod != null)
                        {
                            if (getModPlayerMethod.Invoke(Player, null) is ModPlayer calPlayer)
                            {
                                var field1 = calamityPlayerType.GetField("rogueStealth",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                var field2 = calamityPlayerType.GetField("rogueStealthMax",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                if (field1 != null && field2 != null)
                                {
                                    float rogueStealth = (float)field1.GetValue(calPlayer);
                                    float rogueStealthMax = (float)field2.GetValue(calPlayer);
                                    bool noRogueStealth = rogueStealth == 0f || Player.townNPCs > 2f;
                                    if (noRogueStealth)
                                    {
                                        r *= 0.025f;
                                        g *= 0.15f;
                                        b *= 0.035f;
                                        fullBright = true;
                                    }
                                }
                            }
                        }
                    }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
                {
                    var calamityPlayerType = classic.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
                    if (calamityPlayerType != null)
                    {
                        var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)
                            ?.MakeGenericMethod(calamityPlayerType);
                        if (getModPlayerMethod != null)
                        {
                            if (getModPlayerMethod.Invoke(Player, null) is ModPlayer calPlayer)
                            {
                                var field1 = calamityPlayerType.GetField("rogueStealth",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                var field2 = calamityPlayerType.GetField("rogueStealthMax",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                if (field1 != null && field2 != null)
                                {
                                    float rogueStealth = (float)field1.GetValue(calPlayer);
                                    float rogueStealthMax = (float)field2.GetValue(calPlayer);
                                    bool noRogueStealth = rogueStealth == 0f || Player.townNPCs > 2f;
                                    if (noRogueStealth)
                                    {
                                        r *= 0.025f;
                                        g *= 0.15f;
                                        b *= 0.035f;
                                        fullBright = true;
                                    }
                                }
                            }
                        }
                    }
                }
                */
            }
            if(auricSet)
            {
                if (((double)Math.Abs(Player.velocity.X) > 0.05 || (double)Math.Abs(Player.velocity.Y) > 0.05) && !Player.mount.Active)
                {
                    if (drawInfo.shadow == 0f)
                    {
                        int dust = Dust.NewDust(drawInfo.Position - new Vector2(2f, 2f), Player.width + 4, Player.height + 4, Main.rand.Next(2) == 0 ? 57 : 244, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 100, default(Color), 1.5f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 0.5f;
                    }
                    r *= 0.025f;
                    g *= 0.15f;
                    b *= 0.035f;
                    fullBright = true;
                }
                /*
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                {
                    var calamityPlayerType = calamity.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                    if (calamityPlayerType != null)
                    {
                        var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)
                            ?.MakeGenericMethod(calamityPlayerType);
                        if (getModPlayerMethod != null)
                        {
                            if (getModPlayerMethod.Invoke(Player, null) is ModPlayer calPlayer)
                            {
                                var field1 = calamityPlayerType.GetField("rogueStealth",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                var field2 = calamityPlayerType.GetField("rogueStealthMax",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                if (field1 != null && field2 != null)
                                {
                                    float rogueStealth = (float)field1.GetValue(calPlayer);
                                    float rogueStealthMax = (float)field2.GetValue(calPlayer);
                                    bool noRogueStealth = rogueStealth == 0f || Player.townNPCs > 2f;
                                    if (noRogueStealth)
                                    {
                                        r *= 0.025f;
                                        g *= 0.15f;
                                        b *= 0.035f;
                                        fullBright = true;
                                    }
                                }
                            }
                        }
                    }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
                {
                    var calamityPlayerType = classic.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
                    if (calamityPlayerType != null)
                    {
                        var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)
                            ?.MakeGenericMethod(calamityPlayerType);
                        if (getModPlayerMethod != null)
                        {
                            if (getModPlayerMethod.Invoke(Player, null) is ModPlayer calPlayer)
                            {
                                var field1 = calamityPlayerType.GetField("rogueStealth",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                var field2 = calamityPlayerType.GetField("rogueStealthMax",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                if (field1 != null && field2 != null)
                                {
                                    float rogueStealth = (float)field1.GetValue(calPlayer);
                                    float rogueStealthMax = (float)field2.GetValue(calPlayer);
                                    bool noRogueStealth = rogueStealth == 0f || Player.townNPCs > 2f;
                                    if (noRogueStealth)
                                    {
                                        r *= 0.025f;
                                        g *= 0.15f;
                                        b *= 0.035f;
                                        fullBright = true;
                                    }
                                }
                            }
                        }
                    }
                }
                */
            }
        }
        /// <summary>
        /// 近战挥砍特效：元素手套生效时附带彩虹粒子（无位置修正，仅粒子表现）
        /// </summary>
        public override void MeleeEffects(Item item, Rectangle hitbox)
        {
            // 元素手套的彩虹粒子特效
            if (elementalGauntlet && Main.rand.NextBool(3))
            {
                int num280 = Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, DustID.RainbowTorch, Player.velocity.X * 0.2f + (float)(Player.direction * 3), Player.velocity.Y * 0.2f, 100, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.25f);
                Main.dust[num280].noGravity = true;
            }
            if (demonshadeSetBonus && Main.rand.NextBool(3))
            {
                Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, DustID.Shadowflame, Player.velocity.X * 0.2f + (float)(Player.direction * 3), Player.velocity.Y * 0.2f, 100, default(Color), 2.5f);
            }
        }
        /// <summary>
        /// tModLoader 的 FreeDodge 钩子：完全闪避伤害（不受常规闪避冷却影响）。
        /// 聚合大脑 1/10、大杂烩（The Amalgam）1/8 概率完全免伤；
        /// 弑神者胸甲 / 金源胸甲（godSlayerReflect）5% 概率完全免伤（数值膨胀关闭时为源值 2%）。
        /// </summary>
        public override bool FreeDodge(Player.HurtInfo info)
        {
            if(amalgamatedBrain && Main.rand.NextBool(10))
            {
                return true;
            }
            if(theAmalgam && Main.rand.NextBool(8))
            {
                return true;
            }
            if (godSlayerDamageProtect && info.Damage <= godSlayerDamageProtectMax)
            {
                godSlayerDamageProtectMax = 20;
                Player.immune = true;
                Player.immuneTime = 15;
                return true;
            }
            // 弑神者胸甲 / 金源胸甲的完全免伤（对应 CI 的 GodSlayerReflect 置 freeDodgeFromShieldAbsorption 再被 FreeDodge 吃掉）：
            // 数值膨胀开启 5%（与 tooltip 的膨胀文案一致），关闭取源值 2%（经典版与 CI 都是 1/50）。
            // 注意：这里返回 true 时原版只把本次伤害归零（见 Player.Hurt 的 FreeDodge 分支），不会代补无敌帧，所以要自己给。
            // 原先这段判定写在 ModifyHurt 里、只设 Player.immune 标记，伤害照吃，属于空操作——中英 tooltip 的"5% 完全无效"当时并没有实现。
            if (godSlayerReflect && Main.rand.NextBool(ConfigSystem.StatInflationEnabled ? 20 : 50))
            {
                Player.immune = true;
                Player.immuneTime = 15;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 被 NPC 接触命中前触发，统一通过乘算 modifiers.FinalDamage 施加本模组的接触减伤：
        /// 血肉图腾生效时减半并启动 20 秒冷却；泰拉套装近战（tarraDefense+tarraMelee）减半；
        /// 炎血狂怒期间减半；森林近战效果（silvaMelee）减至 0.8 倍；
        /// 凯旋药水（triumph）按该 NPC 剩余生命比例减伤，最高 25%。
        /// 注意：这些都必须改写 modifiers（仅作用于"本次"伤害），
        /// 直接改写 npc.damage 会永久污染 NPC 的全局伤害、逐次累积并影响其他玩家。
        /// </summary>
        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (fleshTotem && fleshTotemCooldown <= 0)
            {
                fleshTotemCooldown = 1200;
                modifiers.FinalDamage *= 0.5f;
            }
            if (tarraDefense && tarraMelee)
            {
                modifiers.FinalDamage *= 0.5f;   // 泰拉套装接触减伤：本次伤害减半
            }
            if (bloodflareMelee && bloodflareFrenzyTimer > 0)
            {
                modifiers.FinalDamage *= 0.5f;   // 炎血狂怒期间接触减伤：本次伤害减半
            }
            if (silvaMelee && silvaCountdown <= 0 && hasSilvaEffect)
            {
                modifiers.FinalDamage *= 0.8f;   // 森林近战减伤：本次伤害 ×0.8
            }
            if (triumph)
            {
                double HPMultiplier = 0.25 * (1.0 - ((double)npc.life / (double)npc.lifeMax));   // 目标血线越低减伤越高，最高 25%
                modifiers.FinalDamage *= 1f - (float)HPMultiplier;
            }
        }
        /// <summary>
        /// tModLoader 的 ModifyHitByProjectile 钩子：被弹幕命中、伤害结算前调用。
        /// 装备蜜蜂抗性饰品且命中来源属于蜜蜂弹幕列表时，将本次伤害减至 75%。
        /// </summary>
        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            if (beeResist && CalamityDemutation.beeProjectileList.Contains(proj.type))
            {
                modifiers.FinalDamage *= 0.75f;
            }
        }
        /// <summary>
        /// 受到伤害前触发：
        /// 血契有 25% 概率使本次伤害变为 2.5 倍（模拟"被暴击"）。
        /// 弑神者套 / 金源套（godSlayerDamage，由近战头的套装方法置位）把 80 及以下的基础伤害压到 1；
        /// 胸甲的完全免伤（godSlayerReflect）则在 FreeDodge 里结算（ModifyHurt 里设免疫标记取消不了本次伤害）。
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            double damageMult = 1.0 + ((bloodPact && Main.rand.NextBool(4)) ? 1.5 : 0.0) + (enraged ? 0.25 : 0.0);
            modifiers.FinalDamage *= (float)damageMult;
            if (theAbsorber)
            {
                int healAmt = (int)modifiers.SourceDamage.Base / 20;
                Player.statLife += healAmt;
                Player.HealEffect(healAmt);
                if (Player.statLife > Player.statLifeMax2)
                    Player.statLife = Player.statLifeMax2;
            }
            if (sponge)
            {
                int healAmt = (int)modifiers.SourceDamage.Base / 16;
                Player.statLife += healAmt;
                Player.HealEffect(healAmt);
                if (Player.statLife > Player.statLifeMax2)
                    Player.statLife = Player.statLifeMax2;
            }
            if ((godSlayerDamage && modifiers.SourceDamage.Base <= 80) || modifiers.SourceDamage.Base < 1)
            {
                modifiers.SourceDamage.Base = 1f;
            }
        }
        /// <summary>
        /// tModLoader 的 OnHurt 钩子：玩家受到伤害后调用。
        /// 吞噬者/巨壳/海绵受击触发龟壳爆发（ShellBoost）增益；
        /// 聚合大脑受击时以自身为中心范围困惑周围敌对 NPC 并喷射混乱脑弹幕。
        /// </summary>
        public override void OnHurt(Player.HurtInfo info)
        {
            if ((theAbsorber || giantShell || sponge) && !Player.panic)
            {
                Player.AddBuff(ModContent.BuffType<ShellBoost>(), 300);
            }
            if (amalgamatedBrain || theAmalgam)
            {
                // 以"本次受伤量"随机出一个作用半径，对半径内所有敌对 NPC 施加困惑
                //（血脑的能力，大杂烩=合体也应继承）
                for (int m = 0; m < 200; m++)
                {
                    if (Main.npc[m].active && !Main.npc[m].friendly)
                    {
                        float arg_67A_0 = (Main.npc[m].Center - Player.Center).Length();
                        float num10 = (float)Main.rand.Next(200 + (int)info.Damage / 2, 301 + (int)info.Damage * 2);
                        // 对随机半径做三段递减压缩：大半径收益被逐步削减，避免范围过于夸张
                        if (num10 > 500f)
                        {
                            num10 = 500f + (num10 - 500f) * 0.75f;
                        }
                        if (num10 > 700f)
                        {
                            num10 = 700f + (num10 - 700f) * 0.5f;
                        }
                        if (num10 > 900f)
                        {
                            num10 = 900f + (num10 - 900f) * 0.25f;
                        }
                        if (arg_67A_0 < num10)
                        {
                            float num11 = (float)Main.rand.Next(90 + (int)info.Damage / 3, 300 + (int)info.Damage / 2);
                            Main.npc[m].AddBuff(BuffID.Confused, (int)num11, false);
                        }
                    }
                }
                // 同时朝玩家速度方向抛出一颗混乱脑弹幕（纯视觉）
                // OnHurt / PostHurt 是"每名玩家 × 每一端"都跑的钩子（Player.Hurt → PlayerLoader.OnHurt/PostHurt，
                // 网络端收到 PlayerHurt 也会给别的玩家重放一遍），弹幕必须只在主人客户端生成，否则每端各多一份。
                // 源头把整段裹在 if (Player.whoAmI == Main.myPlayer) 里（CalamityPlayerHitHurt.cs OnHurt），移植时漏了。
                if (Player.whoAmI == Main.myPlayer)
                {
                    Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center.X + (float)Main.rand.Next(-40, 40), Player.Center.Y - (float)Main.rand.Next(20, 60), Player.velocity.X * 0.3f, Player.velocity.Y * 0.3f, ProjectileID.BrainOfConfusion, 0, 0f, Player.whoAmI, 0f, 0f);
                }
            }
            if (tarraMelee)
            {
                if (Main.rand.NextBool(4))
                {
                    Player.AddBuff(ModContent.BuffType<TarraLifeRegen>(), Main.rand.Next(90, 180));
                }
            }
            if (frostBarrier) /*&& NPC.downedBoss3*///解锁NPC.downedBoss3
            {
                SoundEngine.PlaySound(SoundID.Item27, Player.position);
                for (int m = 0; m < 200; m++)
                {
                    if (Main.npc[m].active && !Main.npc[m].friendly)
                    {
                        float distance = (Main.npc[m].Center - Player.Center).Length();
                        float num10 = (float)Main.rand.Next(200 + (int)info.Damage / 2, 301 + (int)info.Damage * 2);
                        if (num10 > 500f)
                        {
                            num10 = 500f + (num10 - 500f) * 0.75f;
                        }
                        if (num10 > 700f)
                        {
                            num10 = 700f + (num10 - 700f) * 0.5f;
                        }
                        if (num10 > 900f)
                        {
                            num10 = 900f + (num10 - 900f) * 0.25f;
                        }
                        if (distance < num10)
                        {
                            float num11 = (float)Main.rand.Next(90 + (int)info.Damage / 3, 240 + (int)info.Damage / 2);
                            // 源各版都用灾厄的 GlacialState（真正能定住 NPC）；该 buff 在 1.4.4 缺失，
                            // 故走 fallback：取不到时退回原版 Frozen
                            ApplyCalamityBuffWithFallback(Main.npc[m], "GlacialState", (int)num11, BuffID.Frozen);
                        }
                    }
                }
            }
            if(revivify)
            {
                int healAmt = (int)(info.Damage / 15D);
                Player.Heal(healAmt);
            }
        }
        /// <summary>
        /// tModLoader 的 ModifyHitNPCWithItem 钩子：玩家用物品（真近战等）命中 NPC、伤害结算前调用，
        /// 通过乘算 modifiers.FinalDamage 施加本模组的近战增伤。
        /// 狂怒 buff（enraged）提供 ×2.25 最终伤害——原版该效果仅限 Boss Rush，
        /// 此处以"解除BOSSRUSH限制"的写法无条件生效；女巫套近战（silvaMelee）另在 1/4 概率下追加 +4.0 倍。
        /// </summary>
        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            double damageMult = 1.0;
            if (enraged)
            {
                if (enraged) // 解除BOSSRUSH限制
                {
                    damageMult += 1.25;
                }
            }
            if (silvaMelee && Main.rand.NextBool(4) && (item.CountsAsClass<MeleeDamageClass>() || item.CountsAsClass<MeleeNoSpeedDamageClass>()))
            {
                damageMult += 4.0;
            }
            modifiers.FinalDamage *= (float)damageMult;
        }
        /// <summary>
        /// tModLoader 的 ModifyHitNPCWithProj 钩子：玩家弹幕命中 NPC、伤害结算前调用，乘算 modifiers.FinalDamage。
        /// 与近战版一致，狂怒 buff（enraged）提供 ×2.25 最终伤害且不受 Boss Rush 限制；
        /// 金之套装标记与女巫近战（auricSet &amp;&amp; silvaMelee）的弹幕再按当前生命比例追加至多 +0.2 倍。
        /// 末尾在开启"回退原版削弱"且安装现代版灾厄时，对召唤弹幕补偿灾厄的跨职业惩罚（÷0.75 撤销之）。
        /// </summary>
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            double damageMult = 1.0;
            if (enraged)
            {
                if (enraged) // 解除BOSSRUSH限制
                {
                    damageMult += 1.25;
                }
            }
            if (auricSet && silvaMelee && proj.CountsAsClass<MeleeDamageClass>())
            {
                double multiplier = (double)Player.statLife / (double)Player.statLifeMax2;
                damageMult += multiplier * 0.2;
            }
            // 始源林海射手套装：免死无敌窗口内远程伤害 +40%
            // （经典版 CalamityPlayerPreTrailer.ModifyHitNPCWithProj 的 damageMult += 0.4，判据与源一致：
            //  无敌窗口开启 + 射手头在套装内 + 本次弹幕属于远程职业）
            if (silvaRanged && silvaCountdown > 0 && hasSilvaEffect && proj.CountsAsClass<RangedDamageClass>())
                damageMult += 0.4;
            // 弑神者射手套装：远程暴击的「再次暴击」——2026-10-04 按用户口径改走 CI 模型，
            // 取代原先经典版的 1/max(15, 100-暴击率) 单次骰子：
            // · 总暴击率 >100% 时，按「溢出部分」（总暴击率 − 100）的百分比概率触发（CI 的 randomChance > 1 分支）；
            // · 未超过 100%（或溢出 ≤1%）时，退化为固定 5% 概率（CI 的 Main.rand.NextBool(20)）。
            // 触发后把暴击伤害翻倍：CritDamage 只对暴击生效，故等价于「暴击时造成 4 倍伤害」。
            // 备注：CI 源码该 5% 分支写作 hitInfo.Damage *= 4——叠加在已含暴击（2 倍）的伤害上即 8 倍，
            // 与 CI 自身 tooltip「造成四倍伤害」矛盾，属笔误；本工程取 tooltip 口径（两分支都翻倍到 4 倍），
            // 只让触发条件照 CI。
            if (godSlayerRanged && proj.CountsAsClass<RangedDamageClass>())
            {
                int excessCrit = (int)Player.GetTotalCritChance(DamageClass.Ranged) - 100;
                bool againCrit = excessCrit > 1
                    ? Main.rand.Next(1, 101) <= excessCrit   // 溢出暴击率% 的概率
                    : Main.rand.NextBool(20);                // 未溢出时固定 5%
                if (againCrit)
                    modifiers.CritDamage *= 2f;
            }
            modifiers.FinalDamage *= (float)damageMult;
            // 召唤师跨职业 nerf 回调：灾厄在 ModifyHitNPCWithProj 里对「手持非召唤职业武器时的召唤弹幕」×0.75
            // （CalamityPlayerHitHurt.cs:663-678 + Utilities/PlayerUtils.cs:1005-1032 ShouldTriggerSummonPenalty）。
            // 判据照抄灾厄：必须手持「近战/远程/魔法/投掷」职业武器、可用、非工具/饰品/弹药，
            // 且不在豁免名单内（禁忌甲+法师武器、撒旦军事件；灾厄自有的 fearmonger/GemTech/
            // 各类 CalamityItemSets 名单本模组读不到，暂不覆盖）。
            if (ConfigSystem.Instance?.RevertVanillaNerfs == true && ModLoader.HasMod("CalamityMod") && proj.CountsAsClass<SummonDamageClass>())
            {
                Item heldItem = Player.HeldItem;
                bool forbiddenWithMagicWeapon = Player.armor[0].type == ItemID.AncientBattleArmorHat
                    && Player.armor[1].type == ItemID.AncientBattleArmorShirt
                    && Player.armor[2].type == ItemID.AncientBattleArmorPants
                    && heldItem.CountsAsClass<MagicDamageClass>();
                bool crossClassNerfDisabled = forbiddenWithMagicWeapon || Terraria.GameContent.Events.DD2Event.Ongoing;
                bool heldClassedWeapon = !heldItem.CountsAsClass<SummonDamageClass>()
                    && (heldItem.CountsAsClass<MeleeDamageClass>()
                        || heldItem.CountsAsClass<RangedDamageClass>()
                        || heldItem.CountsAsClass<MagicDamageClass>()
                        || heldItem.CountsAsClass<ThrowingDamageClass>());
                bool heldIsTool = heldItem.pick > 0 || heldItem.axe > 0 || heldItem.hammer > 0;
                if (heldClassedWeapon && heldItem.useStyle != ItemUseStyleID.None && !heldIsTool
                    && !heldItem.accessory && heldItem.ammo == AmmoID.None && !crossClassNerfDisabled)
                    modifiers.FinalDamage /= 0.75f;
            }
        }
        /// <summary>
        /// tModLoader 的 PostHurt 钩子：伤害结算并扣血后调用。
        /// 根据装备的受击反制饰品向四周迸发电火花（Spark）或松露孢子（TruffleSpore）弹幕，
        /// 伤害随游戏阶段（是否困难模式）与饰品类型提升。
        /// </summary>
        public override void PostHurt(Player.HurtInfo info)
        {
            bool hardMode = Main.hardMode;
            // 亚米迪亚斯火花/吞噬者：受伤后向四周迸发两圈电火花
            if (amidiasSpark || theAbsorber)
            {
                if (info.Damage > 0)
                {
                    SoundEngine.PlaySound(SoundID.Item93, Player.position);
                    float spread = 45f * 0.0174f;
                    double startAngle = Math.Atan2(Player.velocity.X, Player.velocity.Y) - spread / 2;
                    double deltaAngle = spread / 8f;
                    double offsetAngle;
                    int i;
                    int sDamage = hardMode ? 36 : 6;
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        for (i = 0; i < 4; i++)
                        {
                            offsetAngle = (startAngle + deltaAngle * (i + i * i) / 2f) + 32f * i;
                            int spark1 = Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center.X, Player.Center.Y, (float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f), ModContent.ProjectileType<Spark>(), sDamage, 1.25f, Player.whoAmI, 0f, 0f);
                            int spark2 = Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center.X, Player.Center.Y, (float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f), ModContent.ProjectileType<Spark>(), sDamage, 1.25f, Player.whoAmI, 0f, 0f);
                            Main.projectile[spark1].timeLeft = 120;
                            Main.projectile[spark2].timeLeft = 120;
                        }
                    }
                }
            }
            // 海绵受伤反制：再迸发一圈电火花（伤害高于亚米迪亚斯火花版）
            if(sponge)
            {
                if (info.Damage > 0)
                {
                    SoundEngine.PlaySound(SoundID.Item93, Player.position);
                    float spread = 45f * 0.0174f;
                    double startAngle = Math.Atan2(Player.velocity.X, Player.velocity.Y) - spread / 2;
                    double deltaAngle = spread / 8f;
                    double offsetAngle;
                    int i;
                    int sDamage = hardMode ? 144 : 36;
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        for (i = 0; i < 4; i++)
                        {
                            offsetAngle = (startAngle + deltaAngle * (i + i * i) / 2f) + 32f * i;
                            int spark1 = Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center.X, Player.Center.Y, (float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f), ModContent.ProjectileType<Spark>(), sDamage, 1.25f, Player.whoAmI, 0f, 0f);
                            int spark2 = Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center.X, Player.Center.Y, (float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f), ModContent.ProjectileType<Spark>(), sDamage, 1.25f, Player.whoAmI, 0f, 0f);
                            Main.projectile[spark1].timeLeft = 120;
                            Main.projectile[spark2].timeLeft = 120;
                        }
                    }
                }
            }
            // 真菌甲壳/吞噬者：受伤后从身侧迸发一圈松露孢子
            if (fungalCarapace || theAbsorber)
            {
                if (info.Damage > 0)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit45, Player.position);
                    float spread = 45f * 0.0174f;
                    double startAngle = Math.Atan2(Player.velocity.X, Player.velocity.Y) - spread / 2;
                    double deltaAngle = spread / 8f;
                    double offsetAngle;
                    int i;
                    int fDamage = (int)Player.GetDamage<GenericDamageClass>().ApplyTo(70);
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        for (i = 0; i < 4; i++)
                        {
                            float xPos = Main.rand.NextBool(2) ? Player.Center.X + 100 : Player.Center.X - 100;
                            Vector2 vector2 = new(xPos, Player.Center.Y + Main.rand.Next(-100, 101));
                            offsetAngle = (startAngle + deltaAngle * (i + i * i) / 2f) + 32f * i;
                            int spore1 = Projectile.NewProjectile(Entity.GetSource_FromThis(), vector2.X, vector2.Y, (float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f), ProjectileID.TruffleSpore, fDamage, 1.25f, Player.whoAmI, 0f, 0f);
                            int spore2 = Projectile.NewProjectile(Entity.GetSource_FromThis(), vector2.X, vector2.Y, (float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f), ProjectileID.TruffleSpore, fDamage, 1.25f, Player.whoAmI, 0f, 0f);
                            Main.projectile[spore1].timeLeft = 120;
                            Main.projectile[spore2].timeLeft = 120;
                        }
                    }
                }
            }
            // 海绵受伤反制：再迸发一轮松露孢子（伤害为真菌甲壳版两倍）
            if(sponge)
            {
                if (info.Damage > 0)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit45, Player.position);
                    float spread = 45f * 0.0174f;
                    double startAngle = Math.Atan2(Player.velocity.X, Player.velocity.Y) - spread / 2;
                    double deltaAngle = spread / 8f;
                    double offsetAngle;
                    int i;
                    int fDamage = 448;//56 * 4 * 2 = 448
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        for (i = 0; i < 4; i++)
                        {
                            float xPos = Main.rand.NextBool(2) ? Player.Center.X + 100 : Player.Center.X - 100;
                            Vector2 vector2 = new(xPos, Player.Center.Y + Main.rand.Next(-100, 101));
                            offsetAngle = (startAngle + deltaAngle * (i + i * i) / 2f) + 32f * i;
                            int spore1 = Projectile.NewProjectile(Entity.GetSource_FromThis(), vector2.X, vector2.Y, (float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f), ProjectileID.TruffleSpore, fDamage, 1.25f, Player.whoAmI, 0f, 0f);
                            int spore2 = Projectile.NewProjectile(Entity.GetSource_FromThis(), vector2.X, vector2.Y, (float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f), ProjectileID.TruffleSpore, fDamage, 1.25f, Player.whoAmI, 0f, 0f);
                            Main.projectile[spore1].timeLeft = 120;
                            Main.projectile[spore2].timeLeft = 120;
                        }
                    }
                }
            }
            if (demonshadeSetBonus)
            {
                int shadowBeamDamage = (int)Player.GetDamage<GenericDamageClass>().ApplyTo(3000);
                int demonScytheDamage = (int)Player.GetDamage<GenericDamageClass>().ApplyTo(5000);
                for (int l = 0; l < 2; l++)
                {
                    float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                    float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                    Vector2 vector = new Vector2(x, y);
                    float num15 = Player.position.X + (float)(Player.width / 2) - vector.X;
                    float num16 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                    num15 += (float)Main.rand.Next(-100, 101);
                    int num17 = 22;
                    float num18 = (float)Math.Sqrt((double)(num15 * num15 + num16 * num16));
                    num18 = (float)num17 / num18;
                    num15 *= num18;
                    num16 *= num18;
                    // 弹幕只在主人客户端生成（源头把这两段也裹在 whoAmI == Main.myPlayer 里）
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        int num19 = Projectile.NewProjectile(Entity.GetSource_FromThis(), x, y, num15, num16, ProjectileID.ShadowBeamFriendly, shadowBeamDamage, 7f, Player.whoAmI, 0f, 0f);
                        Main.projectile[num19].ai[1] = Player.position.Y;
                    }
                }
                for (int l = 0; l < 5; l++)
                {
                    float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                    float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                    Vector2 vector = new Vector2(x, y);
                    float num15 = Player.position.X + (float)(Player.width / 2) - vector.X;
                    float num16 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                    num15 += (float)Main.rand.Next(-100, 101);
                    int num17 = 22;
                    float num18 = (float)Math.Sqrt((double)(num15 * num15 + num16 * num16));
                    num18 = (float)num17 / num18;
                    num15 *= num18;
                    num16 *= num18;
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        int num19 = Projectile.NewProjectile(Entity.GetSource_FromThis(), x, y, num15, num16, ProjectileID.DemonScythe, demonScytheDamage, 7f, Player.whoAmI, 0f, 0f);
                        Main.projectile[num19].ai[1] = Player.position.Y;
                    }
                }
            }
            if (deificAmulet)
            {
                if (info.Damage == 1.0)
                {
                    Player.immuneTime += 10;
                }
                else
                {
                    Player.immuneTime += 20;
                }
                for (int n = 0; n < 3; n++)
                {
                    float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                    float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                    Vector2 vector = new(x, y);
                    float num13 = Player.position.X + (float)(Player.width / 2) - vector.X;
                    float num14 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                    num13 += (float)Main.rand.Next(-100, 101);
                    int num15 = 29;
                    float num16 = (float)Math.Sqrt((double)(num13 * num13 + num14 * num14));
                    num16 = (float)num15 / num16;
                    num13 *= num16;
                    num14 *= num16;
                    // 神圣护符的反击星辰：同样只在主人客户端生成
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        int num17 = Projectile.NewProjectile(Entity.GetSource_FromThis(), x, y, num13, num14, ProjectileID.HallowStar, (int)Player.GetDamage<GenericDamageClass>().ApplyTo(130), 4f, Player.whoAmI, 0f, 0f);
                        Main.projectile[num17].usesLocalNPCImmunity = true;
                        Main.projectile[num17].localNPCHitCooldown = 5;
                    }
                }
            }
            if (rampartofDeities)
            {
                if (info.Damage == 1.0)
                {
                    Player.immuneTime += 15;
                }
                else
                {
                    Player.immuneTime += 30;
                }
                for (int n = 0; n < 6; n++)
                {
                    float x = Player.position.X + (float)Main.rand.Next(-400, 400);
                    float y = Player.position.Y - (float)Main.rand.Next(500, 800);
                    Vector2 vector = new(x, y);
                    float num13 = Player.position.X + (float)(Player.width / 2) - vector.X;
                    float num14 = Player.position.Y + (float)(Player.height / 2) - vector.Y;
                    num13 += (float)Main.rand.Next(-100, 101);
                    int num15 = 29;
                    float num16 = (float)Math.Sqrt((double)(num13 * num13 + num14 * num14));
                    num16 = (float)num15 / num16;
                    num13 *= num16;
                    num14 *= num16;
                    // 神谕壁垒的反击星辰：同样只在主人客户端生成
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        int num17 = Projectile.NewProjectile(Entity.GetSource_FromThis(), x, y, num13, num14, ProjectileID.HallowStar, (int)Player.GetDamage<GenericDamageClass>().ApplyTo(130), 4f, Player.whoAmI, 0f, 0f);
                        Main.projectile[num17].usesLocalNPCImmunity = true;
                        Main.projectile[num17].localNPCHitCooldown = 5;
                    }
                }
            }
            if (godSlayerMelee)
            {
                if (info.Damage > 80)
                {
                    SoundEngine.PlaySound(SoundID.Item73, Player.Center);
                    float spread = 45f * 0.0174f;
                    double startAngle = Math.Atan2(Player.velocity.X, Player.velocity.Y) - spread / 2;
                    double deltaAngle = spread / 8f;
                    double offsetAngle;
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            offsetAngle = startAngle + deltaAngle * (i + i * i) / 2f + 32f * i;
                            Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center.X, Player.Center.Y, (float)(Math.Sin(offsetAngle) * 10f), (float)(Math.Cos(offsetAngle) * 10f), ModContent.ProjectileType<GodSlayerDart>(), (int)Player.GetDamage<MeleeDamageClass>().ApplyTo(1500), 5f, Player.whoAmI, 1f, 0f);
                            Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center.X, Player.Center.Y, (float)(-Math.Sin(offsetAngle) * 10f), (float)(-Math.Cos(offsetAngle) * 10f), ModContent.ProjectileType<GodSlayerDart>(), (int)Player.GetDamage<MeleeDamageClass>().ApplyTo(1500), 5f, Player.whoAmI, 1f, 0f);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// tModLoader 的 ProcessTriggers 钩子：每帧处理按键触发，只在本地客户端（键位状态有效）有实际意义。
        /// 消费两个自定义键位（定义于 Systems/KeybindsSystem）：
        /// 1) GodslayerDashHotKey（默认 H）——交给本模组自持的弑神者冲刺（RequestGodSlayerDash）；
        /// 2) DemonshadeHotKey（默认 Y，套装主动技能）——依次判定：恶魔之影套装（播放音效/迸发吸魂尘埃、
        ///    自身获得 600 帧狂怒 buff，服务器端对 3000 距离内的敌人一并施加狂怒）、
        ///    欧米伽蓝套装（冷却 1800 帧，净化粉末尘埃爆发）、塔拉近战（tarraCooldown 归零时置位 tarraDefense）。
        /// </summary>
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            // 弑神者冲刺：本模组自持实现（见 CalamityDemutationPlayer.GodSlayerDash.cs），
            // 不再借用灾厄的 dash 框架，闸门/位移/命中/冷却均由本模组结算
            if (KeybindsSystem.GodslayerDashHotKey.JustPressed)
                RequestGodSlayerDash();
            if (KeybindsSystem.DemonshadeHotKey.JustPressed)
            {
                if (demonshadeSetBonus)
                {
                    SoundEngine.PlaySound(SoundID.Zombie104, Player.position);
                    for (int num502 = 0; num502 < 36; num502++)
                    {
                        int dust = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y + 16f), Player.width, Player.height - 16, DustID.LifeDrain, 0f, 0f, 0, default(Color), 1f);
                        Main.dust[dust].velocity *= 3f;
                        Main.dust[dust].scale *= 1.15f;
                    }
                    int num226 = 36;
                    for (int num227 = 0; num227 < num226; num227++)
                    {
                        Vector2 vector6 = Vector2.Normalize(Player.velocity) * new Vector2((float)Player.width / 2f, (float)Player.height) * 0.75f;
                        vector6 = vector6.RotatedBy((double)((float)(num227 - (num226 / 2 - 1)) * 6.28318548f / (float)num226), default(Vector2)) + Player.Center;
                        Vector2 vector7 = vector6 - Player.Center;
                        int num228 = Dust.NewDust(vector6 + vector7, 0, 0, DustID.LifeDrain, vector7.X * 1.5f, vector7.Y * 1.5f, 100, default(Color), 1.4f);
                        Main.dust[num228].noGravity = true;
                        Main.dust[num228].noLight = true;
                        Main.dust[num228].velocity = vector7;
                    }
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        Player.AddBuff(ModContent.BuffType<Enraged>(), 600, false);
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int l = 0; l < 200; l++)
                        {
                            NPC nPC = Main.npc[l];
                            if (nPC.active && !nPC.friendly && !nPC.dontTakeDamage && Vector2.Distance(Player.Center, nPC.Center) <= 3000f)
                            {
                                nPC.AddBuff(ModContent.BuffType<Enraged>(), 600, false);
                            }
                        }
                    }
                }
            }
            if (KeybindsSystem.TarragonHotKey.JustPressed)
            {
                if (tarraMelee && tarraCooldown <= 0)
                {
                    tarraDefense = true;
                }
                // 血炎射手套装：同一个键释放波尔特加斯特的迷失灵魂（经典版口径：30 秒冷却、一次 16 枚、每枚 800 伤害）
                if (bloodflareRanged && bloodflareRangedCooldown <= 0)
                {
                    bloodflareRangedCooldown = 1800;
                    SoundEngine.PlaySound(SoundID.Zombie104, Player.position);
                    for (int i = 0; i < 64; i++)
                    {
                        int dust = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y + 16f), Player.width, Player.height - 16, DustID.RedTorch, 0f, 0f, 0, default, 1f);
                        Main.dust[dust].velocity *= 3f;
                        Main.dust[dust].scale *= 1.15f;
                    }
                    // 一圈环状尘（源写法：以玩家中心为圆心把尘铺成一圈并向外推）
                    const int ringDustCount = 36;
                    Vector2 ringBase = Player.velocity.SafeNormalize(Vector2.UnitY) * new Vector2(Player.width / 2f, Player.height) * 0.75f;
                    for (int i = 0; i < ringDustCount; i++)
                    {
                        Vector2 ringPos = ringBase.RotatedBy((i - (ringDustCount / 2 - 1)) * MathHelper.TwoPi / ringDustCount) + Player.Center;
                        Vector2 offset = ringPos - Player.Center;
                        int dust = Dust.NewDust(ringPos + offset, 0, 0, DustID.RedTorch, offset.X * 1.5f, offset.Y * 1.5f, 100, default, 1.4f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].noLight = true;
                        Main.dust[dust].velocity = offset;
                    }
                    // 灵魂本体：一组 8 次、每次左右对称各一枚（共 16 枚），ai[1] 决定转向速率（0.5~1.5）
                    const int soulDamage = 800;
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        const float spread = 45f * 0.0174f;   // 源把 45° 直接写成弧度系数（0.0174 ≈ π/180）
                        double startAngle = Math.Atan2(Player.velocity.X, Player.velocity.Y) - spread / 2;
                        double deltaAngle = spread / 8f;
                        for (int i = 0; i < 8; i++)
                        {
                            float ai1 = Main.rand.NextFloat() + 0.5f;
                            float randomSpeed = Main.rand.Next(1, 7);
                            float randomSpeed2 = Main.rand.Next(1, 7);
                            double offsetAngle = (startAngle + deltaAngle * (i + i * i) / 2f) + 32f * i;
                            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center.X, Player.Center.Y, (float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f) + randomSpeed, ModContent.ProjectileType<BloodflareSoul>(), soulDamage, 0f, Player.whoAmI, 0f, ai1);
                            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center.X, Player.Center.Y, (float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f) + randomSpeed2, ModContent.ProjectileType<BloodflareSoul>(), soulDamage, 0f, Player.whoAmI, 0f, ai1);
                        }
                    }
                }
            }
            if (KeybindsSystem.OmegaBlueHotKey.JustPressed)
            {
                if (omegaBlueSet && omegaBlueCooldown <= 0)
                {
                    omegaBlueCooldown = 1800;
                    SoundEngine.PlaySound(SoundID.Zombie104, Player.position);
                    for (int i = 0; i < 66; i++)
                    {
                        int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.PurificationPowder, 0, 0, 100, Color.Transparent, 2.6f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].fadeIn = 1f;
                        Main.dust[d].velocity *= 6.6f;
                    }
                }
            }
            if (KeybindsSystem.ElysianHotKey.JustPressed)
            {
                if (elysianAegispower && !Player.mount.Active)
                {
                    elysianGuard = !elysianGuard;
                }
            }
        }
        /// <summary>
        /// 近战武器击中NPC时触发效果
        /// </summary>
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 亚利姆徽章：施加圣焰 debuff（现代版 HolyFlames / 经典版 HolyLight，
            // 各按 1/4、1/2、其余概率给 360/240/120 帧；GetBuffType 缓存容错，模组缺 buff 名静默跳过）
            if (yharimsInsignia && (item.CountsAsClass<MeleeDamageClass>() || item.CountsAsClass<MeleeNoSpeedDamageClass>()))
            {
                int holyModern = GetBuffType("CalamityMod", "HolyFlames");
                if (holyModern > 0)
                {
                    if (Main.rand.NextBool(4))
                        target.AddBuff(holyModern, 360, false);
                    else if (Main.rand.NextBool(2))
                        target.AddBuff(holyModern, 240, false);
                    else
                        target.AddBuff(holyModern, 120, false);
                }
                int holyClassic = GetBuffType("CalamityModClassicPreTrailer", "HolyLight");
                if (holyClassic > 0)
                {
                    if (Main.rand.NextBool(4))
                        target.AddBuff(holyClassic, 360, false);
                    else if (Main.rand.NextBool(2))
                        target.AddBuff(holyClassic, 240, false);
                    else
                        target.AddBuff(holyClassic, 120, false);
                }
            }
            // 元素手套：施加多种 debuff（原版 6 种 + 现代/经典版灾厄扩展 debuff）
            if (elementalGauntlet && (item.CountsAsClass<MeleeDamageClass>() || item.CountsAsClass<MeleeNoSpeedDamageClass>()))
            {
                target.AddBuff(BuffID.Poisoned, 120, false);
                target.AddBuff(BuffID.OnFire, 120, false);
                target.AddBuff(BuffID.CursedInferno, 120, false);
                target.AddBuff(BuffID.Frostburn, 120, false);
                target.AddBuff(BuffID.Ichor, 120, false);
                target.AddBuff(BuffID.Venom, 120, false);
                // 现代版灾厄元素 debuff
                ApplyCalamityBuff(target, "CalamityMod", "Voidfrost", 120);
                ApplyCalamityBuff(target, "CalamityMod", "Nightwither", 120);
                ApplyCalamityBuff(target, "CalamityMod", "Plague", 120);
                ApplyCalamityBuff(target, "CalamityMod", "SulphuricPoisoning", 120);
                ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
                ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 120);
                ApplyCalamityBuff(target, "CalamityMod", "ElementalMix", 120);
                // 经典版灾厄元素 debuff（修：此分支原先缺失整段，仅装经典版时近战挥砍无这些，
                // 现与弹幕命中分支（OnHitNPCWithProj）对齐补齐）
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "AbyssalFlames", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "Plague", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BrimstoneFlames", 120);
                if (Main.rand.NextBool(5))
                    ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GlacialState", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 120);
            }
            if (demonshadeSetBonus)
            {
                if (Main.rand.NextBool(4))
                {
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 360, false);
                }
                else if (Main.rand.NextBool(2))
                {
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 240, false);
                }
                else
                {
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 120, false);
                }
            }
            if(omegaBlueChestplate)
            {
                ApplyCalamityBuff(target, "CalamityMod", "HadopelagicPressure", 240);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "CrushDepth", 240);
            }
            if (bloodflareMelee && (item.CountsAsClass<MeleeDamageClass>()|| item.CountsAsClass<MeleeNoSpeedDamageClass>()))
            {
                if (bloodflareMeleeHits < 15 && bloodflareFrenzyTimer <= 0 && bloodflareFrenzyCooldown <= 0)
                {
                    bloodflareMeleeHits++;
                }
                // 回血受 target.canGhostHeal 限制（对齐 2.0.3.9 CalamityPlayerOnHit.cs:1374 的 IsAnEnemy && canGhostHeal && !moonLeech）；命中计数不受影响
                if (Player.whoAmI == Main.myPlayer && target.canGhostHeal && !Player.moonLeech)
                {
                    int healAmount = (Main.rand.Next(3) + 1);
                    Player.statLife += healAmount;
                    Player.HealEffect(healAmount);
                    if (Player.statLife > Player.statLifeMax2)
                        Player.statLife = Player.statLifeMax2;
                }
            }
            int weaponDamage = Player.HeldItem.damage;
            if (godSlayerMelee && godSlayerMeleefireCD <= 0 && (hit.DamageType == DamageClass.Melee || hit.DamageType == DamageClass.MeleeNoSpeed))
            {
                int finalDamage = 500 + weaponDamage / 2;
                Vector2 getSpwanPos = new(Player.Center.X, Player.Center.Y);
                Vector2 velocity = CDUtil.GiveVelocity(200f);
                Projectile.NewProjectile(Player.GetSource_FromThis(), getSpwanPos, velocity * 4f, ModContent.ProjectileType<GodSlayerDart>(), finalDamage, 0f, Player.whoAmI);
                godSlayerMeleefireCD = 60;
            }
            if (holyWrath)
            {
                ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
            }
            if (armorShattering || armorCrumbling)
            {
                ApplyCalamityBuff(target, "CalamityMod", "ArmorCrunch", 240);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "ArmorCrunch", 240);
            }
        }
        /// <summary>
        /// 近战弹幕击中NPC时触发效果
        /// </summary>
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 亚利姆徽章：近战弹幕施加圣焰 debuff（现代/经典版，GetBuffType 缓存容错）
            if (yharimsInsignia && proj.CountsAsClass<MeleeDamageClass>())
            {
                int holyModern = GetBuffType("CalamityMod", "HolyFlames");
                if (holyModern > 0)
                {
                    if (Main.rand.NextBool(4))
                        target.AddBuff(holyModern, 360, false);
                    else if (Main.rand.NextBool(2))
                        target.AddBuff(holyModern, 240, false);
                    else
                        target.AddBuff(holyModern, 120, false);
                }
                int holyClassic = GetBuffType("CalamityModClassicPreTrailer", "HolyLight");
                if (holyClassic > 0)
                {
                    if (Main.rand.NextBool(4))
                        target.AddBuff(holyClassic, 360, false);
                    else if (Main.rand.NextBool(2))
                        target.AddBuff(holyClassic, 240, false);
                    else
                        target.AddBuff(holyClassic, 120, false);
                }
            }
            // 元素手套：近战弹幕施加多种 debuff（原版 6 种 + 两灾厄扩展 debuff）
            if (elementalGauntlet && (proj.CountsAsClass<MeleeDamageClass>() || proj.CountsAsClass<MeleeNoSpeedDamageClass>()))
            {
                target.AddBuff(BuffID.Poisoned, 120, false);
                target.AddBuff(BuffID.OnFire, 120, false);
                target.AddBuff(BuffID.CursedInferno, 120, false);
                target.AddBuff(BuffID.Frostburn, 120, false);
                target.AddBuff(BuffID.Ichor, 120, false);
                target.AddBuff(BuffID.Venom, 120, false);
                // 现代版灾厄元素 debuff
                ApplyCalamityBuff(target, "CalamityMod", "Voidfrost", 120);
                ApplyCalamityBuff(target, "CalamityMod", "Nightwither", 120);
                ApplyCalamityBuff(target, "CalamityMod", "Plague", 120);
                ApplyCalamityBuff(target, "CalamityMod", "SulphuricPoisoning", 120);
                ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
                ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 120);
                ApplyCalamityBuff(target, "CalamityMod", "ElementalMix", 120);
                // 经典版灾厄元素 debuff
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "AbyssalFlames", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "Plague", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BrimstoneFlames", 120);
                if (Main.rand.NextBool(5))
                    ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GlacialState", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 120);
            }
            if(statisBlessing && (proj.CountsAsClass<SummonDamageClass>() || proj.CountsAsClass<SummonMeleeSpeedDamageClass>()))
            {
                // 时滞（TemporalSadness）：现代/经典两灾厄同名，GetBuffType 容错缺名跳过
                ApplyCalamityBuff(target, "CalamityMod", "TemporalSadness", 60);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "TemporalSadness", 60);
            }
            // 时滞（TemporalSadness）与暗影焰：诅咒与腰带分别结算，帧数不同故不合并
            if (statisCurse && (proj.CountsAsClass<SummonDamageClass>() || proj.CountsAsClass<SummonMeleeSpeedDamageClass>()))
            {
                ApplyCalamityBuff(target, "CalamityMod", "TemporalSadness", 60);
                target.AddBuff(BuffID.ShadowFlame, 300);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "TemporalSadness", 60);
            }
            if (statisBeltOfCurses && (proj.CountsAsClass<SummonDamageClass>() || proj.CountsAsClass<SummonMeleeSpeedDamageClass>()))
            {
                ApplyCalamityBuff(target, "CalamityMod", "TemporalSadness", 120);
                target.AddBuff(BuffID.ShadowFlame, 300);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "TemporalSadness", 120);
            }
            if(theFirstShadowflame && (proj.CountsAsClass<SummonDamageClass>() || proj.CountsAsClass<SummonMeleeSpeedDamageClass>()))
            {
                target.AddBuff(BuffID.ShadowFlame, 300);
            }
            if (demonshadeSetBonus)
            {
                if (Main.rand.NextBool(4))
                {
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 360, false);
                }
                else if (Main.rand.NextBool(2))
                {
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 240, false);
                }
                else
                {
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 120, false);
                }
            }
            if (omegaBlueChestplate)
            {
                ApplyCalamityBuff(target, "CalamityMod", "HadopelagicPressure", 240);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "CrushDepth", 240);
            }
            if (bloodflareMelee && proj.CountsAsClass<MeleeNoSpeedDamageClass>())
            {
                if (bloodflareMeleeHits < 15 && bloodflareFrenzyTimer <= 0 && bloodflareFrenzyCooldown <= 0)
                {
                    bloodflareMeleeHits++;
                }
                // 回血受 target.canGhostHeal 限制（对齐 2.0.3.9 CalamityPlayerOnHit.cs:1374 的 IsAnEnemy && canGhostHeal && !moonLeech）；命中计数不受影响
                if (Player.whoAmI == Main.myPlayer && target.canGhostHeal && !Player.moonLeech)
                {
                    int healAmount = (Main.rand.Next(3) + 1);
                    Player.statLife += healAmount;
                    Player.HealEffect(healAmount);
                    if (Player.statLife > Player.statLifeMax2)
                        Player.statLife = Player.statLifeMax2;
                }
            }
            if (proj.CountsAsClass<MeleeDamageClass>() && silvaMelee && Main.rand.NextBool(4))
                target.AddBuff(ModContent.BuffType<SilvaHysteresis>(), 20);
            // 龙蒿射手套装：远程暴击命中时在敌人处炸出一场树叶（经典版 CalamityPlayerPreTrailer.OnHitNPCWithProj 的口径：
            // 2~3 枚 ProjectileID.Leaf，每枚伤害 = 本次弹幕伤害 × 0.25，随机方向、速度 7.0~10.0）。
            // 源写在这条命中回调里，按工程口径只在主人端跑，无需额外判据
            if (tarraRanged && hit.Crit && proj.CountsAsClass<RangedDamageClass>())
            {
                int leafCount = Main.rand.Next(2, 4);
                for (int i = 0; i < leafCount; i++)
                {
                    Vector2 leafVelocity = Main.rand.NextVector2Unit() * (Main.rand.Next(70, 101) * 0.1f);
                    Projectile.NewProjectile(Player.GetSource_FromThis(), target.Center, leafVelocity, ProjectileID.Leaf, (int)(proj.damage * 0.25), 0f, Player.whoAmI);
                }
            }
            Player player = Main.player[proj.owner];
            int weaponDamage = player.HeldItem.damage;
            // 阿巴顿：暴击命中时在敌人中心炸出硫磺爆炸（与灾厄一致只在弹幕命中分支触发，真近战命中的 item 分支不触发）
            // 伤害 = 触发弹幕基础伤害的 3%，经软上限折算（上限 25）；15 帧冷却
            if (abaddon && hit.Crit && abaddonCritCooldown <= 0)
            {
                abaddonCritCooldown = 15;
                int abaddonDamage = DamageSoftCap(proj.damage * 0.03f, 25);
                Projectile.NewProjectile(Player.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<AbaddonCrit>(), abaddonDamage, 0f, Player.whoAmI);
            }
            //弑神飞镖
            if (godSlayerMelee && godSlayerMeleefireCD <= 0 && (proj.CountsAsClass<MeleeDamageClass>() || proj.CountsAsClass<MeleeNoSpeedDamageClass>()))
            {
                int finalDamage = 500 + weaponDamage / 2;
                Vector2 velocity = CDUtil.GiveVelocity(200f);
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, velocity * 4f, ModContent.ProjectileType<GodSlayerDart>(), finalDamage, 0f, Player.whoAmI);
                godSlayerMeleefireCD = 60;
            }
            if(holyWrath)
            {
                ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
            }
            if(armorShattering || armorCrumbling)
            {
                ApplyCalamityBuff(target, "CalamityMod", "ArmorCrunch", 240);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "ArmorCrunch", 240);
            }
        }
        /// <summary>
        /// tModLoader 的 PreKill 钩子：玩家即将死亡前调用，三支保命按源（经典版 PreKill）的顺序判定：
        /// 星云核心（1/10 概率，回复 100 点生命）→ 弑神者（龙之涌动时回满，否则关态 100 / 膨胀 300，并挂 45 秒冷却）
        /// → 始源林海（保至 1 点生命并开启 600 帧保护窗口，窗口内再致死每次扣 100 最大生命上限）。
        /// 任何一支生效都返回 false 取消本次死亡；三支都不满足时才走到替换死因文案。
        /// </summary>
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            // 三支保命的判定顺序与源一致（经典版 CalamityPlayerPreTrailer.PreKill：星云核心 → 弑神者 → 始源林海）。
            // 顺序有实际意义：金源套同时置 godSlayer 与 silvaSet，若把林海摆在前面，就会先触发"回到 1 点生命"
            // 的保护窗口，跳过弑神的回血与 45 秒冷却（以及冷却期的 +10% 伤害）。
            if (nebulousCore && Main.rand.NextBool(10))
            {
                SoundEngine.PlaySound(SoundID.Item67, Player.position);
                for (int j = 0; j < 25; j++)
                {
                    int num = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 2f);
                    Dust expr_A4_cp_0 = Main.dust[num];
                    expr_A4_cp_0.position.X = expr_A4_cp_0.position.X + (float)Main.rand.Next(-20, 21);
                    Dust expr_CB_cp_0 = Main.dust[num];
                    expr_CB_cp_0.position.Y = expr_CB_cp_0.position.Y + (float)Main.rand.Next(-20, 21);
                    Main.dust[num].velocity *= 0.9f;
                    Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    Main.dust[num].shader = GameShaders.Armor.GetSecondaryShader(Player.cWaist, Player);
                    if (Main.rand.NextBool(2))
                    {
                        Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    }
                }
                Player.statLife += 100;
                Player.HealEffect(100);
                if (Player.statLife > Player.statLifeMax2)
                {
                    Player.statLife = Player.statLifeMax2;
                }
                return false;
            }
            if (godSlayer && !godSlayerCooldown)
            {
                SoundEngine.PlaySound(SoundID.Item67, Player.position);
                for (int j = 0; j < 50; j++)
                {
                    int num = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default(Color), 2f);
                    Dust expr_A4_cp_0 = Main.dust[num];
                    expr_A4_cp_0.position.X = expr_A4_cp_0.position.X + (float)Main.rand.Next(-20, 21);
                    Dust expr_CB_cp_0 = Main.dust[num];
                    expr_CB_cp_0.position.Y = expr_CB_cp_0.position.Y + (float)Main.rand.Next(-20, 21);
                    Main.dust[num].velocity *= 0.9f;
                    Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    Main.dust[num].shader = GameShaders.Armor.GetSecondaryShader(Player.cWaist, Player);
                    if (Main.rand.NextBool(2))
                    {
                        Main.dust[num].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    }
                }
                // 保命回复量同样受数值膨胀开关门控：关态取 CI 源值 100，开态 300（用户 2026-10-05 定）。
                // 文案侧由两个弑神者头盔的 SetBonus 用 {1} 占位同步（GodSlayerHelm / GodSlayerHelmet）。
                int heal = draconicSurge ? Player.statLifeMax2 : (ConfigSystem.StatInflationEnabled ? 300 : 100);
                Player.statLife += heal;
                Player.HealEffect(heal);
                if (Player.statLife > Player.statLifeMax2)
                {
                    Player.statLife = Player.statLifeMax2;
                }
                if (Player.FindBuffIndex(ModContent.BuffType<DraconicSurgeBuff>()) > -1)
                {
                    Player.ClearBuff(ModContent.BuffType<DraconicSurgeBuff>());
                    draconicSurgeCooldown = 1800;
                }
                Player.AddBuff(ModContent.BuffType<GodSlayerCooldown>(), 2700);
                return false;
            }
            if (silvaSet && silvaCountdown > 0)
            {
                if (hasSilvaEffect)
                {
                    silvaHitCounter++;
                }
                if (Player.FindBuffIndex(ModContent.BuffType<SilvaRevival>()) == -1)
                {
                    SoundEngine.PlaySound(new SoundStyle("CalamityDemutation/Sounds/Custom/SilvaActivation"), Player.position);
                    Player.AddBuff(ModContent.BuffType<SilvaRevival>(), 600);
                    if (draconicSurge)
                    {
                        Player.statLife += Player.statLifeMax2;
                        Player.HealEffect(Player.statLifeMax2);
                        if (Player.statLife > Player.statLifeMax2)
                        {
                            Player.statLife = Player.statLifeMax2;
                        }
                        if (Player.FindBuffIndex(ModContent.BuffType<DraconicSurgeBuff>()) > -1)
                        {
                            Player.ClearBuff(ModContent.BuffType<DraconicSurgeBuff>());
                            draconicSurgeCooldown = 1800;
                        }
                    }
                }
                hasSilvaEffect = true;
                if (Player.statLife < 1)
                {
                    Player.statLife = 1;
                }
                return false;
            }
            if (hellfireExplosion)
            {
                damageSource = PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(Player.name + " was slain in hell."));
            }
            return true;
        }
        /// <summary>
        /// 永久解锁标志的持久化，共 6 项：extraAccessoryML 天界洋葱 / extraWingSlot 拜月契约
        /// / sugarheartCitrus / organicPod / freshBlueberry / moltenMagmaFruit（四件永久增益消耗品）；
        /// 其余字段每帧由装备重新计算，无需保存
        /// </summary>
        public override void SaveData(TagCompound tag)
        {
            tag["extraAccessoryML"] = extraAccessoryML;
            tag["extraWingSlot"] = extraWingSlot;
            // 四件永久增益消耗品
            tag["sugarheartCitrus"] = sugarheartCitrus;
            tag["organicPod"] = organicPod;
            tag["freshBlueberry"] = freshBlueberry;
            tag["moltenMagmaFruit"] = moltenMagmaFruit;
        }
        /// <summary>
        /// tModLoader 的 LoadData 钩子：读档时恢复永久解锁标志。
        /// 与 SaveData 严格对应，读取同样 6 个键。
        /// </summary>
        public override void LoadData(TagCompound tag)
        {
            extraAccessoryML = tag.GetBool("extraAccessoryML");
            extraWingSlot = tag.GetBool("extraWingSlot");
            sugarheartCitrus = tag.GetBool("sugarheartCitrus");
            organicPod = tag.GetBool("organicPod");
            freshBlueberry = tag.GetBool("freshBlueberry");
            moltenMagmaFruit = tag.GetBool("moltenMagmaFruit");
        }
        /// <summary>
        /// 联机时把本地玩家的 6 个永久解锁标志复制到基准副本，供 SendClientChanges 检测差异用。
        /// 只同步这 6 个标志；其余字段每帧由装备重算，无需跨端传输。
        /// </summary>
        public override void CopyClientState(ModPlayer targetCopy)
        {
            CalamityDemutationPlayer copy = (CalamityDemutationPlayer)targetCopy;
            copy.extraAccessoryML = extraAccessoryML;
            copy.extraWingSlot = extraWingSlot;
            copy.sugarheartCitrus = sugarheartCitrus;
            copy.organicPod = organicPod;
            copy.freshBlueberry = freshBlueberry;
            copy.moltenMagmaFruit = moltenMagmaFruit;
        }
        /// <summary>
        /// 客户端状态变更上报：当本地任一永久解锁标志相对基准副本变化（例如非主机端吃了洋葱，
        /// 服务器本身不知道）时，向服务器发一条带新标志的消息，由主类 HandlePacket
        /// 更新服务器上的玩家副本并广播 SyncPlayer 给所有端。
        /// </summary>
        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            CalamityDemutationPlayer old = (CalamityDemutationPlayer)clientPlayer;
            if (old.extraAccessoryML != extraAccessoryML || old.extraWingSlot != extraWingSlot
                || old.sugarheartCitrus != sugarheartCitrus || old.organicPod != organicPod
                || old.freshBlueberry != freshBlueberry || old.moltenMagmaFruit != moltenMagmaFruit)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)MsgPermanentUnlock);
                packet.Write(Player.whoAmI);
                packet.Write(extraAccessoryML);
                packet.Write(extraWingSlot);
                packet.Write(sugarheartCitrus);
                packet.Write(organicPod);
                packet.Write(freshBlueberry);
                packet.Write(moltenMagmaFruit);
                packet.Send();
            }
        }
        // ── 公开方法 ──
        /// <summary>
        /// 带冷却的回血（对应源的 <c>EModPlayer.TryHealMeWithCd</c>）：冷却未到时返回 false 且不回血，
        /// 可用时回血并把 <see cref="HealingCd"/> 重置为 <paramref name="cd"/> 帧。
        /// 冷却为玩家级、跨弹幕共享，用于限制「同一次爆发里多颗治疗弹幕」的回血次数。
        /// ⚠️ 触发者不论本方法返回真假都要置位自己的「已回过血」标记——源同此语义。
        /// <see cref="Player.Heal"/> 自带超上限夹取。
        /// </summary>
        public bool TryHealMeWithCd(int amount, int cd = 12)
        {
            if (HealingCd > 0)
                return false;
            HealingCd = cd;
            Player.Heal(amount);
            return true;
        }
        /// <summary>
        /// 施加灾厄模组 debuff：buff type 非法（对应模组/技能不存在）时静默跳过
        /// </summary>
        public static void ApplyCalamityBuff(Player target, string modName, string buffName, int duration)
        {
            int type = GetBuffType(modName, buffName);
            if (type > 0)
                target.AddBuff(type, duration, false);
        }
        /// <summary>
        /// NPC 版本：给命中目标（NPC）施加灾厄 debuff 的容错封装（OnHitNPC 命中怪物时用）。
        /// buff type 非法或对应模组缺该名时静默跳过。
        /// </summary>
        public static void ApplyCalamityBuff(NPC target, string modName, string buffName, int duration)
        {
            int type = GetBuffType(modName, buffName);
            if (type > 0)
                target.AddBuff(type, duration, false);
        }
        /// <summary>
        /// NPC 版「从简 + 近似替代」的灾厄减益施加：先试现代版、再试经典版，两版都没有时退回原版近似 buff。
        /// 供 Terratomere（GlacialState→Frostburn）、Excelsus（GodSlayerInferno→CursedInferno）等命中点使用。
        /// </summary>
        public static void ApplyCalamityBuffWithFallback(NPC target, string buffName, int duration, int fallbackBuff)
        {
            int type = GetBuffType("CalamityMod", buffName);
            if (type <= 0)
                type = GetBuffType("CalamityModClassicPreTrailer", buffName);
            if (type > 0)
                target.AddBuff(type, duration, false);
            else
                target.AddBuff(fallbackBuff, duration, false);
        }
        /// <summary>
        /// 玩家版「从简 + 近似替代」的灾厄减益施加（PvP 命中时用），语义同 NPC 版。
        /// </summary>
        public static void ApplyCalamityBuffWithFallback(Player target, string buffName, int duration, int fallbackBuff)
        {
            int type = GetBuffType("CalamityMod", buffName);
            if (type <= 0)
                type = GetBuffType("CalamityModClassicPreTrailer", buffName);
            if (type > 0)
                target.AddBuff(type, duration, false);
            else
                target.AddBuff(fallbackBuff, duration, false);
        }
        /// <summary>
        /// 检查玩家当前是否佩戴指定饰品（直接查 armor 饰品槽，而非 ModPlayer 标志位）。
        /// 供 PvP 命中钩子在受害者客户端上判断【攻击者】的装备，
        /// 避免远程玩家实例的标志位因 ResetEffects 只对本地运行而残留/不同步。
        /// </summary>
        public static bool IsAccessoryEquipped(Player player, int itemType)
        {
            // 饰品槽：armor[3..7]（基础 5 槽）+ 额外饰品槽
            for (int i = 3; i < 8 + player.extraAccessorySlots; i++)
            {
                if (player.armor[i].type == itemType)
                    return true;
            }
            return false;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 伤害软上限（内联灾厄 CalamityUtils.DamageSoftCap）：未超上限时原样返回；
        /// 超过后按 sqrt(超出倍数)/1.25 + 0.2 折算，抑制超模伤害线性堆叠。
        /// 使用者：阿巴顿暴击爆炸（上限 25）、弑神者射手套装破片弹（上限 1500，见 CalamityDemutationGlobalItem.Shoot）。
        /// </summary>
        internal static int DamageSoftCap(double dmgInput, int cap)
        {
            if (dmgInput < cap)
                return (int)dmgInput;
            double overpoweredRatio = dmgInput / cap;
            double cappedRatio = Math.Pow(overpoweredRatio, 0.5) / 1.25 + 0.2;
            return (int)(cap * cappedRatio);
        }
        /// <summary>
        /// 移植自灾厄的 areThereAnyDamnBosses（影之再生据此选择回血速率）：判定场上是否正有 Boss 存活。
        /// 口径取灾厄两版本 AnyBossNPCS() 的并集——npc.boss（排除日耀飞碟核心）、世界吞噬者的头/身/尾，
        /// 以及灾厄未置 boss 标记的史莱姆神分身（现代版 EbonianPaladin/CrimulanPaladin/Split*，
        /// 经典版 SlimeGodRun/SlimeGodRunSplit/SlimeGod/SlimeGodSplit）。
        /// 分身类型按名字软依赖解析一次并缓存；灾厄未安装或改名时该项自动跳过，不影响其余判定。
        /// </summary>
        private static bool AnyBossNPCS()
        {
            if (extraBossTypes == null)
            {
                extraBossTypes = new HashSet<int>();
                void TryAddBoss(string modName, string npcName)
                {
                    if (ModContent.TryFind(modName, npcName, out ModNPC npc))
                        extraBossTypes.Add(npc.Type);
                }
                TryAddBoss("CalamityMod", "EbonianPaladin");
                TryAddBoss("CalamityMod", "CrimulanPaladin");
                TryAddBoss("CalamityMod", "SplitEbonianPaladin");
                TryAddBoss("CalamityMod", "SplitCrimulanPaladin");
                TryAddBoss("CalamityModClassicPreTrailer", "SlimeGodRun");
                TryAddBoss("CalamityModClassicPreTrailer", "SlimeGodRunSplit");
                TryAddBoss("CalamityModClassicPreTrailer", "SlimeGod");
                TryAddBoss("CalamityModClassicPreTrailer", "SlimeGodSplit");
            }
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.type == NPCID.MartianSaucerCore)
                    continue;
                if (npc.boss || extraBossTypes.Contains(npc.type))
                    return true;
                if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 统计 CommunityBosses 中已击败的 Boss 数量。
        /// </summary>
        private static int CommunityBossCount()
        {
            int count = 0;
            foreach (Func<bool> downed in CommunityBosses)
                if (downed())
                    count++;
            return count;
        }
        /// <summary>
        /// 解析"模组名 + buff 名"为 buff type，结果缓存在 buffTypeCache 中。
        /// 模组或技能不存在时静默返回 0（上层据此跳过施加），不抛异常。
        /// </summary>
        private static int GetBuffType(string modName, string buffName)
        {
            if (buffTypeCache.TryGetValue((modName, buffName), out int cached))
                return cached;
            int type = 0;
            if (ModLoader.TryGetMod(modName, out Mod mod) && mod.TryFind<ModBuff>(buffName, out ModBuff buff))
                type = buff.Type;
            buffTypeCache[(modName, buffName)] = type;
            return type;
        }
        /// <summary>
        /// 给玩家添加对指定灾厄 buff 的免疫（水晶态属性块用）。
        /// 走 GetBuffType 的缓存软依赖，对应模组缺该名（type == 0）时静默跳过，避免误置 buffImmune[0]
        /// </summary>
        private static void AddCalamityBuffImmune(Player player, string modName, string buffName)
        {
            int type = GetBuffType(modName, buffName);
            if (type > 0)
                player.buffImmune[type] = true;
        }
        /// <summary>
        /// 判断指定 buff 是否在 The Community 的 Debuff 缩减黑名单内。
        /// 覆盖本模组 Enraged，以及灾厄现代版/经典版的肾上腺素（AdrenalineMode）、怒气（RageMode）、
        /// Enraged。对应 buff 不存在时 GetBuffType 返回 0（无害，循环中 buffType&gt;0 不会命中）。
        /// </summary>
        private static bool IsBuffInCommunityBlacklist(int buffType)
        {
            if (communityDebuffBlacklist == null)
            {
                communityDebuffBlacklist =
                [
                    ModContent.BuffType<Enraged>(),                                  // 本模组 Enraged
                    GetBuffType("CalamityMod", "AdrenalineMode"),                   // 灾厄现代版 肾上腺素
                    GetBuffType("CalamityMod", "RageMode"),                         // 灾厄现代版 怒气
                    GetBuffType("CalamityMod", "Enraged"),                          // 灾厄现代版 Enraged
                    GetBuffType("CalamityModClassicPreTrailer", "AdrenalineMode"),  // 灾厄经典版 肾上腺素
                    GetBuffType("CalamityModClassicPreTrailer", "RageMode"),        // 灾厄经典版 怒气
                    GetBuffType("CalamityModClassicPreTrailer", "Enraged"),         // 灾厄经典版 Enraged
                ];
            }
            return communityDebuffBlacklist.Contains(buffType);
        }
    }
}
