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
    /// 烈风（Galeforce） - 射手弓，天堂之风链**批 4 第三件**（T1 六把武器之一），
    /// ＝灾厄 2.0 的 <c>Items/Weapons/Ranged/Galeforce.cs</c>。
    /// <para>
    /// **口径（用户 2026-10-15 拍板）**：中文名 **烈风**（弹幕 **羽毛** <see cref="FeatherLarge"/>）、
    /// **数值膨胀 18 → 54**、面板 / 机制**全取 2.0**；配方两版**逐字相同**故只注册一条。
    /// 源 tooltip：<c>Fires a spread of low-damage feathers</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：判定 32×52（贴图 34×62）/ 伤害 18 / 使用·动画 17 帧 / 击退 3 / 弹速 20 /
    /// 箭类弹药 / 使用音 `Item5` / 橙档（原版档）/ 4 金。
    /// 2.2.2 是 13 伤 / 20 帧，经典版是 18 伤 / 17 帧但**击退 4 且根本没有羽毛机制**（连 `Shoot` 都没重写）
    /// ——两者的机制与数值都**不取**。
    /// </para>
    /// <para>
    /// 出手（照 2.0）：朝 <c>-8° / 0° / +8°</c> 三个方向各射一根 <see cref="FeatherLarge"/>
    /// （伤害 **÷4**、击退 0），**并且照常把原版箭射出去**（源的 <c>Shoot</c> 返回 <c>true</c>，
    /// 所以这支箭是"羽毛 + 本体箭"一起出）。
    /// </para>
    /// </summary>
    internal class Galeforce : ModItem
    {
        /// <summary>羽毛的散射角度（照源写 <c>for (i = -8; i &lt;= 8; i += 8)</c> 即 -8° / 0° / +8°）</summary>
        private const float FeatherSpreadDegrees = 8f;
        /// <summary>羽毛的伤害除数（照源 <c>damage / 4</c>，是整数除法）</summary>
        private const int FeatherDamageDivisor = 4;
        /// <summary>配方里的空中锭数量（照源 8，两版相同）</summary>
        private const int AerialiteBarCount = 8;
        /// <summary>配方里的日耀板数量（照源 3，两版相同）</summary>
        private const int SunplateBlockCount = 3;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-15 指定：18 → 54）</summary>
        private const float InflatedDamage = 54f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 18</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：判定 32×52（贴图 34×62）/ 伤害 18 / 使用·动画 17 帧 / 击退 3 / 弹速 20 /
        /// 箭类弹药 / 使用音 `Item5` / 橙档 / 4 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 32;                          // 照源（贴图 34×62，判定比贴图略小）
            Item.height = 52;
            Item.damage = 18;                         // 面板伤害（照 2.0；2.2.2 是 13）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 17;
            Item.useAnimation = 17;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 4, 0, 0);   // 价值 4 金（照源）
            Item.rare = ItemRarityID.Orange;          // 橙档（原版档）
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly;  // 原版箭照常射出（见 Shoot 的返回 true）
            Item.shootSpeed = 20f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 羽毛伤害由 <c>Shoot</c> 的 <c>damage</c> 参数 ÷4 派生，会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 洒三根羽毛（±8°、伤害 ÷4、击退 0），**然后照常射出原版箭**——照源返回 <c>true</c>
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = -1; i <= 1; i++)
            {
                Vector2 perturbedSpeed = velocity.RotatedBy(MathHelper.ToRadians(i * FeatherSpreadDegrees));
                Projectile.NewProjectile(source, position, perturbedSpeed, ModContent.ProjectileType<FeatherLarge>(), damage / FeatherDamageDivisor, 0f, player.whoAmI);
            }
            return true;
        }
        /// <summary>
        /// 持握偏移（用户 2026-10-15 对整条链指定的"贴图整体右移"口径）；
        /// <c>HoldoutOffset().X</c> 直接是贴图原点 X（模组默认 10，越小越往右），故取负值。
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);
        /// <summary>
        /// 配方（照源，@天磨坊）：**空中锭 `AerialiteBar`×8** + 日耀板×3。
        /// 两版配方**逐字相同** ⇒ 只注册一条，走"有现代用现代、否则退经典"（同月光弓的写法），
        /// 避免两版灾厄都装时多出一条重复配方。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("AerialiteBar", out ModItem aerialiteBar))
            {
                RegisterRecipe(aerialiteBar.Type);
            }
            else if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("AerialiteBar", out ModItem classicAerialiteBar))
            {
                RegisterRecipe(classicAerialiteBar.Type);
            }
            else
            {
                Mod.Logger.Warn("烈风：两版灾厄里都找不到 AerialiteBar，本条配方未注册。");
            }
        }
        /// <summary>按给定的空中锭类型注册配方（其余两味原版：日耀板×3 @天磨坊）</summary>
        private void RegisterRecipe(int aerialiteBarType)
        {
            CreateRecipe().
                AddIngredient(aerialiteBarType, AerialiteBarCount).
                AddIngredient(ItemID.SunplateBlock, SunplateBlockCount).
                AddTile(TileID.SkyMill).
                Register();
        }
    }
}
