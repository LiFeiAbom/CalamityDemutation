using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 暗影巨弓（Darkecho Greatbow） - 射手弓，天堂之风链上 `AstrealDefeat` 的三把巨弓之一。
    /// <para>
    /// **口径（用户 2026-10-14 拍板）**：中文名 **暗影巨弓**、**数值膨胀 37 → 120**、
    /// 面板 / 机制**全取 2.0**；经典分支用经典版同名材料 `VerstaltiteBar`（＝冰晶锭）注册一条、数值共用 2.0。
    /// 源 tooltip：<c>Fires two arrows at once</c> / <c>Fires an additional crystal dart</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：判定 34×62（贴图 32×66）/ 伤害 37 / 使用·动画 22 帧 / 击退 4 / 弹速 12 /
    /// 箭类弹药 / 使用音 `Item5` / 粉档 / 36 金。
    /// </para>
    /// <para>
    /// 一次射出 **2 支弹药**（各 ±1.5 速度抖动、标记不掉落）+ **1 枚原版水晶镖**
    /// （<see cref="ProjectileID.CrystalDart"/>、伤害 **减半**、把 <c>penetrate</c> 改成 3；照源**不**标记不掉落）。
    /// 2.2.2 把伤害提到 45、使用时间拉到 33 帧、冰晶锭 8→12——**本件按用户口径不取**；
    /// 经典版那套"每支 1/3 概率换诅咒箭、没有水晶镖"也不取。
    /// </para>
    /// </summary>
    internal class DarkechoGreatbow : ModItem
    {
        /// <summary>一次射出的弹药数（照源 2）</summary>
        private const int ArrowCount = 2;
        /// <summary>弹药速度抖动：<c>Next(-30, 31) × 0.05</c> = ±1.5（照源）</summary>
        private const float ArrowJitter = 0.05f;
        /// <summary>水晶镖的穿透值（照源：生成后把 <c>penetrate</c> 改成 3）</summary>
        private const int CrystalDartPenetrate = 3;
        /// <summary>配方里的锭数（照源 8；2.2.2 是 12）</summary>
        private const int BarCount = 8;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-14 指定：37 → 120）</summary>
        private const float InflatedDamage = 120f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 37</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：判定 34×62（照源；贴图 32×66）/ 伤害 37 / 使用·动画 22 帧 /
        /// 击退 4 / 弹速 12 / 箭类弹药 / 使用音 `Item5` / 粉档 / 36 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 34;                          // 照源（贴图 32×66）
            Item.height = 62;
            Item.damage = 37;                         // 面板伤害（照 2.0；2.2.2 是 45）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 36, 0, 0);  // 价值 36 金（照源）
            Item.rare = ItemRarityID.Pink;            // 粉档
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.PurificationPowder;  // 占位：真正射出什么由 Shoot 决定
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（2 支弹药用全额、水晶镖用其一半），会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 开火（照 2.0）：2 支弹药（各 ±1.5 抖动、不掉落）+ 1 枚半伤水晶镖（穿透改成 3）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0; i < ArrowCount; i++)
            {
                float speedX = velocity.X + Main.rand.Next(-30, 31) * ArrowJitter;
                float speedY = velocity.Y + Main.rand.Next(-30, 31) * ArrowJitter;
                int arrow = Projectile.NewProjectile(source, position.X, position.Y, speedX, speedY, type, damage, knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(arrow))
                    Main.projectile[arrow].noDropItem = true;
            }

            int dart = Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, ProjectileID.CrystalDart, damage / 2, knockback, player.whoAmI);
            if (Main.projectile.IndexInRange(dart))
                Main.projectile[dart].penetrate = CrystalDartPenetrate;
            return false;
        }
        /// <summary>
        /// 配方（照源，@秘银砧）：冰晶锭×8。唯一外部件是材料**冰晶锭**，走软依赖——
        /// 现代版叫 `CryonicBar`、经典版与 1.3-release 是旧名 `VerstaltiteBar`（同一件改名，见 §3），
        /// 两分支各注册一条（两版数量都是 8，配方逐字对应）。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("CryonicBar", out ModItem cryonicBar))
            {
                RegisterRecipe(cryonicBar.Type);
            }
            else
            {
                Mod.Logger.Warn("暗影巨弓：现代版灾厄里找不到 CryonicBar，本条配方未注册。");
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("VerstaltiteBar", out ModItem verstaltiteBar))
            {
                RegisterRecipe(verstaltiteBar.Type);
            }
            else
            {
                Mod.Logger.Warn("暗影巨弓：经典版灾厄里找不到 VerstaltiteBar（冰晶锭旧名），本条配方未注册。");
            }
        }
        /// <summary>按给定的"冰晶锭"类型注册配方（站台照源＝秘银砧）</summary>
        private void RegisterRecipe(int barType)
        {
            CreateRecipe().
                AddIngredient(barType, BarCount).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
