using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 宇宙弩炮（Cosmic Bolter） - 射手弓，天堂之风链上 `PlanetaryAnnihilation` 的入料，
    /// 也是本链第一个"配方吃到自持红利"的件（入料之一是已自持的 <see cref="LunarianBow"/>）。
    /// <para>
    /// 面板 / 支数 / 木箭转化**全取 2.0**；**保留旧名 `CosmicBolter`**
    /// （2.2.2 把它**改名**成 `VernalBolter`——那件身上挂着 <c>[LegacyName("CosmicBolter")]</c>，
    /// 按工程"改名＝同一件、本工程留旧名"的既有口径处理）；经典分支照经典配方注册、数值共用 2.0。
    /// 源 tooltip：<c>Fires three arrows at once</c> / <c>Converts wooden arrows into sliding energy bolts</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：贴图 34×74（源把判定写成 **40×76**，照源保留）/ 伤害 39 / 使用·动画 20 帧 /
    /// 击退 2.75 / 弹速 10 / 箭类弹药 / 使用音 `Item75` / 黄档 / 80 金。
    /// </para>
    /// <para>
    /// 一次射出 **3 支**（速度归一化后 ×30、±π/10 排布）：**任何弹药一律换成 <see cref="LunarBolt2"/>**
    /// （弹药全转化，用户 2026-10-15 对天堂之风**及其整条下位链**统一的口径）。贴墙时整排收回。
    /// 2.2.2 额外给木箭 **×1.2 伤害/击退**、并换成 `VernalBolt`——**本件按用户口径不取**。
    /// </para>
    /// </summary>
    internal class CosmicBolter : ModItem
    {
        /// <summary>一次射出的箭数（照 2.0；经典版是 2）</summary>
        private const int ArrowsPerShot = 3;
        /// <summary>相邻两箭的角间隔（照源 <c>MathHelper.Pi * 0.1f</c>）</summary>
        private const float ArrowSpread = MathHelper.Pi / 10f;
        /// <summary>"排箭"用的位移长度（照源 30——只用于排布与贴墙检测，不影响出手速度）</summary>
        private const float BowSpreadDistance = 30f;
        /// <summary>配方里的活体碎片 / 神圣锭 / 视域之魂数量（照源各 5）</summary>
        private const int LivingShardCount = 5;
        private const int HallowedBarCount = 5;
        private const int SoulOfSightCount = 5;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 39</summary>
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：判定 40×76（照源；贴图本身是 34×74）/ 伤害 39 / 使用·动画 20 帧 /
        /// 击退 2.75 / 弹速 10 / 箭类弹药 / 使用音 `Item75` / 黄档 / 80 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 40;                          // 照源（贴图 34×74，判定比贴图略大）
            Item.height = 76;
            Item.damage = 39;                         // 面板伤害（照 2.0；2.2.2 是 35）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 2.75f;
            Item.value = Item.buyPrice(0, 80, 0, 0);  // 价值 80 金（照源）
            Item.rare = ItemRarityID.Yellow;          // 黄档
            Item.UseSound = SoundID.Item75;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LunarBolt2>();
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（3 支箭都由它派生），会自动跟随。
        /// </summary>
        /// <summary>
        /// 一次射出 <see cref="ArrowsPerShot"/> 支：速度归一化后 ×<see cref="BowSpreadDistance"/>、±π/10 排布；
        /// **任何弹药都换成 <see cref="LunarBolt2"/>**（弹药全转化，用户 2026-10-15 定；
        /// 源 2.0 是"木箭才转化、非木箭照原样射出并标 <c>noDropItem</c>"，本工程按新口径不取）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter, true);

            velocity.Normalize();
            velocity *= BowSpreadDistance;
            bool canHit = Collision.CanHit(origin, 0, 0, origin + velocity, 0, 0);

            for (int i = 0; i < ArrowsPerShot; i++)
            {
                float offsetAmount = i - (ArrowsPerShot - 1f) / 2f;
                Vector2 offset = velocity.RotatedBy(ArrowSpread * offsetAmount);
                if (!canHit)
                    offset -= velocity;   // 贴墙时把整排箭收回玩家身上（照源）

                Projectile.NewProjectile(source, origin + offset, velocity, ModContent.ProjectileType<LunarBolt2>(), damage, knockback, player.whoAmI);
            }
            return false;
        }
        /// <summary>
        /// 持握偏移（用户 2026-10-15 对整条链指定的"贴图整体右移"口径）；
        /// <c>HoldoutOffset().X</c> 直接是贴图原点 X（模组默认 10，越小越往右），故取负值。
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);
        /// <summary>
        /// 配方（照源，@秘银砧）：**月光弓**（本工程自持件，两分支都用我们的）+
        /// 活体碎片×5 + 神圣锭×5 + 视域之魂×5。
        /// 唯一外部件是材料**活体碎片 `LivingShard`**，走软依赖，按版本取名注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("LivingShard", out ModItem livingShard))
            {
                RegisterRecipe(livingShard.Type);
            }
            else
            {
                Mod.Logger.Warn("宇宙弩炮：现代版灾厄里找不到 LivingShard，本条配方未注册。");
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("LivingShard", out ModItem classicLivingShard))
            {
                RegisterRecipe(classicLivingShard.Type);
            }
            else
            {
                Mod.Logger.Warn("宇宙弩炮：经典版灾厄里找不到 LivingShard，本条配方未注册。");
            }
        }
        /// <summary>按给定的活体碎片类型注册配方（其余三味相同：自持月光弓 + 两味原版）</summary>
        private void RegisterRecipe(int livingShardType)
        {
            CreateRecipe().
                AddIngredient<LunarianBow>().
                AddIngredient(livingShardType, LivingShardCount).
                AddIngredient(ItemID.HallowedBar, HallowedBarCount).
                AddIngredient(ItemID.SoulofSight, SoulOfSightCount).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
