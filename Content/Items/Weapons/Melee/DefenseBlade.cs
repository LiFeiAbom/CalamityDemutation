using System;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 防御之刃（DefenseBlade） - 传奇级近战武器。
    /// 每次挥砍都会发射一缕追踪敌人的日光碎片 DefenseBeam，碎片的击杀(OnKill)再散落出会弹跳的火星 DefenseFlame，
    /// 火星命中后追加 DefenseBlast2；用剑身直接命中敌人（或 PvP 命中玩家）时，在目标处引发范围爆发 DefenseBlast。
    /// 伤害随主线进度加算成长：每击败进度表 LegendaryBosses 中的 Boss 累加固定增伤（合计 +5.00，满配 6.00×）。
    /// 数值膨胀开关开启时恢复 2026-09-28 削弱前的原值：基础伤害 110 → 135、进度增伤合计 +5.00 → +8.10（满配 6.00× → 9.10×）。
    /// </summary>
    internal class DefenseBlade:ModItem
    {
        // ── 静态字段 ──
        /// <summary>
        /// 传奇武器增伤进度表：石巨人 → 至尊灾厄（主线 18 档）。
        /// 每项为 (是否已击败, 当前固定增伤, 数值膨胀开关开启时的旧版固定增伤)：
        /// 当前口径加算累加合计 +5.00（满配 6.00×，2026-09-28 削弱后的值），
        /// 旧版口径合计 +8.10（满配 9.10×，即该次削弱前的原值）。
        /// 前中期增量小、终局增量大：反映难度递增，同时弥补伤害基数变大后固定增量的相对收益下降。
        /// </summary>
        private static readonly (Func<bool> Downed, float Increment, float LegacyIncrement)[] LegendaryBosses =
        [
            // —— 石巨人 → 月球领主 ——
            (() => NPC.downedGolemBoss, 0.06f, 0.10f),                          // 石巨人 Golem
            (() => BossSystem.Plaguebringer, 0.09f, 0.15f),   // 瘟疫使者歌莉娅 Plaguebringer Goliath
            (() => BossSystem.Ravager, 0.09f, 0.15f),         // 毁灭魔像（掠夺者）Ravager
            (() => NPC.downedAncientCultist, 0.06f, 0.10f),                     // 拜月教邪教徒 Lunatic Cultist
            (() => BossSystem.AstrumDeus, 0.12f, 0.20f),      // 星神游龙 Astrum Deus
            (() => NPC.downedMoonlord, 0.12f, 0.20f),                           // 月球领主 Moon Lord
            // —— 月后 · 亵渎前 ——
            (() => BossSystem.Guardians, 0.12f, 0.20f),       // 亵渎守卫 Profaned Guardians
            (() => BossSystem.Dragonfolly, 0.12f, 0.20f),     // 丛林龙 Dragonfolly
            // —— 亵渎 → 噬神 ——
            (() => BossSystem.Providence, 0.25f, 0.40f),      // 亵渎天神 Providence
            (() => BossSystem.CeaselessVoid || ClassicSentinelsDowned, 0.12f, 0.20f), // 无尽虚空 Ceaseless Void
            (() => BossSystem.StormWeaver || ClassicSentinelsDowned, 0.12f, 0.20f),   // 风暴编织者 Storm Weaver
            (() => BossSystem.Signus || ClassicSentinelsDowned, 0.12f, 0.20f),        // 西格纳斯 Signus
            (() => BossSystem.Polterghast, 0.31f, 0.50f),     // 噬魂幽花 Polterghast
            (() => BossSystem.OldDuke, 0.31f, 0.50f),         // 老公爵 Old Duke
            (() => BossSystem.DevourerOfGods, 0.49f, 0.80f),  // 噬神者 Devourer of Gods
            // —— 终局 ——
            (() => BossSystem.Yharon, 1.26f, 2.00f),          // 犽戎 Yharon
            (() => BossSystem.ExoMechs, 0.62f, 1.00f),        // 星流巨械 Exo Mechs
            (() => BossSystem.SupremeCalamitas, 0.62f, 1.00f),// 至尊灾厄 Supreme Calamitas
        ];
        // ── 属性 ──
        /// <summary>
        /// 经典版三使者合并标记是否全部倒下。
        /// 经典版没有单个使者的独立标记（只有 Sentinel1/2/3），因此在经典版下，
        /// 三个使者条目退化为"三使者皆倒"时同时生效（各 +0.20，合计 +0.60）。
        /// </summary>
        private static bool ClassicSentinelsDowned =>
            BossSystem.Sentinel1 && BossSystem.Sentinel2 && BossSystem.Sentinel3;
        // ── 生命周期方法 ──
        /// <summary>静态属性：研究所解锁数量设为 1；允许右键连续触发（右键掷刃是独立招式）。</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        /// <summary>
        /// 基础属性：尺寸 72、近战伤害 110（数值膨胀开关开启时恢复削弱前的 135）、14 tick 挥击、自动连击；
        /// 设置 Item.shoot 为 DefenseBeam，挥砍时发射碎片；
        /// 稀有度用月后自定义等级 17（基础稀有度由 GlobalItem 统一改写）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.height = Item.width = 72;              // 贴图宽高（像素）
            Item.DamageType = DamageClass.Melee;        // 伤害类型：近战
            Item.damage = 110;                          // 基础伤害（2026-09-28 削弱前的原值为 135，见 BaseDamage）
            Item.useStyle = ItemUseStyleID.Swing;       // 使用样式：挥击
            Item.useAnimation = Item.useTime = 14;      // 使用动画/间隔（tick）
            Item.scale *= 1.25f;                        // 贴图放大 25%
            Item.useTurn = true;                        // 挥砍中可转向
            Item.knockBack = 4.25f;                     // 击退
            Item.UseSound = SoundID.Item1;              // 挥砍音效
            Item.autoReuse = true;                      // 自动连击
            Item.shootSpeed = 14f;                      // 弹幕初速度
            Item.shoot = ModContent.ProjectileType<DefenseBeam>();                       // 发射的弹幕：日光碎片
            Item.rare = ItemRarityID.Lime;              // 基础稀有度：青柠
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 17;// 月后自定义稀有度等级（0=未设置）
        }
        /// <summary>
        /// 数值膨胀开关开启时恢复的旧版基础伤害（2026-09-28「135 → 110」那次削弱的原值）。
        /// 进度增伤倍率的旧版口径见 <see cref="LegendaryBosses"/> 的第三列。
        /// </summary>
        private const float LegacyDamage = 135f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="LegacyDamage"/>，否则维持 <c>Item.damage</c> 的 110（削弱后）。
        /// 面板回调、剑身命中生成的 DefenseBlast 以及进度倍率都统一读这里/配 <see cref="LegendaryDamage"/>。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? LegacyDamage : Item.damage;
        /// <summary>右键可用（灾厄大修 0.4.0.1.3 的庇护巨刃）</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 按左右键切换状态（照抄 CWR 的 AegisBladeEcType.CanUseItem）：
        /// 右键收起贴图与剑身判定、改用 Item73 音效并改为掷出 <see cref="DefenseBladeProj"/>；
        /// 左键恢复原有的巨剑挥砍与 <see cref="DefenseBeam"/>。
        /// 巨刃在场期间整把武器不可再次使用（CWR 的 <c>ownedProjectileCounts == 0</c> 口径）。
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.UseSound = SoundID.Item73;
                Item.shoot = ModContent.ProjectileType<DefenseBladeProj>();
            }
            else
            {
                Item.noUseGraphic = false;
                Item.noMelee = false;
                Item.UseSound = SoundID.Item1;
                Item.shoot = ModContent.ProjectileType<DefenseBeam>();
            }
            return player.ownedProjectileCounts[ModContent.ProjectileType<DefenseBladeProj>()] == 0;
        }
        /// <summary>右键掷刃的挥舞速度 ×1.33（CWR 原值）</summary>
        public override float UseSpeedMultiplier(Player player) => player.altFunctionUse != 2 ? 1f : 1.33f;
        /// <summary>
        /// 右键掷出庇护巨刃，伤害为面板的 3.3 倍（CWR 原值）；
        /// 左键返回 true，交给默认逻辑发射 <see cref="DefenseBeam"/>。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<DefenseBladeProj>(), (int)(damage * 3.3f), knockback, player.whoAmI);
                return false;
            }
            return true;
        }
        /// <summary>挥砍表现：调用 BetterSwing 修正巨剑挥舞位置，并随机洒落金币色尘埃（约 1/3 概率）。</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.GoldCoin, 0f, 0f, 0, new Color(255, Main.DiscoG, 53));
        }
        /// <summary>把面板基础伤害与传奇进度增伤倍率乘入最终伤害（两者都按膨胀开关取值）。</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
            damage *= LegendaryDamage();
        }
        /// <summary>剑身直接命中 NPC 时，在目标中心生成范围爆发弹幕 DefenseBlast（伤害取当前基础伤害的裸面板，含膨胀修正）。</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) => Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero, ModContent.ProjectileType<DefenseBlast>(), (int)BaseDamage, Item.knockBack, Main.myPlayer);
        /// <summary>PvP 下剑身命中玩家时，同样在目标中心生成 DefenseBlast。</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo) => Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero, ModContent.ProjectileType<DefenseBlast>(), (int)BaseDamage, Item.knockBack, Main.myPlayer);
        // ── 公开方法 ──
        /// <summary>
        /// 按 LegendaryBosses 进度表加算累加，返回传奇武器伤害倍率：
        /// 常态基础 1.00×、满配 6.00×（合计 +5.00）；数值膨胀开关开启时改用旧版增量，满配 9.10×（合计 +8.10）。
        /// </summary>
        public static float LegendaryDamage()
        {
            float damageBuff = 1f;
            // 加算阶梯式：遍历进度表，每击败一个 Boss 累加固定增伤，总倍率有明确上限（常态 6.00× / 膨胀 9.10×）。
            bool legacy = ConfigSystem.StatInflationEnabled;
            foreach (var (downed, increment, legacyIncrement) in LegendaryBosses)
            {
                if (downed())
                    damageBuff += legacy ? legacyIncrement : increment;
            }
            return damageBuff;
        }
    }
}
