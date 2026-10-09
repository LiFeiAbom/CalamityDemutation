using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 宇宙灯笼（Cosmilamp）—— 「归虚之灵」链（CI 口径）的自持件之一。
    /// 老规矩取灾厄 **2.0.3.9**：42×60 贴图（判定 42×60）、伤害 **127**、魔力 10、使用/动画 **15 帧**、
    /// 击退 4、**月后 12 档（青绿，即灾厄 `Turquoise`）**、价值 **1 铂金 20 金**（`Rarity12BuyPrice`）、
    /// 音 `SoundID.Item44`、`autoReuse`、弹速 10，在鼠标处点亮一盏**宇宙灯笼**（<see cref="CosmilampMinion"/>）。
    /// </summary>
    /// <remarks>
    /// 为什么自持、为什么取 2.0.3.9：1.4.4-release（实装那版）把它砍到伤害 95、使用帧 24；
    /// 2.0 那版更旧也更弱（伤害 89、且**还没有光束弹幕**，2.0.3.9 才加的 <see cref="CosmilampBeam"/>）；
    /// 经典 cal-1.4.2.101 则是伤害 180 / 使用 36 帧的另一套。本工程按「回调 + 链条统一 2.0.3.9」取 2.0.3.9。
    /// <para>
    /// 机制要点（照源）：一盏灯笼**吃 2 格召唤栏**（<see cref="LanternSummonCost"/>），
    /// 所以 <see cref="CanUseItem"/> 要 `maxMinions >= 2`；出手时会把**场上已有的灯笼计时器全部归零**
    /// （重新对齐阵型），并把"已有盏数"写进新灯笼的 `ai[0]`（决定它排在头顶阵型的哪个位置）。
    /// </para>
    /// <para>
    /// **无配方**——来源照源：**Signus（虚空之影 / 神明吞噬者的使者）**。
    /// 现代分支挂 `Signus` 本体（源里走"非专家武器池" `CalamityStyle(1/4, {宇宙苦无, 宇宙灯笼})`，
    /// 本工程沿用既有简化口径记 1/4）与 `SignusBag`（1/3）；
    /// 经典分支挂 `CosmicWraith`（经典里 Signus 的类名）那条 1/3 本体掉落
    /// （源上还带一个"非神明吞噬者哨兵阶段"的自定义条件，本工程无法复刻，按纯 1/3 简化）。
    /// </para>
    /// </remarks>
    internal class Cosmilamp:ModItem
    {
        // ── 源里写在物品上的可调常量（仆从与光束直接读，照源保留）──
        /// <summary>每盏灯笼的开火间隔（帧，源值 105）</summary>
        public const int BeamShootRate = 105;
        /// <summary>索敌半径（源值 1360）</summary>
        public const float MaxTargetingDistance = 1360f;
        /// <summary>光束的追踪速度（源值 17）</summary>
        public const float BeamHomeSpeed = 17f;
        /// <summary>一盏灯笼占用的召唤栏位（源值 2）</summary>
        public const float LanternSummonCost = 2f;

        /// <summary>研究解锁一份（照源 2.0 的 `SacrificeTotal = 1`）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：照源 2.0.3.9（伤害 127、使用 15 帧、月后 12 档、1 铂金 20 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 60;
            Item.damage = 127;
            Item.mana = 10;
            Item.useTime = Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(1, 20, 0, 0);   // 源用 Rarity12BuyPrice = buyPrice(1, 20, 0, 0)
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;   // 月后 12：青绿（＝灾厄 Turquoise）
            Item.UseSound = SoundID.Item44;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CosmilampMinion>();
            Item.shootSpeed = 10f;
            Item.DamageType = DamageClass.Summon;
        }
        /// <summary>一盏灯笼吃 2 格召唤栏，所以栏位不够 2 格时不让出手（照源）</summary>
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-09 指定：127 → 200）。
        /// </summary>
        private const float InflatedDamage = 200f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 127。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 召唤件特有一环：<c>Shoot</c> 里写进灯笼的 <c>originalDamage</c> 也必须读 <see cref="BaseDamage"/>。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        public override bool CanUseItem(Player player) => player.maxMinions >= 2;
        /// <summary>
        /// 出手：先把场上已有灯笼的计时器全部归零（重新对齐阵型），再在鼠标处点亮一盏新的，
        /// 把"已有盏数"写进 `ai[0]`、把面板伤害写进 `originalDamage`（照源）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == type && Main.projectile[i].owner == player.whoAmI && Main.projectile[i].active)
                    {
                        Main.projectile[i].ai[1] = 0f;   // 源写的是 CosmilampMinion.Timer = 0f（同一个 ai[1]）
                        Main.projectile[i].netUpdate = true;
                    }
                }

                int existingLamps = player.ownedProjectileCounts[type];
                int p = Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(p))
                {
                    Main.projectile[p].originalDamage = (int)BaseDamage;
                    Main.projectile[p].ai[0] = existingLamps;
                }
            }
            return false;
        }
    }
}
