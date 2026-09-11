using System;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 防御之刃（DefenseBlade） - 传奇级近战武器。
    /// 每次挥砍都会发射一缕追踪敌人的日光碎片 DefenseBeam，碎片的击杀(OnKill)再散落出会弹跳的火星 DefenseFlame，
    /// 火星命中后追加 DefenseBlast2；用剑身直接命中敌人（或 PvP 命中玩家）时，在目标处引发范围爆发 DefenseBlast。
    /// 伤害随主线进度加算成长：每击败进度表 LegendaryBosses 中的 Boss 累加固定增伤（合计 +8.10，满配 9.10×）。
    /// </summary>
    internal class DefenseBlade:ModItem
    {
        /// <summary>静态属性：研究所解锁数量设为 1。</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 基础属性：尺寸 72、近战伤害 135、14 tick 挥击、自动连击；
        /// 设置 Item.shoot 为 DefenseBeam 并开启 shootsEveryUse，使每次挥砍都发射碎片；
        /// 稀有度用月后自定义等级 17（基础稀有度由 GlobalItem 统一改写）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.height = Item.width = 72;              // 贴图宽高（像素）
            Item.DamageType = DamageClass.Melee;        // 伤害类型：近战
            Item.damage = 135;                          // 基础伤害
            Item.useStyle = ItemUseStyleID.Swing;       // 使用样式：挥击
            Item.useAnimation = Item.useTime = 14;      // 使用动画/间隔（tick）
            Item.scale *= 1.25f;                        // 贴图放大 25%
            Item.useTurn = true;                        // 挥砍中可转向
            Item.knockBack = 4.25f;                     // 击退
            Item.UseSound = SoundID.Item1;              // 挥砍音效
            Item.autoReuse = true;                      // 自动连击
            Item.shootSpeed = 14f;                      // 弹幕初速度
            Item.shootsEveryUse = true;                 // 每次挥砍都发射（而非隔次）
            Item.shoot = ModContent.ProjectileType<DefenseBeam>();                       // 发射的弹幕：日光碎片
            Item.rare = ItemRarityID.Lime;              // 基础稀有度：青柠
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 17;// 月后自定义稀有度等级（0=未设置）
        }
        /// <summary>挥砍表现：调用 BetterSwing 修正巨剑挥舞位置，并随机洒落金币色尘埃（约 1/3 概率）。</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.GoldCoin, 0f, 0f, 0, new Color(255, Main.DiscoG, 53));
        }
        /// <summary>把传奇进度增伤倍率乘入最终伤害（倍率由 LegendaryDamage 结算）。</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage *= LegendaryDamage();
        }
        /// <summary>剑身直接命中 NPC 时，在目标中心生成范围爆发弹幕 DefenseBlast（伤害/击退沿用武器数值）。</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) => Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero, ModContent.ProjectileType<DefenseBlast>(), Item.damage, Item.knockBack, Main.myPlayer);
        /// <summary>PvP 下剑身命中玩家时，同样在目标中心生成 DefenseBlast。</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo) => Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero, ModContent.ProjectileType<DefenseBlast>(), Item.damage, Item.knockBack, Main.myPlayer);
        /// <summary>按 LegendaryBosses 进度表加算累加，返回传奇武器伤害倍率（基础 1.00×，满配 9.10×）。</summary>
        public static float LegendaryDamage()
        {
            float damageBuff = 1f;
            // 加算阶梯式：遍历进度表，每击败一个 Boss 累加固定增伤，总倍率有明确上限（满配 9.10×）。
            foreach (var (downed, increment) in LegendaryBosses)
            {
                if (downed())
                    damageBuff += increment;
            }
            return damageBuff;
        }
        /// <summary>
        /// 传奇武器增伤进度表：石巨人 → 至尊灾厄（主线 18 档）。
        /// 每项为 (是否已击败, 固定增伤)，加算累加，合计 +8.10，满配 9.10×。
        /// 前中期增量小、终局增量大：反映难度递增，同时弥补伤害基数变大后固定增量的相对收益下降。
        /// </summary>
        private static readonly (Func<bool> Downed, float Increment)[] LegendaryBosses =
        [
            // —— 石巨人 → 月球领主 ——
            (() => NPC.downedGolemBoss, 0.10f),                          // 石巨人 Golem
            (() => CalamityDemulationBossSystem.Plaguebringer, 0.15f),   // 瘟疫使者歌莉娅 Plaguebringer Goliath
            (() => CalamityDemulationBossSystem.Ravager, 0.15f),         // 毁灭魔像（掠夺者）Ravager
            (() => NPC.downedAncientCultist, 0.10f),                     // 拜月教邪教徒 Lunatic Cultist
            (() => CalamityDemulationBossSystem.AstrumDeus, 0.20f),      // 星神游龙 Astrum Deus
            (() => NPC.downedMoonlord, 0.20f),                           // 月球领主 Moon Lord
            // —— 月后 · 亵渎前 ——
            (() => CalamityDemulationBossSystem.Guardians, 0.20f),       // 亵渎守卫 Profaned Guardians
            (() => CalamityDemulationBossSystem.Dragonfolly, 0.20f),     // 丛林龙 Dragonfolly
            // —— 亵渎 → 噬神 ——
            (() => CalamityDemulationBossSystem.Providence, 0.40f),      // 亵渎天神 Providence
            (() => CalamityDemulationBossSystem.CeaselessVoid || ClassicSentinelsDowned, 0.20f), // 无尽虚空 Ceaseless Void
            (() => CalamityDemulationBossSystem.StormWeaver || ClassicSentinelsDowned, 0.20f),   // 风暴编织者 Storm Weaver
            (() => CalamityDemulationBossSystem.Signus || ClassicSentinelsDowned, 0.20f),        // 西格纳斯 Signus
            (() => CalamityDemulationBossSystem.Polterghast, 0.50f),     // 噬魂幽花 Polterghast
            (() => CalamityDemulationBossSystem.OldDuke, 0.50f),         // 老公爵 Old Duke
            (() => CalamityDemulationBossSystem.DevourerOfGods, 0.80f),  // 噬神者 Devourer of Gods
            // —— 终局 ——
            (() => CalamityDemulationBossSystem.Yharon, 2.0f),          // 犽戎 Yharon
            (() => CalamityDemulationBossSystem.ExoMechs, 1.0f),        // 星流巨械 Exo Mechs
            (() => CalamityDemulationBossSystem.SupremeCalamitas, 1.0f),// 至尊灾厄 Supreme Calamitas
        ];
        /// <summary>
        /// 经典版三使者合并标记是否全部倒下。
        /// 经典版没有单个使者的独立标记（只有 Sentinel1/2/3），因此在经典版下，
        /// 三个使者条目退化为"三使者皆倒"时同时生效（各 +0.20，合计 +0.60）。
        /// </summary>
        private static bool ClassicSentinelsDowned =>
            CalamityDemulationBossSystem.Sentinel1 && CalamityDemulationBossSystem.Sentinel2 && CalamityDemulationBossSystem.Sentinel3;
    }
}
