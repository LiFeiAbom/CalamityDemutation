using CalamityDemutation.Content.Projectiles.Ranged;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 发条弓（Clockwork Bow） - 射手弓，天堂之风链**批 4 第二件**（T1 六把武器之一），
    /// ＝灾厄 2.0 的 <c>Items/Weapons/Ranged/ClockworkBow.cs</c>。
    /// <para>
    /// **口径（用户 2026-10-15 拍板）**：中文名 **发条弓**、**数值膨胀 808 → 1724**、
    /// 面板 / 机制**全取 2.0**；配方**现代与经典两条并存**（见下）。弹幕 **精准弩箭** <see cref="PrecisionBolt"/>。
    /// 源 tooltip：<c>Hold left click to load up to six precision bolts</c> /
    /// <c>The more precision bolts are loaded, the harder they hit</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：48×96 / 伤害 808 / 使用 60 帧·动画 30 帧（<c>channel = true</c> 蓄力式）/
    /// 击退 4.25 / 弹速 15 / 箭类弹药 / <c>noUseGraphic = true</c>（贴图由手持弹幕负责画）/
    /// 使用音 `Item20` / **红档（原版 10 档，源 <c>ItemRarityID.Red</c>，不涉月后档）** /
    /// 价值 **1 铂金**（源 `Rarity10BuyPrice`）。2.2.2 是 770 / 紫档 / 1 铂金 20 金——**不取**。
    /// </para>
    /// <para>
    /// 左键按下时生成 <see cref="ClockworkBowHoldout"/> 手持弹幕，之后整个蓄力 - 齐射流程都归它；
    /// <c>CanUseItem</c> 保证场上只可能有一个手持弹幕（源就是靠这条挡住重复按下）。
    /// 源的 <c>UseTimeMultiplier() => 1f</c> 是空实现（等于默认值），本工程不写。
    /// </para>
    /// </summary>
    internal class ClockworkBow : ModItem
    {
        /// <summary>最多装填的弩箭数（照源 <c>public const int MaxBolts = 6</c>；手持弹幕要读它）</summary>
        public const int MaxBolts = 6;
        /// <summary>配方里的齿轮数量（两版都是 50）</summary>
        private const int CogCount = 50;
        /// <summary>现代分支配方里的月亮锭数量（照 2.0）</summary>
        private const int ModernLunarBarCount = 5;
        /// <summary>经典分支配方里的月亮锭数量（照 cal-1.4.2.101）</summary>
        private const int ClassicLunarBarCount = 15;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-15 指定：808 → 1724）</summary>
        private const float InflatedDamage = 1724f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 808</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：48×96 / 伤害 808 / 使用 60 帧·动画 30 帧 / 蓄力式 / 击退 4.25 / 弹速 15 /
        /// 箭类弹药 / 不画物品贴图 / 使用音 `Item20` / 红档 / 1 铂金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 48;                          // 贴图宽（像素）
            Item.height = 96;                         // 贴图高（像素）
            Item.damage = 808;                        // 面板伤害（照 2.0；2.2.2 是 770）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 60;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.channel = true;                      // 按住蓄力，松手齐射
            Item.knockBack = 4.25f;
            Item.value = Item.buyPrice(1, 0, 0, 0);   // 价值 1 铂金（源 Rarity10BuyPrice）
            Item.rare = ItemRarityID.Red;             // 红档（原版 10 档，不涉月后档）
            Item.noUseGraphic = true;                 // 贴图交给手持弹幕画
            Item.UseSound = SoundID.Item20;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.PurificationPowder;  // 源写的占位值（真正射出什么由 Shoot 决定）
            Item.shootSpeed = 15f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件派生伤害走手持弹幕里的 <c>Player.PickAmmo</c>，而它的入参来自
        /// <c>GetWeaponDamage(武器)</c>（内部会调 <c>ModifyWeaponDamage</c>），故会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>场上已有手持弹幕时不许再按（照源；这条同时挡住了"蓄力期间重复出手"）</summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<ClockworkBowHoldout>()] <= 0;
        /// <summary>出手只生成手持弹幕，真正射箭由它按蓄力进度负责（照源）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 shootDirection = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(source, position, shootDirection, ModContent.ProjectileType<ClockworkBowHoldout>(), damage, knockback, player.whoAmI);
            return false;
        }
        /// <summary>
        /// 配方（用户 2026-10-15 指定「并存」，两条都注册、都 @远古操纵机、全是原版件）：
        /// **现代**（照 2.0）= 月亮锭×5 + 齿轮×50；
        /// **经典**（照 cal-1.4.2.101）= 齿轮×50 + 月亮锭×15。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.LunarBar, ModernLunarBarCount).
                AddIngredient(ItemID.Cog, CogCount).
                AddTile(TileID.LunarCraftingStation).
                Register();

            CreateRecipe().
                AddIngredient(ItemID.Cog, CogCount).
                AddIngredient(ItemID.LunarBar, ClassicLunarBarCount).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
