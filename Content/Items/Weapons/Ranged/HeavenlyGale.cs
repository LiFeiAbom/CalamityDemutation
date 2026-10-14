using CalamityDemutation.Content.Projectiles.Ranged;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 天堂之风（Heavenly Gale） - 射手弓。**灾厄 2.0.3.9 起被重做**成"蓄力星流水晶弓"（现行 2.2.2 就是重做版），
    /// 本件是"旧版回归"：取 **CI Beta1.12 的 `HeavenlyGaleold`（含 ExoLore 传颂之物分支）** 那一版形态。
    /// <para>
    /// **口径（用户 2026-10-14 逐条拍板）**：形态取 **CI（ExoLore 分支）**、基础伤害 **208**、
    /// 数值膨胀 **208 → 720**、配方取 **2.0**、中文名「天堂之风」、带发光层。
    /// 照工程既有口径**恒处 Lore 模式**（同第 9 节 ExoBlade / ExoBeam / 归墟之灵的处理）：
    /// 不搬 CI 的 `LoreExo / PanelsLoreExo` 判定、不留非 Lore 分支，也不搬 CI 的
    /// `CIConfig.AmmoConversion` 开关（即"木箭必转星流箭"这一条恒为开）。
    /// </para>
    /// <para>
    /// 面板（照 CI 的 Lore 分支）：46×98 / 使用·动画 9·18 帧 / 击退 4 / 弹速 12 / 箭类弹药 /
    /// 一次射出 **7 支**（源非 Lore 分支是 5 支）/ 66% 不消耗弹药 / 月后稀有度 15 档 / 1 铂金 50 金。
    /// 七支的取型照源的 5 元素数组（橙出现两次）：青 20% / 橙 40% / 绿 20% / 蓝 20%；
    /// 其中**绿、橙两色会追踪敌人**（见各自弹幕）。用非木箭则不打折扣地射出 7 支原箭、伤害 ×1.25 且不掉落。
    /// </para>
    /// </summary>
    internal class HeavenlyGale : ModItem
    {
        /// <summary>非木箭的伤害倍率（照源 <c>NormalArrowDamageMult</c>）</summary>
        private const float NormalArrowDamageMult = 1.25f;
        /// <summary>省弹率（照源：<c>rand(0,100) &lt; 66</c> 即不消耗，也就是 66%）</summary>
        private const int AmmoSavePercent = 66;
        /// <summary>一次射出的箭数（照 CI 的 Lore 分支：7；非 Lore 分支是 5）</summary>
        private const int ArrowsPerShot = 7;
        /// <summary>相邻两箭的角间隔（照源 <c>MathHelper.Pi / 10</c>）</summary>
        private const float ArrowSpread = MathHelper.Pi / 10f;
        /// <summary>沿弓身"排箭"的位移长度（照源 40——与弹速无关，只是把七支箭头摆在大弓上）</summary>
        private const float BowSpreadDistance = 40f;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-14 指定：208 → 720）</summary>
        private const float InflatedDamage = 720f;
        /// <summary>
        /// 木箭要变成的星流箭池（照 CI 的 `ExoArrowsExoLore`）：**橙色故意出现两次**，
        /// 于是五等分的实际概率是青 20% / 橙 40% / 绿 20% / 蓝 20%
        /// </summary>
        private static int[] ExoArrows;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 208</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份；顺带把星流箭池填好（照源的写法，在静态初始化里取弹幕类型）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ExoArrows = new[]
            {
                ModContent.ProjectileType<TealExoArrow>(),
                ModContent.ProjectileType<OrangeExoArrow>(),
                ModContent.ProjectileType<OrangeExoArrow>(),
                ModContent.ProjectileType<GreenExoArrow>(),
                ModContent.ProjectileType<BlueExoArrow>()
            };
        }
        /// <summary>
        /// 基础属性：46×98 / 伤害 208（用户指定，源 CI 是 508）/ 使用·动画 9·18 帧 / 击退 4 / 弹速 12 /
        /// 箭类弹药 / 使用音 `Item5` / 月后稀有度 15 档 / 1 铂金 50 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 46;                          // 贴图宽（像素）
            Item.height = 98;                         // 贴图高（像素）
            Item.damage = 208;                        // 面板伤害（用户 2026-10-14 指定；源 CI 是 508）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 9;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly;  // 木箭只是"入口"，真正射出的是星流箭池
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = Item.buyPrice(1, 50, 0, 0);  // 价值 1 铂金 50 金（＝灾厄 Violet 档价目）
            Item.rare = ItemRarityID.Red;             // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;  // 月后 15：紫（＝灾厄 Violet / CI CatalystViolet）
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（七支箭的伤害都由它派生），会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>66% 不消耗弹药（照源：<c>rand(0,100) &lt; 66</c> 时不消耗）</summary>
        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.Next(100) >= AmmoSavePercent;
        /// <summary>
        /// 一次射出 <see cref="ArrowsPerShot"/> 支：以玩家中心为源、沿弓身做 ±3 × (π/10) 的扇形排布
        /// （位移只挪出箭点、不散射速度）；贴墙时改把整排箭收回到玩家身上（照源的 <c>againstWall</c>）。
        /// 木箭 → 随机星流箭（伤害照传入口径，Lore 分支没有惩罚）；非木箭 → 7 支原箭、伤害 ×1.25 且不掉落。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter, true);

            Vector2 bowOffset = velocity;
            bowOffset.Normalize();
            bowOffset *= BowSpreadDistance;

            bool againstWall = !Collision.CanHit(origin, 0, 0, origin + bowOffset, 0, 0);

            for (int i = 0; i < ArrowsPerShot; i++)
            {
                float offsetAmount = i - (ArrowsPerShot - 1f) / 2f;
                Vector2 offset = bowOffset.RotatedBy(ArrowSpread * offsetAmount);
                if (againstWall)
                    offset -= bowOffset;

                if (type == ProjectileID.WoodenArrowFriendly)
                {
                    int arrowType = Main.rand.Next(ExoArrows);
                    Projectile.NewProjectile(source, origin + offset, velocity, arrowType, damage, knockback, player.whoAmI);
                }
                else
                {
                    int arrow = Projectile.NewProjectile(source, origin + offset, velocity, type, (int)(damage * NormalArrowDamageMult), knockback, player.whoAmI);
                    if (Main.projectile.IndexInRange(arrow))
                        Main.projectile[arrow].noDropItem = true;   // 转化出来的原箭不掉落（照源）
                }
            }
            return false;
        }
        /// <summary>地上那件的发光层（照 CI 的同名 Glow 贴图）</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
            => Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Ranged/HeavenlyGaleGlow").Value);
        /// <summary>
        /// 配方照 **2.0**（用户 2026-10-14 指定）：七件武器 @ 嘉登熔炉；
        /// 经典分支去掉奇迹物质（经典版没有它，与超新星等件同口径）。
        /// 两分支各自注册一条，缺件写警告、不静默消失。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("PlanetaryAnnihilation", out ModItem planetaryAnnihilation) &&
                calamity.TryFind<ModItem>("Alluvion", out ModItem alluvion) &&
                calamity.TryFind<ModItem>("AstrealDefeat", out ModItem astrealDefeat) &&
                calamity.TryFind<ModItem>("ClockworkBow", out ModItem clockworkBow) &&
                calamity.TryFind<ModItem>("Galeforce", out ModItem galeforce) &&
                calamity.TryFind<ModItem>("TheBallista", out ModItem theBallista) &&
                calamity.TryFind<ModItem>("MiracleMatter", out ModItem miracleMatter) &&
                calamity.TryFind<ModTile>("DraedonsForge", out ModTile forge))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(planetaryAnnihilation.Type);
                recipe.AddIngredient(alluvion.Type);
                recipe.AddIngredient(astrealDefeat.Type);
                recipe.AddIngredient(clockworkBow.Type);
                recipe.AddIngredient(galeforce.Type);
                recipe.AddIngredient(theBallista.Type);
                recipe.AddIngredient(miracleMatter.Type);
                recipe.AddTile(forge.Type);
                recipe.Register();
            }
            else
            {
                Mod.Logger.Warn("天堂之风：现代版灾厄里缺件（2.0 配方的七味 / 嘉登熔炉），本条配方未注册。");
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("PlanetaryAnnihilation", out ModItem classicPlanetaryAnnihilation) &&
                classic.TryFind<ModItem>("Alluvion", out ModItem classicAlluvion) &&
                classic.TryFind<ModItem>("AstrealDefeat", out ModItem classicAstrealDefeat) &&
                classic.TryFind<ModItem>("ClockworkBow", out ModItem classicClockworkBow) &&
                classic.TryFind<ModItem>("Galeforce", out ModItem classicGaleforce) &&
                classic.TryFind<ModItem>("TheBallista", out ModItem classicTheBallista) &&
                classic.TryFind<ModTile>("DraedonsForge", out ModTile classicForge))
            {
                Recipe classicRecipe = CreateRecipe();
                classicRecipe.AddIngredient(classicPlanetaryAnnihilation.Type);
                classicRecipe.AddIngredient(classicAlluvion.Type);
                classicRecipe.AddIngredient(classicAstrealDefeat.Type);
                classicRecipe.AddIngredient(classicClockworkBow.Type);
                classicRecipe.AddIngredient(classicGaleforce.Type);
                classicRecipe.AddIngredient(classicTheBallista.Type);
                classicRecipe.AddTile(classicForge.Type);
                classicRecipe.Register();
            }
            else
            {
                Mod.Logger.Warn("天堂之风：经典版灾厄里缺件（配方七味去掉奇迹物质 / 嘉登熔炉），本条配方未注册。");
            }
        }
    }
}
