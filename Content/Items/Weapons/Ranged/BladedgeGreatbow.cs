using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 剑刃巨弓（源的英文显示名是 **Bladedge Railbow**，类名 <c>BladedgeGreatbow</c>） - 射手弓，
    /// 天堂之风链上 `AstrealDefeat` 的入料之一；**灾厄 2.2.2 已删**，属"旧版回归"重点件。
    /// <para>
    /// **口径（用户 2026-10-14 拍板）**：中文名 **剑刃巨弓**、数值膨胀 **28 → 60**、
    /// 面板 / 叶子 / 分支处理**全取 2.0**；经典分支照经典版配方注册、数值共用 2.0
    /// （经典那套"5 发 + 1/5 概率绿藻箭 + 无叶子"**不取**）。
    /// 源 tooltip：<c>Fires 4 arrows at once</c> / <c>Fires 2 additional leaves</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：74×24 横长弓 / 伤害 28 / 使用·动画 26 帧 / 击退 3.5 / 弹速 14 / 箭类弹药 /
    /// 使用音 `Item5` / 青柠稀有度 / 60 金；持握偏移 `(-10, 0)`（照源）。
    /// </para>
    /// <para>
    /// 一次射出 **4 发弹药本体**（每发各自 ±3 速度抖动、标记不掉落）+ **2 片原版树叶**
    /// （<see cref="ProjectileID.Leaf"/>，伤害 **减半**、±π/10 排布、贴墙时收回）。
    /// 源给叶子挂的是灾厄的 <c>Calamity().forceRanged</c>（把它算成射手伤害）——本工程没有这套 API，
    /// 直接写等价的 <c>DamageType = DamageClass.Ranged</c>，意图一致。
    /// </para>
    /// </summary>
    internal class BladedgeGreatbow : ModItem
    {
        /// <summary>一次射出的弹药数（照 2.0）</summary>
        private const int AmmoShots = 4;
        /// <summary>每发弹药的速度抖动：<c>Next(-60, 61) × 0.05</c> = ±3（照源）</summary>
        private const float SpreadJitter = 0.05f;
        /// <summary>一次附带射出的树叶数（照 2.0）</summary>
        private const int LeavesPerShot = 2;
        /// <summary>两片叶子的角间隔（照源 <c>0.314159274f</c> = π/10）</summary>
        private const float LeafSpread = MathHelper.Pi / 10f;
        /// <summary>叶子"排布位移"的长度（照源 10——只用于排布与贴墙检测，不影响出手速度）</summary>
        private const float LeafSpreadDistance = 10f;
        /// <summary>配方里的锭数（照源 12）</summary>
        private const int BarCount = 12;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-14 指定：28 → 60）</summary>
        private const float InflatedDamage = 60f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 28</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：74×24 / 伤害 28 / 使用·动画 26 帧 / 击退 3.5 / 弹速 14 / 箭类弹药 /
        /// 使用音 `Item5` / 青柠稀有度 / 60 金（全照 2.0）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 74;                          // 贴图宽（像素）
            Item.height = 24;                         // 贴图高（像素）
            Item.damage = 28;                         // 面板伤害（照 2.0）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 3.5f;
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金（照源）
            Item.rare = ItemRarityID.Lime;            // 青柠
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.PurificationPowder;  // 占位：真正射出什么由 Shoot 决定
            Item.shootSpeed = 14f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>持握偏移：照源的 `(-10, 0)`</summary>
        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（4 发弹药用全额、2 片叶子用其一半），会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 开火（照 2.0）：4 发弹药本体各带 ±3 速度抖动、标记不掉落；再补 2 片树叶（伤害减半、±π/10、贴墙收回）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // ① 4 发弹药本体（照源：每发各自抖动）
            for (int i = 0; i < AmmoShots; i++)
            {
                float speedX = velocity.X + Main.rand.Next(-60, 61) * SpreadJitter;
                float speedY = velocity.Y + Main.rand.Next(-60, 61) * SpreadJitter;
                int arrow = Projectile.NewProjectile(source, position.X, position.Y, speedX, speedY, type, damage, knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(arrow))
                    Main.projectile[arrow].noDropItem = true;
            }

            // ② 2 片树叶（照源：伤害减半、无击退、贴墙收回）
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter, true);
            Vector2 leafOffset = velocity;
            leafOffset.Normalize();
            leafOffset *= LeafSpreadDistance;
            bool canHit = Collision.CanHit(origin, 0, 0, origin + leafOffset, 0, 0);

            for (int i = 0; i < LeavesPerShot; i++)
            {
                float offsetAmount = i - (LeavesPerShot - 1f) / 2f;
                Vector2 offset = leafOffset.RotatedBy(LeafSpread * offsetAmount);
                if (!canHit)
                    offset -= leafOffset;

                int leaf = Projectile.NewProjectile(source, origin + offset, velocity, ProjectileID.Leaf, damage / 2, 0f, player.whoAmI);
                // 源用灾厄的 forceRanged 把原版叶子算作射手伤害；这里直接写 DamageType，等价
                if (Main.projectile.IndexInRange(leaf))
                    Main.projectile[leaf].DamageType = DamageClass.Ranged;
            }
            return false;
        }
        /// <summary>
        /// 配方（照源，@秘银砧）：永恒锭×12。唯一外部件是**永恒锭**，走软依赖——
        /// 现代版叫 `PerennialBar`，经典版是旧名 `DraedonBar`（两者都 ← `PerennialOre`×5，同一件改名），
        /// 两分支各注册一条。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("PerennialBar", out ModItem perennialBar))
            {
                RegisterRecipe(perennialBar.Type);
            }
            else
            {
                Mod.Logger.Warn("剑刃巨弓：现代版灾厄里找不到 PerennialBar，本条配方未注册。");
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("DraedonBar", out ModItem draedonBar))
            {
                RegisterRecipe(draedonBar.Type);
            }
            else
            {
                Mod.Logger.Warn("剑刃巨弓：经典版灾厄里找不到 DraedonBar（永恒锭旧名），本条配方未注册。");
            }
        }
        /// <summary>按给定的"永恒锭"类型注册配方（站台照源＝秘银砧）</summary>
        private void RegisterRecipe(int barType)
        {
            CreateRecipe().
                AddIngredient(barType, BarCount).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
