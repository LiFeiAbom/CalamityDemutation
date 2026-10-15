using CalamityDemutation.Content.Items.Materials;
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
    /// 星穹悲愿（Astreal Defeat） - 射手弓，天堂之风链**批 4 第五件**（T1 六把武器之一），
    /// ＝灾厄 2.0 的 <c>Items/Weapons/Ranged/AstrealDefeat.cs</c>。
    /// <para>
    /// **口径（用户 2026-10-15 拍板）**：中文名 **星穹悲愿**（弹幕 **星穹箭** <see cref="AstrealArrow"/> /
    /// **星穹烈焰** <see cref="AstrealFlame"/>；官方中文没翻这件，名字由本工程定）、
    /// **数值膨胀 153 → 336**、面板 / 机制**全取 2.0**；配方两版等价且全自持 ⇒ 只注册一条。
    /// 源 tooltip 4 行（3 行背景 + 1 行机制）：<c>Ethereal bow of the tyrant king's mother</c> /
    /// <c>The mother strongly discouraged acts of violence throughout her life</c> /
    /// <c>Though she kept this bow close, to protect her family in times of great disaster</c> /
    /// <c>All arrows are converted to Astreal Arrows that emit flames as they travel</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：40×78 / 伤害 153 / 使用 4 帧·动画 20 帧（每 4 帧一发）/ 击退 5.5 / 弹速 4 /
    /// 箭类弹药 / 使用音 `Item102` / **紫档** / **1 铂金 20 金**（源 `Rarity11BuyPrice`）。
    /// 2.2.2 数值相同但**把三把巨弓从配方里删了**；经典版是 109 伤 / 3 帧 / 弹速 1、
    /// 且名字染成青绿（＝本工程月后 12 档色）——**都不取**。
    /// </para>
    /// <para>
    /// 出手（照 2.0）：**不管用什么弹药，一律换成 <see cref="AstrealArrow"/>**
    /// （源注释：正常弓允许射原版箭，但这把"允许了就强得离谱"），
    /// 出手速度**封顶 8**（超过就按比例缩回），并随机给一个 `ai[0]`（0~3，决定箭的加速方式）。
    /// </para>
    /// </summary>
    internal class AstrealDefeat : ModItem
    {
        /// <summary>出手速度上限（照源：速度 &gt; 8 就按比例缩到 8）</summary>
        private const float MaxArrowSpeed = 8f;
        /// <summary>随机 <c>ai[0]</c> 的档数（照源 <c>Main.rand.Next(4)</c>）</summary>
        private const int AiVariantCount = 4;
        /// <summary>配方里的银河奇点 / 月亮锭数量（照源各 5）</summary>
        private const int GalacticaSingularityCount = 5;
        private const int LunarBarCount = 5;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-15 指定：153 → 336）</summary>
        private const float InflatedDamage = 336f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 153</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：40×78 / 伤害 153 / 使用 4 帧·动画 20 帧 / 击退 5.5 / 弹速 4 /
        /// 箭类弹药 / 使用音 `Item102` / 紫档 / 1 铂金 20 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 40;                          // 贴图宽（像素）
            Item.height = 78;                         // 贴图高（像素）
            Item.damage = 153;                        // 面板伤害（照 2.0）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 4;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 5.5f;
            Item.value = Item.buyPrice(1, 20, 0, 0);  // 价值 1 铂金 20 金（源 Rarity11BuyPrice）
            Item.rare = ItemRarityID.Purple;          // 紫档
            Item.UseSound = SoundID.Item102;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AstrealArrow>();
            Item.shootSpeed = 4f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药（但射出去的永远是星穹箭）
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 星穹箭由 <c>Shoot</c> 的 <c>damage</c> 参数派生、烈焰再由箭的伤害 ×0.5 派生，都会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 一律换成 <see cref="AstrealArrow"/>（速度封顶 <see cref="MaxArrowSpeed"/>），
        /// 并随机给一个 0~3 的 <c>ai[0]</c>（照 2.0）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float speed = velocity.Length();
            if (speed > MaxArrowSpeed)
                velocity *= MaxArrowSpeed / speed;

            type = Item.shoot;
            float aiVariant = Main.rand.Next(AiVariantCount);

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, aiVariant);
            return false;
        }
        /// <summary>
        /// 配方（照 2.0，@月之站台）：灵焰 + 暗影焰弓 + **大陆巨弓 + 剑刃巨弓 + 暗影巨弓（三把都是本工程自持件）**
        /// + 银河奇点×5（自持件）+ 月亮锭×5。
        /// 经典 cal-1.4.2.101 是**同一套料**（经典里"大陆巨弓"叫 `GreatbowofTurmoil`）⇒ 只注册一条；
        /// 又因全是自持件 / 原版件 ⇒ **本条配方零软依赖**。
        /// 2.2.2 把三把巨弓删掉、改成灾厄之烬×5——**不取**。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SpiritFlame).
                AddIngredient(ItemID.ShadowFlameBow).
                AddIngredient<ContinentalGreatbow>().
                AddIngredient<BladedgeGreatbow>().
                AddIngredient<DarkechoGreatbow>().
                AddIngredient<GalacticaSingularity>(GalacticaSingularityCount).
                AddIngredient(ItemID.LunarBar, LunarBarCount).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
