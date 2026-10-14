using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 大陆巨弓（Continental Greatbow） - 射手弓，天堂之风链上 `AstrealDefeat` 的三把巨弓之一。
    /// <para>
    /// **身世**：类名取 2.0 源的 `ContinentalGreatbow`；2.0 / 2.2.2 的类上都挂着
    /// <c>[LegacyName("GreatbowofTurmoil")]</c>，说明它由「乱世巨弓」改名而来——
    /// 但**经典版的 `GreatbowofTurmoil` 是另一把武器**（显示名 Greatbow of Turmoil、52 伤 / 17 帧），
    /// 与本件不是同一数值，故只在注释里留档、不当作同一件。
    /// </para>
    /// <para>
    /// **口径（用户 2026-10-14 拍板）**：中文名 **大陆巨弓**、**数值膨胀 34 → 102**、
    /// 面板 / 机制**全取 2.0**；经典分支照 (a) —— 用经典版同名材料 `CruptixBar`（推定＝熔渣锭）
    /// 注册一条、数值共用 2.0。源 tooltip：<c>Wooden arrows are set alight with fire</c> /
    /// <c>Fires 3 arrows at once</c> / <c>Fires 2 additional cursed, hellfire, or ichor arrows</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：贴图 38×68（源把判定写成 **18×36**，照源保留）/ 伤害 34 / 使用·动画 20 帧 /
    /// 击退 4 / 弹速 17 / 箭类弹药 / 使用音 `Item5` / 黄档 / 80 金。
    /// </para>
    /// <para>
    /// 一次射出 **3 支主箭**（速度归一化后 ×40、±π/10）：木箭 → **火焰箭 `FireArrow`**（"点燃"），
    /// 全部标记不掉落；再补 **2 支追加箭**，从 诅咒 / 地狱火 / 灵液 三选一、
    /// **伤害 ×0.5**、±0.5 速度抖动、挂本地命中冷却 10 帧（照源）。
    /// 2.2.2 把伤害提到 45、使用时间拉到 24 帧、追加箭池去掉诅咒箭——**本件按用户口径不取**。
    /// </para>
    /// </summary>
    internal class ContinentalGreatbow : ModItem
    {
        /// <summary>一次射出的主箭数（照源 3）</summary>
        private const int MainArrowCount = 3;
        /// <summary>相邻两主箭的角间隔（照源 <c>MathHelper.Pi * 0.1f</c>）</summary>
        private const float ArrowSpread = MathHelper.Pi / 10f;
        /// <summary>"排箭"用的位移长度（照源 40——只用于排布与贴墙检测，不影响出手速度）</summary>
        private const float BowSpreadDistance = 40f;
        /// <summary>追加箭数量（照源 2）</summary>
        private const int ExtraArrowCount = 2;
        /// <summary>追加箭的伤害倍率（照源 <c>damage * 0.5f</c>）</summary>
        private const float ExtraArrowDamageMult = 0.5f;
        /// <summary>追加箭的速度抖动：<c>Next(-10, 11) × 0.05</c> = ±0.5（照源）</summary>
        private const float ExtraArrowJitter = 0.05f;
        /// <summary>追加箭的本地命中冷却（照源 10）</summary>
        private const int ExtraArrowHitCooldown = 10;
        /// <summary>配方里的锭数（照源 10）</summary>
        private const int BarCount = 10;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-14 指定：34 → 102）</summary>
        private const float InflatedDamage = 102f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 34</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：判定 18×36（照源；贴图是 38×68）/ 伤害 34 / 使用·动画 20 帧 /
        /// 击退 4 / 弹速 17 / 箭类弹药 / 使用音 `Item5` / 黄档 / 80 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 照源（贴图 38×68，判定比贴图小）
            Item.height = 36;
            Item.damage = 34;                         // 面板伤害（照 2.0；2.2.2 是 45）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 80, 0, 0);  // 价值 80 金（照源）
            Item.rare = ItemRarityID.Yellow;          // 黄档
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly;  // 真正射出什么由 Shoot 决定
            Item.shootSpeed = 17f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（3 支主箭用全额、2 支追加箭用其一半），会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 开火（照 2.0）：3 支主箭（木箭 → 火焰箭、全部不掉落）+ 2 支半伤的元素箭（诅咒/地狱火/灵液三选一）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // ① 3 支主箭
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter, true);
            velocity.Normalize();
            velocity *= BowSpreadDistance;
            bool canHit = Collision.CanHit(origin, 0, 0, origin + velocity, 0, 0);

            for (int i = 0; i < MainArrowCount; i++)
            {
                float offsetAmount = i - (MainArrowCount - 1f) / 2f;
                Vector2 offset = velocity.RotatedBy(ArrowSpread * offsetAmount);
                if (!canHit)
                    offset -= velocity;   // 贴墙时把整排箭收回玩家身上（照源）

                int spawnType = type == ProjectileID.WoodenArrowFriendly ? ProjectileID.FireArrow : type;
                int arrow = Projectile.NewProjectile(source, origin + offset, velocity, spawnType, damage, knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(arrow))
                    Main.projectile[arrow].noDropItem = true;
            }

            // ② 2 支追加的元素箭（半伤、带本地命中冷却）
            int[] extraArrowPool = { ProjectileID.CursedArrow, ProjectileID.HellfireArrow, ProjectileID.IchorArrow };
            for (int i = 0; i < ExtraArrowCount; i++)
            {
                float speedX = velocity.X + Main.rand.Next(-10, 11) * ExtraArrowJitter;
                float speedY = velocity.Y + Main.rand.Next(-10, 11) * ExtraArrowJitter;
                int extraType = extraArrowPool[Main.rand.Next(extraArrowPool.Length)];
                int extra = Projectile.NewProjectile(source, position, new Vector2(speedX, speedY), extraType,
                    (int)(damage * ExtraArrowDamageMult), knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(extra))
                {
                    Main.projectile[extra].noDropItem = true;
                    Main.projectile[extra].usesLocalNPCImmunity = true;
                    Main.projectile[extra].localNPCHitCooldown = ExtraArrowHitCooldown;
                }
            }
            return false;
        }
        /// <summary>
        /// 配方（照源，@秘银砧）：熔渣锭×10。唯一外部件是材料**熔渣锭**，走软依赖——
        /// 现代版叫 `ScoriaBar`、经典版是旧名 `CruptixBar`（推定同一件：都 ← 矿×5、30×24、Yellow 档），
        /// 两分支各注册一条。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("ScoriaBar", out ModItem scoriaBar))
            {
                RegisterRecipe(scoriaBar.Type);
            }
            else
            {
                Mod.Logger.Warn("大陆巨弓：现代版灾厄里找不到 ScoriaBar，本条配方未注册。");
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("CruptixBar", out ModItem cruptixBar))
            {
                RegisterRecipe(cruptixBar.Type);
            }
            else
            {
                Mod.Logger.Warn("大陆巨弓：经典版灾厄里找不到 CruptixBar（熔渣锭旧名），本条配方未注册。");
            }
        }
        /// <summary>按给定的"熔渣锭"类型注册配方（站台照源＝秘银砧）</summary>
        private void RegisterRecipe(int barType)
        {
            CreateRecipe().
                AddIngredient(barType, BarCount).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
