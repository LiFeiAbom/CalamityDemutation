using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Ranged;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 行星湮灭（Planetary Annihilation） - 射手弓，天堂之风链**批 4 第四件**（T1 六把武器之一），
    /// ＝灾厄 2.0 的 <c>Items/Weapons/Ranged/PlanetaryAnnihilation.cs</c>。
    /// <para>
    /// **口径（用户 2026-10-15 拍板）**：中文名 **行星湮灭**（弹幕 **能量球**
    /// <see cref="PlanetaryAnnihilationProj"/>）、**数值膨胀 58 → 126**、面板 / 机制**全取 2.0**；
    /// 配方两版**逐字相同**故只注册一条。
    /// 源 tooltip：<c>Fires a storm of 7 arrows from the sky</c> /
    /// <c>Wooden arrows are converted into homing energy bolts</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：判定 58×102（贴图 60×100；另有 `HoldoutOffset(-5, 0)`）/ 伤害 58 / 使用·动画 22 帧 /
    /// 击退 5.5 / 弹速 12 / 箭类弹药 / 使用音 `Item75` / **紫档** / **1 铂金 20 金**（源 `Rarity11BuyPrice`）。
    /// 2.2.2 是 66 伤、经典版是 75 伤 / 弹速 16（且每次出手在 19~34 间随机）——**都不取**。
    /// </para>
    /// <para>
    /// 出手（照 2.0）：从**玩家上方 600 像素**处（再上抬 100、横向随机散布 ±200）朝鼠标方向洒下
    /// **7 枚**弹幕，速度带 ±2.4 的随机抖动；木箭 → <see cref="PlanetaryAnnihilationProj"/>（`ai[1]` = 0~6 定颜色），
    /// 非木箭 → 照原弹种 ×7 并标记不掉落。
    /// **经典版是"7 个各自独立的 `TerraBall` 弹幕 + 7 张贴图"的老实现**，2.0 已把它收敛成一个隐形弹幕
    /// （见弹幕文件），本工程照 2.0。
    /// </para>
    /// <para>
    /// 与源的一处差别：源的 <c>Shoot</c> 前半段（把鼠标方向化成初速的那几行）紧接着就被后半段**整体覆盖**，
    /// 是**不消耗随机数、无副作用的死算**，本工程按 §15.13 的同样处理删掉（行为完全一致）；
    /// 鼠标坐标统一走 <c>Main.MouseWorld</c>（源的 <c>mouseX + screenPosition</c> 同义，且这里的
    /// <c>Shoot</c> 本来就只在主人端跑）。
    /// </para>
    /// </summary>
    internal class PlanetaryAnnihilation : ModItem
    {
        /// <summary>一次洒下的弹幕枚数（照源 7）</summary>
        private const int ProjectileCount = 7;
        /// <summary>出手点比玩家高多少（照源 600）</summary>
        private const float SpawnHeightAbovePlayer = 600f;
        /// <summary>出手点再上抬的高度（照源 100）</summary>
        private const float ExtraSpawnLift = 100f;
        /// <summary>出手点的横向随机散布（照源 ±200）</summary>
        private const int SpawnLateralJitter = 200;
        /// <summary>初始横向偏移的随机量级（照源 201）</summary>
        private const int SpawnBackwardsRange = 201;
        /// <summary>速度抖动幅度（照源 ±120，实际乘 0.02）</summary>
        private const int VelocityJitter = 120;
        /// <summary>配方里的银河奇点数量（照源 5）</summary>
        private const int GalacticaSingularityCount = 5;
        /// <summary>配方里的月亮锭数量（照源 5）</summary>
        private const int LunarBarCount = 5;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-15 指定：58 → 126）</summary>
        private const float InflatedDamage = 126f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 58</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：判定 58×102（贴图 60×100）/ 伤害 58 / 使用·动画 22 帧 / 击退 5.5 / 弹速 12 /
        /// 箭类弹药 / 使用音 `Item75` / 紫档 / 1 铂金 20 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 58;                          // 照源（贴图 60×100）
            Item.height = 102;
            Item.damage = 58;                         // 面板伤害（照 2.0；2.2.2 是 66、经典版 75）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 5.5f;
            Item.value = Item.buyPrice(1, 20, 0, 0);  // 价值 1 铂金 20 金（源 Rarity11BuyPrice）
            Item.rare = ItemRarityID.Purple;          // 紫档
            Item.UseSound = SoundID.Item75;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PlanetaryAnnihilationProj>();
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>贴图在手上时往左挪 5 像素（照源）</summary>
        public override Vector2? HoldoutOffset() => new Vector2(-5, 0);
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 洒下的 7 枚弹幕都由 <c>Shoot</c> 的 <c>damage</c> 参数派生，会自动跟随
        /// （弹幕消亡时的那发"爆炸"伤害是它自己 ÷2 得来的，同样跟随）。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 从玩家上方洒下 <see cref="ProjectileCount"/> 枚弹幕：木箭换能量球（`ai[1]` 定色），
        /// 其余照原弹种射出并标记不掉落（照 2.0）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float shootSpeed = Item.shootSpeed;

            // 出手点：玩家上方 600 像素、再上抬 100，横向带随机散布
            Vector2 spawnPosition = new Vector2(
                player.position.X + player.width * 0.5f + Main.rand.Next(SpawnBackwardsRange) * -player.direction + (Main.MouseWorld.X - player.position.X),
                player.MountedCenter.Y - SpawnHeightAbovePlayer);
            spawnPosition.X = (spawnPosition.X + player.Center.X) / 2f + Main.rand.Next(-SpawnLateralJitter, SpawnLateralJitter + 1);
            spawnPosition.Y -= ExtraSpawnLift;

            // 朝鼠标的出射速度（照源：Y 分量先取绝对值并夹到最小 20，再整体归一化到弹速）
            float toMouseX = Main.MouseWorld.X - spawnPosition.X;
            float toMouseY = Main.MouseWorld.Y - spawnPosition.Y;
            if (toMouseY < 0f)
                toMouseY *= -1f;
            if (toMouseY < 20f)
                toMouseY = 20f;
            float distance = (float)Math.Sqrt(toMouseX * toMouseX + toMouseY * toMouseY);
            distance = shootSpeed / distance;
            toMouseX *= distance;
            toMouseY *= distance;

            for (int i = 0; i < ProjectileCount; i++)
            {
                float speedX = toMouseX + Main.rand.Next(-VelocityJitter, VelocityJitter + 1) * 0.02f;
                float speedY = toMouseY + Main.rand.Next(-VelocityJitter, VelocityJitter + 1) * 0.02f;

                if (type == ProjectileID.WoodenArrowFriendly)
                {
                    Projectile.NewProjectile(source, spawnPosition.X, spawnPosition.Y, speedX, speedY,
                        ModContent.ProjectileType<PlanetaryAnnihilationProj>(), damage, knockback, player.whoAmI, 0f, i);
                }
                else
                {
                    int proj = Projectile.NewProjectile(source, spawnPosition.X, spawnPosition.Y, speedX, speedY, type, damage, knockback, player.whoAmI);
                    if (Main.projectile.IndexInRange(proj))
                        Main.projectile[proj].noDropItem = true;
                }
            }
            return false;
        }
        /// <summary>
        /// 配方（照源，@月之站台）：**宇宙弩炮（本工程自持件）+ 代达罗斯风暴弓 + 银河奇点×5（本工程自持件）+ 月亮锭×5**。
        /// 2.0 与经典 cal-1.4.2.101 **逐字相同**（只是书写顺序不同）⇒ 只注册一条；
        /// 又因三味全是原版件 / 自持件，**本条配方零软依赖**（链上第一件）。
        /// 2.2.2 额外加了生命合金×5——**不取**。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<CosmicBolter>().
                AddIngredient(ItemID.DaedalusStormbow).
                AddIngredient<GalacticaSingularity>(GalacticaSingularityCount).
                AddIngredient(ItemID.LunarBar, LunarBarCount).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
