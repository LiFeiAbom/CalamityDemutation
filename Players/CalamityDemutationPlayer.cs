using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Buffs.PositiveBuffs;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Projectiles.Magic;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Content.Projectiles.Typeless;
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
    /// 另含天界洋葱/翅膀洋葱的永久解锁标志（extraAccessoryML / extraWingSlot），
    /// 并由 SaveData / LoadData 负责这两个字段的持久化。
    /// </summary>
    internal class CalamityDemutationPlayer : ModPlayer
    {
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
        public bool bloodflareSet = false;
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
        /// <summary>
        /// 硫磺娘（大胸玫瑰 BigBustyRose）仆从在场标记：由 BrimstoneWaifu 召唤增益每帧置位
        /// </summary>
        public bool brimstoneWaifu = false;
        /// <summary>
        /// 按"模组名/技能名"缓存 buff type，避免每次命中都 TryGetMod + Find。
        /// Mod.BuffType 找不到返回 0（不抛异常），比 Find&lt;ModBuff&gt; 更抗灾厄版本变化。
        /// </summary>
        private static readonly Dictionary<(string mod, string buff), int> buffTypeCache = new();
        /// <summary>
        /// 已装备灾厄之戒：+15% 通用伤害，免疫受击期间概率在玩家附近降下站火弹幕
        /// </summary>
        public bool calamityRing = false;
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
        public bool demonshadeSetBonus = false;
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
        /// <summary>
        /// 翅膀洋葱已使用（永久开启一个专用翅膀饰品栏）
        /// </summary>
        public bool extraWingSlot = false;
        /// <summary>
        /// 已装备风暴之眼（召唤饰品）：置位后维持云娘仆从存在并允许其存活
        /// </summary>
        public bool eyeoftheStorm = false;
        /// <summary>
        /// 地狱火齐射的总扇形散开角度（单位：度），120 度均分给 FireProjectiles 发弹幕
        /// </summary>
        public const float FireAngleSpread = 120;
        /// <summary>
        /// 单次地狱火齐射的弹幕数量（4 发），与 FireAngleSpread 配合铺开扇形
        /// </summary>
        public const int FireProjectiles = 4;
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
        public bool godSlayerReflect = false;
        public float godSlayerDamage;
        public bool godSlayerDamageProtect = false;
        public int godSlayerDamageProtectMax = 80;
        /// <summary>
        /// 已装备大凝胶：移速/跳跃提升、+20 生命与魔力上限，静止时额外回血回蓝
        /// </summary>
        public bool grandGelatin = false;
        public bool hasSilvaEffect = false;
        /// <summary>
        /// 已装备元素之心：综合生命/魔力/移速/减伤/通用增伤/暴击增益，
        /// 脚底自动生长草/花，并作为五娘化饰品的合集核心（置 allWaifus）
        /// </summary>
        public bool heartoftheElements = false;
        /// <summary>
        /// 元素之心"隐藏视觉"版标记（数值约为完整版一半）：由 HeartoftheElements.cs 依据配置置位
        /// </summary>
        public bool heartoftheElementshideVisual = false;
        /// <summary>
        /// 已装备蜜露：丛林区获得回血/防御/减伤，免疫蜂蜜与中毒，并附加蜂蜜式生命回复
        /// </summary>
        public bool honeyDew = false;
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
        /// 已装备魅惑之饵（召唤饰品）：置位后维持塞壬娘仆从存在并允许其存活
        /// </summary>
        public bool lureofEnthrallment = false;
        /// <summary>
        /// 已装备魔力凝胶：+20 魔力上限，静止不动时额外魔力回复
        /// </summary>
        public bool manaJelly = false;
        public static int MinionsAddition = 1;
        public float modStealth = 1f;
        public int modStealthTimer;
        /// <summary>
        /// 星云核心已装备（+20%通用伤害/暴击，20%概率免死并回复100生命）
        /// </summary>
        public bool nebulousCore = false;
        /// <summary>
        /// 烦恼项链已装备（通用伤害+5%，半血以下额外+15%）
        /// </summary>
        public bool necklaceOfVexation = false;
        public bool omegaBlueChestplate = false;
        public bool omegaBlueSet = false;
        public bool omegaBlueHentai = false;
        public int omegaBlueCooldown = 0;
        public bool psychoticAmulet = false;
        /// <summary>
        /// 已装备辐射软泥：夜间发出暖黄光并提供生命回复
        /// </summary>
        public bool radiantOoze = false;
        public bool rampartofDeities = false;
        public bool redDevil = false;
        public bool redDevil2 = false;
        /// <summary>
        /// 已装备玫瑰石：生命回复/上限、+3% 通用增伤与粉色照明（同时驱动玫瑰娘召唤物）
        /// </summary>
        public bool roseStone = false;
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
        public bool shadowSpeed = false;
        /// <summary>
        /// 龟壳爆发（ShellBoost 正面增益）激活标记：受击后置位，提供 +90% 移速
        /// </summary>
        public bool shellBoost = false;
        /// <summary>
        /// 已装备灾厄符印：+15% 魔法伤害/+10% 魔法暴击、+100 魔力上限、魔力消耗 ×0.85，
        /// 附带寻宝与药剂效果
        /// </summary>
        public bool sigilofCalamitas = false;
        public int silvaCountdown = 600;
        public int silvaHitCounter = 0;
        public bool silvaMelee = false;
        public bool silvaSet = false;
        /// <summary>
        /// 塞壬娘（SirenLure）仆从在场标记：由 SirenLure 召唤增益每帧置位，受魅惑之饵驱动
        /// </summary>
        public bool sirenLureWaifu = false;
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
        public bool tarraSet = false;
        /// <summary>
        /// 已装备吞噬者（The Absorber）：综合生命/魔力/移速/荆棘/减伤/静止回复，
        /// 浸水增益、受击回血并触发龟壳爆发
        /// </summary>
        public bool theAbsorber = false;
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
        // ===== The Community 属性成长（Boss 进度驱动）=====
        /// <summary>
        /// The Community 进度 Boss 清单（石巨人 → 至尊灾厄，18 档），与庇护之刃 LegendaryBosses 同序。
        /// 仅用于统计"已击败几档"来驱动各属性的线性成长；召唤栏/飞行等一次性加成另行按 Boss 判定。
        /// </summary>
        private static readonly Func<bool>[] CommunityBosses =
        [
            () => NPC.downedGolemBoss,                               // 石巨人 Golem
            () => CalamityDemulationBossSystem.Plaguebringer,        // 瘟疫使者歌莉娅 Plaguebringer Goliath
            () => CalamityDemulationBossSystem.Ravager,              // 毁灭魔像（掠夺者）Ravager
            () => NPC.downedAncientCultist,                          // 拜月教邪教徒 Lunatic Cultist
            () => CalamityDemulationBossSystem.AstrumDeus,           // 星神游龙 Astrum Deus
            () => NPC.downedMoonlord,                                // 月球领主 Moon Lord
            () => CalamityDemulationBossSystem.Guardians,            // 亵渎守卫 Profaned Guardians
            () => CalamityDemulationBossSystem.Dragonfolly,          // 丛林龙 Dragonfolly
            () => CalamityDemulationBossSystem.Providence,           // 亵渎天神 Providence
            () => CalamityDemulationBossSystem.CeaselessVoid || ClassicSentinelsDowned, // 无尽虚空 Ceaseless Void
            () => CalamityDemulationBossSystem.StormWeaver || ClassicSentinelsDowned,   // 风暴编织者 Storm Weaver
            () => CalamityDemulationBossSystem.Signus || ClassicSentinelsDowned,        // 西格纳斯 Signus
            () => CalamityDemulationBossSystem.Polterghast,          // 噬魂幽花 Polterghast
            () => CalamityDemulationBossSystem.OldDuke,              // 硫海遗爵（老公爵）Old Duke
            () => CalamityDemulationBossSystem.DevourerOfGods,       // 噬神者 Devourer of Gods
            () => CalamityDemulationBossSystem.Yharon,               // 犽戎 Yharon
            () => CalamityDemulationBossSystem.ExoMechs,             // 星流巨械 Exo Mechs
            () => CalamityDemulationBossSystem.SupremeCalamitas,     // 至尊灾厄 Supreme Calamitas
        ];
        /// <summary>
        /// 经典版三使者是否全部倒下（经典版无单个使者标记，只有 Sentinel1/2/3）。
        /// </summary>
        private static bool ClassicSentinelsDowned =>
            CalamityDemulationBossSystem.Sentinel1 && CalamityDemulationBossSystem.Sentinel2 && CalamityDemulationBossSystem.Sentinel3;
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
        // 各属性成长端点：初始值（击败石巨人时）→ 满配值（18 档全清）
        private const float InitDamage = 0.10f, MaxDamage = 0.30f;           // 通用增伤（0.10 = +10%）
        private const float InitCrit = 10f, MaxCrit = 30f;                   // 暴击（百分点：10 = +10%）
        private const float InitMeleeSpeed = 0.05f, MaxMeleeSpeed = 0.30f;   // 近战攻速（0.05 = +5%）
        private const float InitEndurance = 0.05f, MaxEndurance = 0.15f;     // 伤害减免（0.05 = +5%）
        private const float InitMoveSpeed = 0.05f, MaxMoveSpeed = 0.30f;     // 移速（0.05 = +5%）
        private const float InitLifePct = 0.05f, MaxLifePct = 0.30f;         // 最大生命（百分比）
        private const float InitManaPct = 0.05f, MaxManaPct = 0.30f;         // 最大魔力（百分比）
        private const int InitLifeRegen = 1, MaxLifeRegen = 10;              // 回血
        private const int InitManaRegen = 1, MaxManaRegen = 10;              // 回蓝
        private const int InitDefense = 5, MaxDefense = 30;                  // 防御
        private const float InitJumpPct = 0.05f, MaxJumpPct = 0.30f;         // 跳跃力（0.05 = +5%）
        private const float InitMinePct = 0.05f, MaxMinePct = 0.30f;         // 挖掘速度（0.05 = +5%）
        private const float InitCostPct = 0.05f, MaxCostPct = 0.30f;         // 魔力消耗减免（0.05 = -5%）
        private const float InitThornsPct = 0.05f, MaxThornsPct = 0.30f;     // 荆棘反伤（0.05 = 5%）
        private const float InitLuckPct = 0.05f, MaxLuckPct = 0.30f;         // 幸运（0.05 = +0.05）
        /// <summary>
        /// The Community 的 Debuff 缩减黑名单缓存：懒加载一次后复用。
        /// 这些 buff 虽被标为 debuff（Main.debuff=true），实为增益/特殊状态，不应被缩短持续时间。
        /// </summary>
        private static HashSet<int> communityDebuffBlacklist;
        /// <summary>
        /// 判断指定 buff 是否在 The Community 的 Debuff 缩减黑名单内。
        /// 覆盖本模组 Enraged，以及灾厄现代版/经典版的肾上腺素（AdrenalineMode）、怒气（RageMode）、
        /// Enraged。对应 buff 不存在时 GetBuffType 返回 0（无害，循环中 buffType&gt;0 不会命中）。
        /// </summary>
        private static bool IsBuffInCommunityBlacklist(int buffType)
        {
            if (communityDebuffBlacklist == null)
            {
                communityDebuffBlacklist = new HashSet<int>
                {
                    ModContent.BuffType<Enraged>(),                                  // 本模组 Enraged
                    GetBuffType("CalamityMod", "AdrenalineMode"),                   // 灾厄现代版 肾上腺素
                    GetBuffType("CalamityMod", "RageMode"),                         // 灾厄现代版 怒气
                    GetBuffType("CalamityMod", "Enraged"),                          // 灾厄现代版 Enraged
                    GetBuffType("CalamityModClassicPreTrailer", "AdrenalineMode"),  // 灾厄经典版 肾上腺素
                    GetBuffType("CalamityModClassicPreTrailer", "RageMode"),        // 灾厄经典版 怒气
                    GetBuffType("CalamityModClassicPreTrailer", "Enraged"),         // 灾厄经典版 Enraged
                };
            }
            return communityDebuffBlacklist.Contains(buffType);
        }
        /// <summary>
        /// 已装备最初暗影焰（召唤饰品）：召唤物命中敌人时施加 5 秒暗影焰
        /// </summary>
        public bool theFirstShadowflame = false;
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
        /// <summary>
        /// 每帧重置所有饰品开关标记（由各饰品的 UpdateAccessory 重新置位）
        /// </summary>
        public override void ResetEffects()
        {
            aeroStone = false;
            afflicted = false;
            affliction = false;
            allWaifus = false;
            amalgamatedBrain = false;
            ambrosialAmpoule = false;
            amidiasSpark = false;
            ancientFossil = false;
            archaicPowder = false;
            auricBoost = false;
            auricSet = false;
            beeResist = false;
            bloodflareCore = false;
            bloodflareMelee = false;
            bloodflareSet = false;
            bloomStone = false;
            badgeOfBravery = false;
            bloodPact = false;
            bloodyWormScarf = false;
            bloodyWormTooth = false;
            brimstoneWaifu = false;
            calamityRing = false;
            chaosStone = false;
            cloudWaifu = false;
            coreOfTheBloodGod = false;
            corruptFlask = false;
            crawCarapace = false;
            crimsonFlask = false;
            cryoStone = false;
            daedalusEmblem = false;
            darkSunRing = false;
            deificAmulet = false;
            demonshadeSetBonus = false;
            drewsSandyWaifu = false;
            elementalGauntlet = false;
            elementalQuiver = false;
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
            godSlayerDamageProtect = false;
            godSlayerMelee = false;
            godSlayerReflect = false;
            grandGelatin = false;
            heartoftheElements = false;
            heartoftheElementshideVisual = false;
            honeyDew = false;
            levianthanAmbergris = false;
            lifeJelly = false;
            livingDew = false;
            lureofEnthrallment = false;
            manaJelly = false;
            nebulousCore = false;
            necklaceOfVexation = false;
            omegaBlueChestplate = false;
            omegaBlueSet = false;
            omegaBlueHentai = false;
            psychoticAmulet = false;
            radiantOoze = false;
            rampartofDeities = false;
            redDevil = false;
            redDevil2 = false;
            roseStone = false;
            rottenBrain = false;
            sandyWaifu = false;
            seaShell = false;
            shellBoost = false;
            shadeRegen = false;
            shadowSpeed = false;
            sigilofCalamitas = false;
            silvaMelee = false;
            silvaSet = false;
            sirenLureWaifu = false;
            sponge = false;
            statisBlessing = false;
            statisBeltOfCurses = false;
            statisCurse = false;
            tarraLifeRegen = false;
            tarraMelee = false;
            tarraSet = false;
            theAmalgam = false;
            theAbsorber = false;
            theCommunity = false;
            theFirstShadowflame = false;
            vitalJelly = false;
            voidofExtinction = false;
            wifeinaBottle = false;
            wifeinaBottlewithBoobs = false;
            yharimsInsignia = false;
            yharimPower = false;
        }
        /// <summary>
        /// 玩家死亡时清空所有饰品标记与冷却
        /// </summary>
        public override void UpdateDead()
        {
            aeroStone = false;
            afflicted = false;
            affliction = false;
            allWaifus = false;
            amalgamatedBrain = false;
            ambrosialAmpoule = false;
            amidiasSpark = false;
            ancientFossil = false;
            archaicPowder = false;
            auricBoost = false;
            auricSet = false;
            beeResist = false;
            bloodflareCore = false;
            bloodflareFrenzyTimer = 0;
            bloodflareFrenzyCooldown = 0;
            bloodflareHeartTimer = 0;
            bloodflareMelee = false;
            bloodflareManaTimer = 0;
            bloodflareMeleeHits = 0;
            bloodflareSet = false;
            bloomStone = false;
            badgeOfBravery = false;
            bloodPact = false;
            bloodyWormScarf = false;
            bloodyWormTooth = false;
            brimstoneWaifu = false;
            calamityRing = false;
            chaosStone = false;
            cloudWaifu = false;
            coreOfTheBloodGod = false;
            corruptFlask = false;
            crawCarapace = false;
            crimsonFlask = false;
            cryoStone = false;
            daedalusEmblem = false;
            darkSunRing = false;
            deificAmulet = false;
            demonshadeSetBonus = false;
            drewsSandyWaifu = false;
            elementalGauntlet = false;
            elementalQuiver = false;
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
            godSlayerCooldown = false;;
            godSlayerDamageProtect = false;
            godSlayerMelee = false;
            godSlayerReflect = false;
            grandGelatin = false;
            hasSilvaEffect = false;
            heartoftheElements = false;
            heartoftheElementshideVisual = false;
            honeyDew = false;
            levianthanAmbergris = false;
            lifeJelly = false;
            livingDew = false;
            lureofEnthrallment = false;
            manaJelly = false;
            nebulousCore = false;
            necklaceOfVexation = false;
            omegaBlueChestplate = false;
            omegaBlueSet = false;
            omegaBlueCooldown = 0;
            psychoticAmulet = false;
            radiantOoze = false;
            rampartofDeities = false;
            redDevil = false;
            redDevil2 = false;
            roseStone = false;
            rottenBrain = false;
            sandyWaifu = false;
            seaShell = false;
            shadeRegen = false;
            shadowSpeed = false;
            shellBoost = false;
            sigilofCalamitas = false;
            silvaCountdown = 600;
            silvaHitCounter = 0;
            silvaMelee = false;
            silvaSet = false;
            sirenLureWaifu = false;
            sponge = false;
            statisBlessing = false;
            statisBeltOfCurses = false;
            statisCurse = false;
            tarraCooldown = 0;
            tarraDefense = false;
            tarraDefenseTime = 0;
            tarraLifeRegen = false;
            tarraMelee = false;
            tarraSet = false;
            theAmalgam = false;
            theAbsorber = false;
            theCommunity = false;
            theFirstShadowflame = false;
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
        /// 女巫套装（silvaSet）+5%，同时乘算 runAcceleration 与 maxRunSpeed。
        /// 若开启"回退原版削弱"且安装现代版灾厄，再补偿灾厄对暗影护甲与腾飞徽章移动属性的削弱。
        /// </summary>
        public override void PostUpdateRunSpeeds()
        {
            float runAccMult = 1f + (shadowSpeed ? 0.5f : 0f) + (silvaSet ? 0.05f : 0f) + (auricSet ? 0.1f : 0f);
            float runSpeedMult = 1f + (shadowSpeed ? 0.5f : 0f) + (silvaSet ? 0.05f : 0f) + (auricSet ? 0.1f : 0f);
            Player.runAcceleration *= runAccMult;
            Player.maxRunSpeed *= runSpeedMult;
            // 回退灾厄对原版移动的削弱（近似补偿）
            if (CalamityDemutationConfigSystem.Instance?.RevertVanillaNerfs == true && ModLoader.HasMod("CalamityMod"))
            {
                // 暗影护甲：灾厄把移动加成从 1.75/1.15/1.15/1.75 削弱成 1.25/1.05/1.05/1.5，这里补回
                if (Player.shadowArmor && !(Player.hasMagiluminescence && Player.velocity.Y == 0))
                {
                    Player.runAcceleration *= 1.75f / 1.25f;
                    Player.maxRunSpeed *= 1.15f / 1.05f;
                    Player.accRunSpeed *= 1.15f / 1.05f;
                    Player.runSlowdown *= 1.75f / 1.5f;
                }
                // 腾飞徽章：灾厄把 run acceleration 从 1.75 削弱成 1.25
                if (Player.empressBrooch)
                    Player.runAcceleration *= 1.75f / 1.25f;
            }
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
            // 龟壳爆发（ShellBoost）：受击后增益期间 +90% 移速。
            // 原结算于 UpdateBadLifeRegen（该钩子仅在负面生命回复期运行，常漏加），
            // 改到本方法（每帧全饰品结算中心）保证增益期全程生效。
            if (shellBoost)
            {
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
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.05f;
            }
            // 血蠕虫围巾：+10% 近战伤害、+10% 近战攻速、+15% 伤害减免
            if(bloodyWormScarf)
            {
                Player.GetDamage<MeleeDamageClass>() += 0.1f;
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
                Player.endurance += 0.15f;
            }
            // 血蠕虫牙：半血以下效果翻倍（+10% vs +5%）
            if(bloodyWormTooth)
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
                Player.GetDamage<GenericDamageClass>() += 0.05f;
                // 半血判定：statLife 为当前生命，statLifeMax2 为加成后的最大生命
                if (Player.statLife <= 0.5 * Player.statLifeMax2)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.15f;
                }
            }
            // 亚利姆徽章：+10%近战伤害、+10%近战暴击率、+10%近战攻速、烈火手套击退、+240熔岩免疫时间，半血以下额外+10%通用伤害
            if (yharimsInsignia)
            {
                Player.GetDamage<MeleeDamageClass>() += 0.1f;
                Player.GetCritChance<MeleeDamageClass>() += 10;
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
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
            if(daedalusEmblem)
            {
                Player.GetDamage<RangedDamageClass>() += 0.15f;
                Player.GetCritChance<RangedDamageClass>() += 10;
                Player.lifeRegen += 2;
                Player.GetKnockback<RangedDamageClass>().Base += 0.5f;
                Player.pickSpeed -= 0.15f;
            }
            // 元素箭袋：远程增伤/暴击/回血/击退/挖速（不耗弹见 CanConsumeAmmo）
            if(elementalQuiver)
            {
                Player.GetDamage<RangedDamageClass>() += 0.2f;
                Player.GetCritChance<RangedDamageClass>() += 20;
                Player.lifeRegen += 4;
                Player.GetKnockback<RangedDamageClass>().Base += 1f;
                Player.pickSpeed -= 0.3f;
            }
            // 灾厄符印：魔法增伤/暴击/魔力上限与减耗，附带寻宝/药剂
            if(sigilofCalamitas)
            {
                Player.GetDamage<MagicDamageClass>() += 0.15f;
                Player.GetCritChance<MagicDamageClass>() += 10;
                Player.statManaMax2 += 100;
                Player.manaCost *= 0.85f;
                Player.findTreasure = true;
                Player.pStone = true;
            }
            // 虚灵护符：魔法增伤/暴击/魔力上限与减耗，附带寻宝/药剂/魔力花
            if(etherealTalisman)
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
            if(statisBlessing)
            {
                Player.GetKnockback<SummonDamageClass>().Base += 2.5f;
                Player.GetDamage<SummonDamageClass>() += 0.1f;
                Player.maxMinions += 3;
            }
            // 时滞诅咒：召唤增伤/击退 + 3 召唤栏，鞭子范围与召唤近战攻速提升
            if(statisCurse)
            {
                Player.GetKnockback<SummonDamageClass>().Base += 2.5f;
                Player.GetDamage<SummonDamageClass>() += 0.1f;
                Player.maxMinions += 3;
                Player.whipRangeMultiplier += 0.1f;
                Player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.1f;
            }
            // 时滞诅咒腰带：召唤增伤/栏位 + 鞭子范围，外加移动/跳跃/闪避/冲刺
            if(statisBeltOfCurses)
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
                Player.dash = 1;
                Player.spikedBoots = 2;
            }
            // 暗日之戒：召唤栏/通用增伤/近战攻速/暴击/挖速；白昼回血、夜晚加防
            if(darkSunRing)
            {
                Player.maxMinions += 2;
                Player.GetDamage<GenericDamageClass>() += 0.12f;
                Player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
                Player.GetCritChance<GenericDamageClass>() += 5;
                Player.pickSpeed -= 0.15f;
                if (Main.dayTime)
                {
                    Player.lifeRegen += 6;
                }
                else
                {
                    Player.statDefense += 30;
                }
            }
            // 星云核心：+20% 通用增伤/暴击（免死回血见 PreKill）
            if(nebulousCore)
            {
                Player.GetDamage<GenericDamageClass>() += 0.2f;
                Player.GetCritChance<GenericDamageClass>() += 20;
            }
            // 亚利姆之力：通用/近战/召唤全面增伤 + 生存属性大礼包
            if(yharimPower)
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
            if(afflicted || affliction)
            {
                Player.noKnockback = true;
                Player.GetDamage<GenericDamageClass>() += 0.15f;
                Player.statDefense += 45;
                Player.endurance += 0.08f;
                Player.statLifeMax2 += (int)(Player.statLifeMax2 * 0.2);
                Player.lifeRegen += 8;
            }
            // 苦难饰品附加光环：每 10 帧向同队（非本人）玩家刷一次 Afflicted 增益
            if(affliction && Player.miscCounter % 10 == 0)
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
            if(roseStone)
            {
                Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0.6f, 0f, 0.25f);
                Player.lifeRegen += 2;
                Player.statLifeMax2 += 20;
                Player.GetDamage<GenericDamageClass>() += 0.03f;
            }
            // 风之石：青色照明 + 移速/跳跃提升 + 3% 通用增伤
            if(aeroStone)
            {
                Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0f, 0.425f, 0.425f);
                Player.moveSpeed += 0.1f;
                Player.jumpSpeedBoost += 2.0f;
                Player.GetDamage<GenericDamageClass>() += 0.03f;
            }
            // 寒晶石：蓝色照明 + 减伤 + 3% 通用增伤
            if(cryoStone)
            {
                Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0f, 0.25f, 0.6f);
                Player.endurance += 0.05f;
                Player.GetDamage<GenericDamageClass>() += 0.03f;
            }
            // 混沌石：红色照明 + 魔力上限/减耗 + 3% 通用增伤
            if(chaosStone)
            {
                Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 0.85f, 0f, 0f);
                Player.statManaMax2 += 50;
                Player.manaCost *= 0.95f;
                Player.GetDamage<GenericDamageClass>() += 0.03f;
            }
            // 绽放之石：绿色照明 + 少量通用增伤/暴击；光环给周围敌人上 debuff，
            // 站立地面时还会让脚下空砖长出草/花/染料植物（装饰）
            if(bloomStone)
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
            if(heartoftheElements)
            {
                Player.lifeRegen += 6;
                Player.manaRegen += 6;
                Player.statLifeMax2 += 50;
                Player.statManaMax2 += 50;
                Player.moveSpeed += 0.1f;
                Player.jumpSpeedBoost += 2.0f;
                Player.endurance += 0.05f;
                Player.manaCost *= 0.85f;
                Player.GetDamage<GenericDamageClass>() += 0.1f;
                Player.GetCritChance<GenericDamageClass>() += 1f;
                // 隐藏视觉版元素之心：数值约为完整版一半，仅在未显示外观时叠加
                if (heartoftheElementshideVisual)
                {
                    Player.lifeRegen += 2;
                    Player.manaRegen += 2;
                    Player.statLifeMax2 += 20;
                    Player.statManaMax2 += 20;
                    Player.moveSpeed += 0.04f;
                    Player.jumpSpeedBoost += 0.5f;
                    Player.endurance += 0.02f;
                    Player.manaCost *= 0.9f;
                    Player.GetDamage<GenericDamageClass>() += 0.02f;
                    Player.GetCritChance<GenericDamageClass>() += 2;
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
            // 腐化烧瓶：腐化之地内 +3 防御与 +7% 减伤
            if (corruptFlask)
            {
                if (Player.ZoneCorrupt)
                {
                    Player.statDefense += 3;
                    Player.endurance += 0.07f;
                }
            }
            // 猩红烧瓶：猩红之地内 +3 防御与 +7% 减伤
            if (crimsonFlask)
            {
                if (Player.ZoneCrimson)
                {
                    Player.statDefense += 3;
                    Player.endurance += 0.07f;
                }
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
            if(archaicPowder)
            {
                if (Player.ZoneDirtLayerHeight || Player.ZoneRockLayerHeight || Player.ZoneUnderworldHeight)
                {
                    Player.statDefense += 3;
                    Player.endurance += 0.07f;
                    Player.pickSpeed -= 0.5f;
                }
            }
            // 辐射软泥：夜间暖黄照明 + 生命回复
            if(radiantOoze)
            {
                if (!Main.dayTime)
                {
                    Lighting.AddLight((int)(Player.position.X + (float)(Player.width / 2)) / 16, (int)(Player.position.Y + (float)(Player.height / 2)) / 16, 1f, 1f, 0.6f);
                    Player.lifeRegen += 2;
                }
            }
            // 蜜露：丛林区回血/防御/减伤；免疫蜂蜜与中毒，额外附加蜂蜜式回复
            if(honeyDew)
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
            if(livingDew)
            {
                if (Player.ZoneJungle)
                {
                    Player.lifeRegen += 2;
                    Player.statDefense += 5;
                    Player.endurance += 0.1f;
                }
            }
            // 仙馐药瓶：减伤/挖速/回血，免疫冰系与毒系 debuff，蜂蜜式回复
            if(ambrosialAmpoule)
            {
                Player.endurance += 0.12f;
                Player.pickSpeed -= 0.5f;
                Lighting.AddLight((int)(Player.position.X + (float)(Player.width / 2)) / 16, (int)(Player.position.Y + (float)(Player.height / 2)) / 16, 1f, 1f, 0.6f);
                Player.lifeRegen += 4;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
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
            if(manaJelly)
            {
                Player.statManaMax2 += 20;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.manaRegenBonus += 2;
                }
            }
            // 生命凝胶：+20 生命上限，静止时额外回血
            if(lifeJelly)
            {
                Player.statLifeMax2 += 20;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.lifeRegen += 2;
                }
            }
            // 活力凝胶：移速/跳跃提升
            if(vitalJelly)
            {
                Player.moveSpeed += 0.1f;
                Player.jumpSpeedBoost += 1.0f;
            }
            // 大凝胶：移速/跳跃/生命/魔力上限，静止时回血回蓝
            if(grandGelatin)
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
            if(seaShell)
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
            if(crawCarapace)
            {
                Player.endurance += 0.05f;
                Player.thorns = 0.25f;
            }
            // 巨型陆龟壳：减速 + 荆棘反伤
            if(giantTortoiseShell)
            {
                Player.moveSpeed -= 0.1f;
                Player.thorns = 0.25f;
            }
            // 吞噬者：生命/魔力/移速/荆棘/减伤/静止回复 + 浸水增益
            if(theAbsorber)
            {
                Player.statLifeMax2 += 30;
                Player.statManaMax2 += 30;
                Player.moveSpeed += 0.12f;
                Player.jumpSpeedBoost += 1.2f;
                Player.thorns = 0.5f;
                Player.endurance += 0.06f;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.lifeRegen += 2;
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
            if(giantShell)
            {
                Player.moveSpeed -= 0.15f;
            }
            // 海绵：大量生存属性/静止回复/溺水免疫，受击反制见 ModifyHurt/PostHurt
            if(sponge)
            {
                Player.lifeRegen += 6;
                Player.endurance += 0.18f;
                Player.statLifeMax2 += 50;
                Player.statManaMax2 += 50;
                Player.moveSpeed += 0.12f;
                Player.jumpSpeedBoost += 1.2f;
                Player.thorns = 0.5f;
                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
                {
                    Player.lifeRegen += 2;
                    Player.manaRegenBonus += 2;
                }
                if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
                {
                    Player.statDefense += 10;
                    Player.endurance += 0.1f;
                    Player.moveSpeed += 0.2f;
                    Player.ignoreWater = true;
                }
                Player.pickSpeed -= 0.5f;
                Lighting.AddLight((int)(Player.position.X + (float)(Player.width / 2)) / 16, (int)(Player.position.Y + (float)(Player.height / 2)) / 16, 1f, 1f, 0.6f);
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
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
            if(rottenBrain)
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
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<AuraRain>(), 90, 2f, Player.whoAmI, 0f, 0f);//18*5
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
            if(amalgamatedBrain)
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
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<AuraRain>(), 160, 2f, Player.whoAmI, 0f, 0f);//18*5
                                Main.projectile[num19].ai[1] = Player.position.Y;
                                Main.projectile[num19].tileCollide = false;
                            }
                        }
                    }
                }
            }
            // 灾厄之戒：常驻增伤；免伤期间概率在头顶召唤站火（StandingFire）
            if(calamityRing)
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
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<StandingFire>(), 120, 5f, Player.whoAmI, 0f, 0f);
                                Main.projectile[num19].ai[1] = Player.position.Y;
                            }
                        }
                    }
                }
            }
            // 炼狱：每 600 帧（10 秒）从高空向玩家瞄准方向齐射一轮扇形地狱火流星雨
            if(gehenna)
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
                                int projectile = Projectile.NewProjectile(Player.GetSource_FromThis(), spawn.X, spawn.Y, velocity.X, velocity.Y, ModContent.ProjectileType<BrimstoneHellfireballFriendly2>(), 108, 5f, Main.myPlayer, 0f, 0f);
                                Main.projectile[projectile].tileCollide = false;
                                Main.projectile[projectile].timeLeft = 50;
                            }
                        }
                    }
                }
            }
            // 虚空之烬：常驻增伤/熔岩免疫；每 10 秒地狱火齐射，免伤期间再召唤高伤站火
            if(voidofExtinction)
            {
                Player.GetDamage<GenericDamageClass>() += 0.15f;
                Player.lavaRose = true;
                Player.lavaMax += 240;
                if(Player.lavaWet)
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
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<StandingFire>(), 480, 5f, Player.whoAmI, 0f, 0f);
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
                                int projectile = Projectile.NewProjectile(Player.GetSource_FromThis(), spawn.X, spawn.Y, velocity.X, velocity.Y, ModContent.ProjectileType<BrimstoneHellfireballFriendly2>(), 432, 5f, Main.myPlayer, 0f, 0f);
                                Main.projectile[projectile].tileCollide = false;
                                Main.projectile[projectile].timeLeft = 50;
                            }
                        }
                    }
                }
            }
            // 利维坦龙涎香：免疫溺水、水下作战；移动时制造毒海水，并周期对近身敌人施毒
            if(levianthanAmbergris)
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
                        Player.GetDamage<GenericDamageClass>() += 0.05f;
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
            if(theAmalgam)
            {
                Player.GetDamage<GenericDamageClass>() += 0.3f;
                Player.GetCritChance<GenericDamageClass>() += 15;
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
                                int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<AuraRain>(), 320, 2f, Player.whoAmI, 0f, 0f);//18*5
                                Main.projectile[num19].ai[1] = Player.position.Y;
                                Main.projectile[num19].tileCollide = false;
                            }
                        }
                    }
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
                            int num19 = Projectile.NewProjectile(Player.GetSource_FromThis(), x, y, num15, num16, ModContent.ProjectileType<StandingFire>(), 360, 5f, Player.whoAmI, 0f, 0f);
                            Main.projectile[num19].ai[1] = Player.position.Y;
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
                                int projectile = Projectile.NewProjectile(Player.GetSource_FromThis(), spawn.X, spawn.Y, velocity.X, velocity.Y, ModContent.ProjectileType<BrimstoneHellfireballFriendly2>(), 540, 5f, Main.myPlayer, 0f, 0f);
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
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center.X, Player.Center.Y, 0f, 0f, ModContent.ProjectileType<PoisonousSeawater>(), 450, 5f, Player.whoAmI, 0f, 0f);
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
            if (theCommunity)
            {
                int defeated = CommunityBossCount();
                if (defeated > 0)
                {
                    // 进度系数：击败 1 档（石巨人）= 0（初始值），全清 18 档 = 1（满配值）
                    float t = (defeated - 1) / 17f;
                    float damage     = InitDamage     + (MaxDamage     - InitDamage)     * t;
                    float crit       = InitCrit       + (MaxCrit       - InitCrit)       * t;
                    float meleeSpeed = InitMeleeSpeed + (MaxMeleeSpeed - InitMeleeSpeed) * t;
                    float endurance  = InitEndurance  + (MaxEndurance  - InitEndurance)  * t;
                    float moveSpeed  = InitMoveSpeed  + (MaxMoveSpeed  - InitMoveSpeed)  * t;
                    float lifePct    = InitLifePct    + (MaxLifePct    - InitLifePct)    * t;
                    float manaPct    = InitManaPct    + (MaxManaPct    - InitManaPct)    * t;
                    int lifeRegen = (int)(InitLifeRegen + (MaxLifeRegen - InitLifeRegen) * t);
                    int manaRegen = (int)(InitManaRegen + (MaxManaRegen - InitManaRegen) * t);
                    int defense   = (int)(InitDefense   + (MaxDefense   - InitDefense)   * t);
                    float jumpPct   = InitJumpPct   + (MaxJumpPct   - InitJumpPct)   * t;
                    float minePct   = InitMinePct   + (MaxMinePct   - InitMinePct)   * t;
                    float costPct   = InitCostPct   + (MaxCostPct   - InitCostPct)   * t;
                    float thornsPct = InitThornsPct + (MaxThornsPct - InitThornsPct) * t;
                    float luckPct   = InitLuckPct   + (MaxLuckPct   - InitLuckPct)   * t;
                    Player.GetDamage<GenericDamageClass>() += damage;
                    Player.GetCritChance<GenericDamageClass>() += crit;
                    Player.GetAttackSpeed<MeleeDamageClass>() += meleeSpeed;
                    Player.endurance += endurance;
                    Player.moveSpeed += moveSpeed;
                    Player.statLifeMax2 += (int)(Player.statLifeMax2 * lifePct);
                    Player.statManaMax2 += (int)(Player.statManaMax2 * manaPct);
                    Player.lifeRegen += lifeRegen;
                    Player.manaRegen += manaRegen;
                    Player.statDefense += defense;
                    Player.jumpSpeedBoost *= 1f + jumpPct;      // 跳跃力 +5%~+30%
                    Player.pickSpeed *= 1f - minePct;           // 挖掘提速 5%~30%（pickSpeed 越低越快）
                    Player.manaCost *= 1f - costPct;            // 魔力消耗减免 5%~30%
                    if (thornsPct > Player.thorns)
                        Player.thorns = thornsPct;              // 荆棘反伤 5%~30%（不覆盖更高的反伤）
                    Player.luck += luckPct;                     // 幸运 +0.05~+0.30
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
                // 特殊一次性加成（绑定特定 Boss，与进度系数无关）
                if (NPC.downedMoonlord)
                {
                    Player.maxMinions += 1;      // 月亮领主：+1 召唤栏
                    Player.wingTimeMax *= 2;     // 月亮领主：最大飞行时间 ×2
                }
                if (CalamityDemulationBossSystem.OldDuke)
                {
                    Player.maxMinions += 2;      // 硫海遗爵（老公爵）：+2 召唤栏
                }
                if (CalamityDemulationBossSystem.ExoMechs || CalamityDemulationBossSystem.SupremeCalamitas)
                {
                    Player.wingTime = 10000 * Player.wingTimeMax; // 星流巨械/至尊灾厄：每帧回满飞行时间 = 无限飞行
                }
            }
            if(deificAmulet)
            {
                Player.panic = true;
                Player.manaMagnet = true;
                Player.magicCuffs = true;
                Player.GetArmorPenetration<GenericDamageClass>() += 25;
                if(Player.wet)
                {
                    Lighting.AddLight((int)Player.Center.X / 16, (int)Player.Center.Y / 16, 1.35f, 0.3f, 0.9f);
                }
            }
            if(frigidBulwark)
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
            if(rampartofDeities)
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
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<OmegaBlueTentacle>()] < 6)
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
                        if (Main.rand.Next(2) == 0)
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
                        if (Player.FindBuffIndex(ModContent.BuffType<SilvaRevival>()) > -1) { Player.ClearBuff(Mod.Find<ModBuff>("SilvaRevival").Type); }
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
            if (godSlayerDamageProtect)
            {
                if (godSlayerDamageProtectMax < 80)
                    godSlayerDamageProtectMax++;
            }
            if (godSlayerDamage > 0f)
                godSlayerDamage -= 2.5f;
            if (godSlayerDamage < 0f)
                godSlayerDamage = 0f;
            if (godSlayerCooldown)
            {
                Player.GetDamage<GenericDamageClass>() += 0.1f;
            }
            if (godSlayerMeleefireCD > 0)
                godSlayerMeleefireCD--;
            if (frostBarrier)
            {
                Player.buffImmune[46] = true;
            }
            if(psychoticAmulet)
            {
                Player.GetDamage<RangedDamageClass>() += 0.05f;
                Player.GetCritChance<RangedDamageClass>() += 5;
                Player.shroomiteStealth = true;
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
        /// 影之再生（shadeRegen）通过反射读取灾厄 CalamityPlayer / CalamityPlayerPreTrailer 的私有字段
        /// areThereAnyDamnBosses，据此选择"静止回血"参数（有 Boss：时间上限 900、每帧 +2、上限 16；
        /// 无 Boss：上限 3600、每帧 +8、上限 60），在玩家完全静止且无使用动画时快速累积回复量，
        /// 并按 lifeRegenTime 概率生成 ShadowbeamStaff 尘埃。
        /// 塔拉生命回复（tarraLifeRegen）则简单叠加 +10 生命回复。
        /// </summary>
        public override void UpdateLifeRegen()
        {
            if (shadeRegen)
            {
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
                                var field = calamityPlayerType.GetField("areThereAnyDamnBosses",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                if (field != null)
                                {
                                    bool areThereAnyDamnBosses = (bool)field.GetValue(calPlayer);
                                    int lifeRegenTimeMaxBoost = (areThereAnyDamnBosses ? 900 : 3600);
                                    int lifeRegenMaxBoost = (areThereAnyDamnBosses ? 2 : 8);
                                    float lifeRegenLifeRegenTimeMaxBoost = (areThereAnyDamnBosses ? 16 : 60);
                                    if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
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
                                }
                            }
                        }
                    }
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                var calamityPlayerType = calamity1.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
                if (calamityPlayerType != null)
                {
                    var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)
                            ?.MakeGenericMethod(calamityPlayerType);
                    if (getModPlayerMethod != null)
                    {
                        if (getModPlayerMethod.Invoke(Player, null) is ModPlayer calPlayer)
                        {
                            var field = calamityPlayerType.GetField("areThereAnyDamnBosses",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                bool areThereAnyDamnBosses = (bool)field.GetValue(calPlayer);
                                int lifeRegenTimeMaxBoost = (areThereAnyDamnBosses ? 900 : 3600);
                                int lifeRegenMaxBoost = (areThereAnyDamnBosses ? 2 : 8);
                                float lifeRegenLifeRegenTimeMaxBoost = (areThereAnyDamnBosses ? 16 : 60);
                                if ((double)Math.Abs(Player.velocity.X) < 0.05 && (double)Math.Abs(Player.velocity.Y) < 0.05 && Player.itemAnimation == 0)
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
                            }
                        }
                    }
                }
            }
            if (tarraLifeRegen)
            {
                Player.lifeRegen += 10;
            }
        }
        /// <summary>
        /// tModLoader 的 UpdateBadLifeRegen 钩子：仅在生命回复为负时被调用。
        /// 在女巫套装免死无敌窗口（silvaCountdown &gt; 0 且 hasSilvaEffect 且 silvaSet）内，
        /// 把负的 Player.lifeRegen 钳回 0，抵消 debuff 造成的持续掉血；
        /// 与 PostUpdateBuffs / PostUpdateEquips 的同一判定重复，用于覆盖各结算路径。
        /// </summary>
        public override void UpdateBadLifeRegen()
        {
            if (silvaCountdown > 0 && hasSilvaEffect && silvaSet)
            {
                if (Player.lifeRegen < 0)
                    Player.lifeRegen = 0;
            }
        }
        /// <summary>
        /// tModLoader 的 CanConsumeAmmo 钩子：判定本次射击是否消耗弹药。
        /// 代达罗斯纹章 1/5（20%）、元素箭袋 40% 概率不消耗弹药。
        /// </summary>
        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            if(daedalusEmblem && weapon.DamageType == DamageClass.Ranged && Main.rand.NextBool(5))
            {
                return false;
            }
            if (elementalQuiver && weapon.DamageType == DamageClass.Ranged && Main.rand.NextFloat() < 0.4f)
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
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    var calamityPlayerType = calamity1.Code.GetTypes()
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
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    var calamityPlayerType = calamity1.Code.GetTypes()
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
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    var calamityPlayerType = calamity1.Code.GetTypes()
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
        /// 近战挥砍特效：修正武器挥舞位置，元素手套生效时附带彩虹粒子
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
        /// 聚合大脑 1/8、大杂烩（The Amalgam）1/4 概率完全免伤。
        /// </summary>
        public override bool FreeDodge(Player.HurtInfo info)
        {
            if(amalgamatedBrain && Main.rand.NextBool(8))
            {
                return true;
            }
            if(theAmalgam && Main.rand.NextBool(4))
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
            return false;
        }
        /// <summary>
        /// 被 NPC 接触命中前触发：
        /// 血肉图腾生效时将本次接触伤害减半，并启动 20 秒冷却。
        /// 注意：这里改用 modifiers.FinalDamage 仅作用于"本次"伤害，
        /// 避免直接改写 npc.damage（那样会永久污染 NPC 的全局伤害并影响其他玩家）。
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
                npc.damage /= 2;
            }
            if (bloodflareMelee && bloodflareFrenzyTimer > 0)
            {
                npc.damage /= 2;
            }
            if (silvaMelee && silvaCountdown <= 0 && hasSilvaEffect)
            {
                npc.damage = (int)((double)npc.damage * 0.8);
            }
        }
        /// <summary>
        /// tModLoader 的 ModifyHitByProjectile 钩子：被弹幕命中、伤害结算前调用。
        /// 装备蜜蜂抗性饰品且命中来源属于蜜蜂弹幕列表时，将本次伤害减半。
        /// </summary>
        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            if (beeResist && CalamityDemutation.beeProjectileList.Contains(proj.type))
            {
                modifiers.FinalDamage *= 0.5f;
            }
        }
        /// <summary>
        /// 受到伤害前触发：
        /// 血契有 25% 概率使本次伤害变为 2.5 倍（模拟"被暴击"）。
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            double damageMult = 1.0 + ((bloodPact && Main.rand.NextBool(4)) ? 1.5 : 0.0) + (enraged ? 0.25 : 0.0);
            modifiers.FinalDamage *= (float)damageMult;
            if (theAbsorber)
            {
                int healAmt = (int)modifiers.SourceDamage.Base / 10;//20->10
                Player.statLife += healAmt;
                Player.HealEffect(healAmt);
            }
            if (sponge)
            {
                int healAmt = (int)modifiers.SourceDamage.Base / 5;//10->5
                Player.statLife += healAmt;
                Player.HealEffect(healAmt);
            }
            if (godSlayerReflect && Main.rand.NextBool(20))
            {
                Player.immuneNoBlink = true;
                Player.immune = true;
            }
            if ((godSlayerReflect && modifiers.SourceDamage.Base <= 80) || modifiers.SourceDamage.Base < 1)
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
                Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.Center.X + (float)Main.rand.Next(-40, 40), Player.Center.Y - (float)Main.rand.Next(20, 60), Player.velocity.X * 0.3f, Player.velocity.Y * 0.3f, ProjectileID.BrainOfConfusion, 0, 0f, Player.whoAmI, 0f, 0f);
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
                            Main.npc[m].AddBuff(BuffID.Frozen, (int)num11, false);
                        }
                    }
                }
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
            modifiers.FinalDamage *= (float)damageMult;
            // 召唤师跨职业 nerf 近似回调：灾厄对手持非召唤武器时的召唤弹幕伤害 ×0.75，这里撤销
            if (CalamityDemutationConfigSystem.Instance?.RevertVanillaNerfs == true && ModLoader.HasMod("CalamityMod") && proj.CountsAsClass<SummonDamageClass>())
            {
                Item heldItem = Player.HeldItem;
                bool heldNonSummonWeapon = heldItem.damage > 0
                    && !heldItem.CountsAsClass<SummonDamageClass>()
                    && heldItem.useStyle != ItemUseStyleID.None
                    && !(heldItem.pick > 0 || heldItem.axe > 0 || heldItem.hammer > 0)
                    && !heldItem.accessory
                    && heldItem.ammo == AmmoID.None;
                if (heldNonSummonWeapon)
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
                    int fDamage = 224;//56 * 4 = 224
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
                    int num19 = Projectile.NewProjectile(Entity.GetSource_FromThis(), x, y, num15, num16, ProjectileID.ShadowBeamFriendly, shadowBeamDamage, 7f, Player.whoAmI, 0f, 0f);
                    Main.projectile[num19].ai[1] = Player.position.Y;
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
                    int num19 = Projectile.NewProjectile(Entity.GetSource_FromThis(), x, y, num15, num16, ProjectileID.DemonScythe, demonScytheDamage, 7f, Player.whoAmI, 0f, 0f);
                    Main.projectile[num19].ai[1] = Player.position.Y;
                }
            }
            if(deificAmulet)
            {
                if (info.Damage == 1.0)
                {
                    Player.immuneTime += 15;//10->15
                }
                else
                {
                    Player.immuneTime += 25;//20->25
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
                    int num17 = Projectile.NewProjectile(Entity.GetSource_FromThis(), x, y, num13, num14, ProjectileID.HallowStar,260, 4f, Player.whoAmI, 0f, 0f);//130->260
                    Main.projectile[num17].usesLocalNPCImmunity = true;
                    Main.projectile[num17].localNPCHitCooldown = 5;
                }
            }
            if(rampartofDeities)
            {
                if (info.Damage == 1.0)
                {
                    Player.immuneTime += 25;//15->25
                }
                else
                {
                    Player.immuneTime += 40;//25->40
                }
                for (int n = 0; n < 7; n++)
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
                    int num17 = Projectile.NewProjectile(Entity.GetSource_FromThis(), x, y, num13, num14, ProjectileID.HallowStar, (int)Player.GetDamage<GenericDamageClass>().ApplyTo(780), 4f, Player.whoAmI, 0f, 0f);//130->260
                    Main.projectile[num17].usesLocalNPCImmunity = true;
                    Main.projectile[num17].localNPCHitCooldown = 5;
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
        /// 消费两个自定义键位（定义于 Systems/CalamityDemulationKeybindsSystem）：
        /// 1) GodslayerDashHotKey（默认 H）——转发给 GodSlayerHelm.RequestGodslayerDash，由灾厄侧执行冲刺；
        /// 2) DemonshadeHotKey（默认 Y，套装主动技能）——依次判定：恶魔之影套装（播放音效/迸发吸魂尘埃、
        ///    自身获得 600 帧狂怒 buff，服务器端对 3000 距离内的敌人一并施加狂怒）、
        ///    欧米伽蓝套装（冷却 1800 帧，净化粉末尘埃爆发）、塔拉近战（tarraCooldown 归零时置位 tarraDefense）。
        /// </summary>
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            // 弑神者冲刺：本模组的键与灾厄自己的键(默认 H)都能触发；
            // GodSlayerHelm 会把灾厄的 godSlayer 标志置真，闸门与冲刺表现全由灾厄负责
            if(CalamityDemulationKeybindsSystem.GodslayerDashHotKey.JustPressed)
                Content.Items.Armors.GodSlayer.GodSlayerHelm.RequestGodslayerDash(Player);
            if(CalamityDemulationKeybindsSystem.DemonshadeHotKey.JustPressed)
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
                if(omegaBlueSet && omegaBlueCooldown <= 0)
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
                if (tarraMelee && tarraCooldown <= 0)
                {
                    tarraDefense = true;
                }
                if (auricSet)
                {
                    Asset<Texture2D> carpetAuric = ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/AuricCarpet");
                    if (auricSet) { TextureAssets.FlyingCarpet = carpetAuric; }
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
                if (Player.whoAmI == Main.myPlayer)
                {
                    int healAmount = (Main.rand.Next(3) + 1);
                    Player.statLife += healAmount;
                    Player.HealEffect(healAmount);
                }
            }
            int weaponDamage = Player.HeldItem.damage;
            if (godSlayerMelee && godSlayerMeleefireCD <= 0 && (hit.DamageType == DamageClass.Melee || hit.DamageType == DamageClass.MeleeNoSpeed))
            {
                int finalDamage = 500 + weaponDamage / 2;
                Vector2 getSpwanPos = new(Player.Center.Y, Player.Center.X);
                Vector2 velocity = CDUtil.GiveVelocity(200f);
                Projectile.NewProjectile(Player.GetSource_FromThis(), getSpwanPos, velocity * 4f, ModContent.ProjectileType<GodSlayerDart>(), finalDamage, 0f, Player.whoAmI);
                godSlayerMeleefireCD = 60;
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
            if ((statisCurse || statisBeltOfCurses) && (proj.CountsAsClass<SummonDamageClass>() || proj.CountsAsClass<SummonMeleeSpeedDamageClass>()))
            {
                ApplyCalamityBuff(target, "CalamityMod", "TemporalSadness", 120);
                target.AddBuff(BuffID.ShadowFlame, 120);
                ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "TemporalSadness", 120);
                target.AddBuff(BuffID.ShadowFlame, 120);
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
                if (Player.whoAmI == Main.myPlayer)
                {
                    int healAmount = (Main.rand.Next(3) + 1);
                    Player.statLife += healAmount;
                    Player.HealEffect(healAmount);
                }
            }
            if (proj.CountsAsClass<MeleeDamageClass>() && silvaMelee && Main.rand.NextBool(4))
                target.AddBuff(ModContent.BuffType<SilvaHysteresis>(), 20);
            Player player = Main.player[proj.owner];
            int weaponDamage = player.HeldItem.damage;
            //弑神飞镖
            if (godSlayerMelee && godSlayerMeleefireCD <= 0 && (proj.CountsAsClass<MeleeDamageClass>() || proj.CountsAsClass<MeleeNoSpeedDamageClass>()))
            {
                int finalDamage = 500 + weaponDamage / 2;
                Vector2 velocity = CDUtil.GiveVelocity(200f);
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, velocity * 4f, ModContent.ProjectileType<GodSlayerDart>(), finalDamage, 0f, Player.whoAmI);
                godSlayerMeleefireCD = 60;
            }
        }
        /// <summary>
        /// tModLoader 的 PreKill 钩子：玩家即将死亡前调用。
        /// 星云核心 1/5（20%）概率免死：播放特效、回复 100 点生命并取消本次死亡。
        /// </summary>
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            if (nebulousCore && Main.rand.NextBool(5))
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
                }
                hasSilvaEffect = true;
                if (Player.statLife < 1)
                {
                    Player.statLife = 1;
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
                int heal = 300;//1.3.2:300->150
                Player.statLife += heal;
                Player.HealEffect(heal);
                if (Player.statLife > Player.statLifeMax2)
                {
                    Player.statLife = Player.statLifeMax2;
                }
                Player.AddBuff(ModContent.BuffType<GodSlayerCooldown>(), 2700);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 洋葱类永久标志的持久化（extraAccessoryML 天界洋葱 / extraWingSlot 翅膀洋葱；
        /// 其余字段每帧由装备重新计算，无需保存）
        /// </summary>
        public override void SaveData(TagCompound tag)
        {
            tag["extraAccessoryML"] = extraAccessoryML;
            tag["extraWingSlot"] = extraWingSlot;
        }
        /// <summary>
        /// tModLoader 的 LoadData 钩子：读档时恢复洋葱类永久解锁标志。
        /// 与 SaveData 严格对应，只读取两个持久化字段。
        /// </summary>
        public override void LoadData(TagCompound tag)
        {
            extraAccessoryML = tag.GetBool("extraAccessoryML");
            extraWingSlot = tag.GetBool("extraWingSlot");
        }
        /// <summary>
        /// 自定义网络消息码：客户端上报"洋葱永久解锁状态变更"（见 SendClientChanges），
        /// 由主类 CalamityDemutation.HandlePacket 处理。
        /// </summary>
        public const byte MsgPermanentUnlock = 0;
        /// <summary>
        /// 联机时把本地玩家的洋葱解锁标志复制到基准副本，供 SendClientChanges 检测差异用。
        /// 只同步这两个永久标志；其余字段每帧由装备重算，无需跨端传输。
        /// </summary>
        public override void CopyClientState(ModPlayer targetCopy)
        {
            CalamityDemutationPlayer copy = (CalamityDemutationPlayer)targetCopy;
            copy.extraAccessoryML = extraAccessoryML;
            copy.extraWingSlot = extraWingSlot;
        }
        /// <summary>
        /// 客户端状态变更上报：当本地洋葱解锁标志相对基准副本变化（例如非主机端吃了洋葱，
        /// 服务器本身不知道）时，向服务器发一条带新标志的消息，由主类 HandlePacket
        /// 更新服务器上的玩家副本并广播 SyncPlayer 给所有端。
        /// </summary>
        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            CalamityDemutationPlayer old = (CalamityDemutationPlayer)clientPlayer;
            if (old.extraAccessoryML != extraAccessoryML || old.extraWingSlot != extraWingSlot)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)MsgPermanentUnlock);
                packet.Write(Player.whoAmI);
                packet.Write(extraAccessoryML);
                packet.Write(extraWingSlot);
                packet.Send();
            }
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
    }
}
