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
    /// 炎翼弓（Flarewing Bow） - 射手弓，天堂之风链上**月光弓的下一件**（`CosmicBolter` 的入料之一，
    /// 也是 `Monsoon` 的入料）。
    /// <para>
    /// **口径（用户 2026-10-14 拍板）**：中文名 **炎翼弓**、数值膨胀 **17 → 54**，
    /// 面板 / 支数（5 支）/ 蝙蝠机制 / 非木箭处理**全取 2.0**；经典分支照经典版配方注册、数值共用 2.0。
    /// 源 tooltip：<c>Shoots a spread of 5 arrows</c> / <c>Wooden arrows are converted into bouncing obsidian bats</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：40×72 / 伤害 17 / 使用·动画 30 帧 / 击退 1.5 / 弹速 16 / 箭类弹药 /
    /// 使用音 `Item5` / 浅红稀有度（原版档）/ 12 金（源 `Rarity4BuyPrice`）。
    /// 2.0.3.9 起灾厄把它压成"3 支 + 31 伤 + `CheckWoodenAmmo`"、2.2.2 再把使用时间拉到 35 帧、
    /// 蝙蝠改成"命中挂燃烧、穿透一次后追踪"，**本件按用户口径都不取**。
    /// </para>
    /// <para>
    /// 一次射出 **5 支**（±2 × π/10 = **±36°**）：**任何弹药一律换成 <see cref="FlareBat"/>**
    /// （弹药全转化，用户 2026-10-15 对天堂之风**及其整条下位链**统一的口径）。
    /// 贴墙时整排收回玩家身上（照源的 <c>canHit</c> 判据）。
    /// </para>
    /// </summary>
    internal class FlarewingBow : ModItem
    {
        /// <summary>一次射出的箭数（照 2.0；2.0.3.9 起是 3）</summary>
        private const int ArrowsPerShot = 5;
        /// <summary>相邻两箭的角间隔（照源 <c>0.314159274f</c> = π/10）</summary>
        private const float ArrowSpread = MathHelper.Pi / 10f;
        /// <summary>沿弓身"排箭"的位移长度（照源 50——只用于排布与贴墙检测，不影响出手速度）</summary>
        private const float BowSpreadDistance = 50f;
        /// <summary>配方里的日光精华数量（照源）</summary>
        private const int SunlightEssenceCount = 5;
        /// <summary>配方里的活火块 / 黑曜石数量（照源）</summary>
        private const int LivingFireBlockCount = 50;
        private const int ObsidianCount = 10;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-14 指定：17 → 54）</summary>
        private const float InflatedDamage = 54f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 17</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：40×72 / 伤害 17 / 使用·动画 30 帧 / 击退 1.5 / 弹速 16 / 箭类弹药 /
        /// 使用音 `Item5` / 浅红稀有度 / 12 金（全照 2.0）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 40;                          // 贴图宽（像素）
            Item.height = 72;                         // 贴图高（像素）
            Item.damage = 17;                         // 面板伤害（照 2.0；2.0.3.9 起是 31）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 1.5f;
            Item.value = Item.buyPrice(0, 12, 0, 0);  // 价值 12 金（源 Rarity4BuyPrice）
            Item.rare = ItemRarityID.LightRed;        // 浅红（原版档）
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly;  // 真正射出什么由 Shoot 决定
            Item.shootSpeed = 16f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（5 支箭都由它派生），会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 一次射出 <see cref="ArrowsPerShot"/> 支：沿弓身做 ±2 × π/10（±36°）排布、只挪箭点不散射速度；
        /// **任何弹药都换成 <see cref="FlareBat"/>**（弹药全转化，用户 2026-10-15 定；
        /// 源 2.0 是"木箭才转化、非木箭照原弹种射出"，本工程按新口径不取）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter, true);

            Vector2 bowOffset = velocity;
            bowOffset.Normalize();
            bowOffset *= BowSpreadDistance;
            bool canHit = Collision.CanHit(origin, 0, 0, origin + bowOffset, 0, 0);

            for (int i = 0; i < ArrowsPerShot; i++)
            {
                float offsetAmount = i - (ArrowsPerShot - 1f) / 2f;
                Vector2 offset = bowOffset.RotatedBy(ArrowSpread * offsetAmount);
                if (!canHit)
                    offset -= bowOffset;   // 贴墙时把整排箭收回玩家身上（照源）

                Projectile.NewProjectile(source, origin + offset, velocity, ModContent.ProjectileType<FlareBat>(), damage, knockback, player.whoAmI);
            }
            return false;
        }
        /// <summary>
        /// 持握偏移（用户 2026-10-15 对整条链指定的"贴图整体右移"口径）；
        /// <c>HoldoutOffset().X</c> 直接是贴图原点 X（模组默认 10，越小越往右），故取负值。
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);
        /// <summary>
        /// 配方（照源，@秘银砧）：地狱之翼弓 + 日光精华×5 + 活火块×50 + 黑曜石×10。
        /// 唯一外部件是**日光精华**，走软依赖：现代版叫 `EssenceofSunlight`、经典版是旧名 `EssenceofCinder`
        /// （同一件改名的既有口径），两分支各注册一条。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceOfSunlight))
            {
                RegisterRecipe(essenceOfSunlight.Type);
            }
            else
            {
                Mod.Logger.Warn("炎翼弓：现代版灾厄里找不到 EssenceofSunlight，本条配方未注册。");
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("EssenceofCinder", out ModItem essenceOfCinder))
            {
                RegisterRecipe(essenceOfCinder.Type);
            }
            else
            {
                Mod.Logger.Warn("炎翼弓：经典版灾厄里找不到 EssenceofCinder，本条配方未注册。");
            }
        }
        /// <summary>按给定的"日光精华"类型注册配方（其余三味相同，站台照源＝秘银砧）</summary>
        private void RegisterRecipe(int sunlightEssenceType)
        {
            CreateRecipe().
                AddIngredient(ItemID.HellwingBow).
                AddIngredient(sunlightEssenceType, SunlightEssenceCount).
                AddIngredient(ItemID.LivingFireBlock, LivingFireBlockCount).
                AddIngredient(ItemID.Obsidian, ObsidianCount).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
