using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 季风（Monsoon） - 射手弓，天堂之风链上 `Alluvion` 的入料，**批 2 的末件**；
    /// 本链目前最重的一件：自己带一整条 5 枚弹幕的链。
    /// <para>
    /// 面板 / 机制 / 弹幕链**全取 2.0**；经典分支照经典版配方注册、数值共用 2.0。
    /// 源 tooltip：<c>Fires a spread of 5 arrows</c> / <c>Wooden arrows are converted into typhoon arrows and sharks</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：46×78 / 伤害 121 / 使用·动画 20 帧 / 击退 2.5 / 弹速 12 / 箭类弹药 /
    /// 使用音 `Item5` / **稀有度 PureGreen＝本工程 13 档（荧光绿）** / 价值 **1 铂金 30 金**（源 `Rarity13BuyPrice`）。
    /// </para>
    /// <para>
    /// 一次射出 **5 支**（速度归一化后 ×40、±π/10 排布），**任何弹药都按位次分工**
    /// （弹药全转化，用户 2026-10-15 对天堂之风**及其整条下位链**统一的口径）：
    /// **第 3 支（p=2）→ <see cref="TyphoonArrow"/>**、**其余 4 支 → <see cref="MiniSharkron"/>**，
    /// 伤害 **×1.1**，生成后补 <c>arrow = true</c> 与 <c>extraUpdates += 1</c>；
    /// 贴墙时整排收回。源 2.0 对非木箭是"照原弹种射出并标记不掉落"——**按新口径不取**。
    /// 2.2.2 把伤害提到 156 并把配方砍到 4 味——**本件按用户口径不取**。
    /// </para>
    /// </summary>
    internal class Monsoon : ModItem
    {
        /// <summary>一次射出的箭数（照源 5）</summary>
        private const int ArrowsPerShot = 5;
        /// <summary>相邻两箭的角间隔（照源 <c>MathHelper.Pi * 0.1f</c>）</summary>
        private const float ArrowSpread = MathHelper.Pi / 10f;
        /// <summary>"排箭"用的位移长度（照源 40——只用于排布与贴墙检测，不影响出手速度）</summary>
        private const float BowSpreadDistance = 40f;
        /// <summary>转化出来的台风箭 / 小鲨鱼龙的伤害倍率（照源 <c>damage * 1.1</c>）</summary>
        private const float ConvertedDamageMult = 1.1f;
        /// <summary>台风箭所在的那一支（源 <c>case 2</c>，即第 3 支）</summary>
        private const int TyphoonArrowIndex = 2;
        /// <summary>现代分支配方里的材料数量（照 2.0）</summary>
        private const int ReaperToothCount = 6;
        private const int SharkFinCount = 2;
        private const int DepthCellsCount = 10;
        private const int LumenylCount = 10;
        private const int TenebrisCount = 5;
        /// <summary>经典分支配方里的星璇碎片数量（照经典版）</summary>
        private const int VortexFragmentCount = 15;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 121</summary>
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：46×78 / 伤害 121 / 使用·动画 20 帧 / 击退 2.5 / 弹速 12 / 箭类弹药 /
        /// 使用音 `Item5` / 月后 13 档（荧光绿）/ 1 铂金 30 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 46;                          // 贴图宽（像素）
            Item.height = 78;                         // 贴图高（像素）
            Item.damage = 121;                        // 面板伤害（照 2.0；2.2.2 是 156）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 2.5f;
            Item.value = Item.buyPrice(1, 30, 0, 0);  // 价值 1 铂金 30 金（源 Rarity13BuyPrice）
            Item.rare = ItemRarityID.Red;             // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后 13：荧光绿（＝灾厄 PureGreen）
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly;  // 真正射出什么由 Shoot 决定
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（5 支全由它派生），会自动跟随。
        /// </summary>
        /// <summary>
        /// 一次射出 <see cref="ArrowsPerShot"/> 支：**任何弹药都按位次分工**
        /// （1 支台风箭 + 4 支小鲨鱼龙，伤害 ×1.1；弹药全转化，用户 2026-10-15 定）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter, true);

            velocity.Normalize();
            velocity *= BowSpreadDistance;
            bool canHit = Collision.CanHit(origin, 0, 0, origin + velocity, 0, 0);

            for (int p = 0; p < ArrowsPerShot; p++)
            {
                float offsetAmount = p - (ArrowsPerShot - 1f) / 2f;
                Vector2 offset = velocity.RotatedBy(ArrowSpread * offsetAmount);
                if (!canHit)
                    offset -= velocity;   // 贴墙时把整排箭收回玩家身上（照源）

                int convertedType = p == TyphoonArrowIndex
                    ? ModContent.ProjectileType<TyphoonArrow>()
                    : ModContent.ProjectileType<MiniSharkron>();
                int proj = Projectile.NewProjectile(source, origin.X + offset.X, origin.Y + offset.Y, velocity.X, velocity.Y,
                    convertedType, (int)(damage * ConvertedDamageMult), knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(proj))
                {
                    Main.projectile[proj].arrow = true;         // 让这两类也算"箭"（照源）
                    Main.projectile[proj].extraUpdates += 1;    // 再快一档（照源）
                }
            }
            return false;
        }
        /// <summary>
        /// 持握偏移（用户 2026-10-15 对整条链指定的"贴图整体右移"口径）；
        /// <c>HoldoutOffset().X</c> 直接是贴图原点 X（模组默认 10，越小越往右），故取负值。
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);
        /// <summary>
        /// 配方（分版本，缺件写警告不静默）：
        /// **现代**（照 2.0）= 海啸 + **炎翼弓（本工程自持件）** + 猎魂鲨牙×6 + 鲨鱼鳍×2 +
        /// 深渊细胞×10 + **流明晶 `Lumenyl`×10** + **暮色碎片 `PlantyMush`×5** @月台；
        /// **经典**（照 cal-1.4.2.101，逐字不同、且不含炎翼弓）= 星璇碎片×15 + 海啸 + 鲨鱼鳍×2 +
        /// 深渊细胞×10 + **`Lumenite`×10** + **`Tenebris`×5** @月台。
        /// 两味"改名对"（暮色碎片 / 流明晶）见 AGENTS §3。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("ReaperTooth", out ModItem reaperTooth) &&
                calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells) &&
                calamity.TryFind<ModItem>("Lumenyl", out ModItem lumenyl) &&
                calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.Tsunami);
                recipe.AddIngredient<FlarewingBow>();
                recipe.AddIngredient(reaperTooth.Type, ReaperToothCount);
                recipe.AddIngredient(ItemID.SharkFin, SharkFinCount);
                recipe.AddIngredient(depthCells.Type, DepthCellsCount);
                recipe.AddIngredient(lumenyl.Type, LumenylCount);
                recipe.AddIngredient(plantyMush.Type, TenebrisCount);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
            else
            {
                Mod.Logger.Warn("季风：现代版灾厄里缺件（ReaperTooth / DepthCells / Lumenyl / PlantyMush），本条配方未注册。");
            }

            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("DepthCells", out ModItem classicDepthCells) &&
                classic.TryFind<ModItem>("Lumenite", out ModItem lumenite) &&
                classic.TryFind<ModItem>("Tenebris", out ModItem tenebris))
            {
                Recipe classicRecipe = CreateRecipe();
                classicRecipe.AddIngredient(ItemID.FragmentVortex, VortexFragmentCount);
                classicRecipe.AddIngredient(ItemID.Tsunami);
                classicRecipe.AddIngredient(ItemID.SharkFin, SharkFinCount);
                classicRecipe.AddIngredient(classicDepthCells.Type, DepthCellsCount);
                classicRecipe.AddIngredient(lumenite.Type, LumenylCount);
                classicRecipe.AddIngredient(tenebris.Type, TenebrisCount);
                classicRecipe.AddTile(TileID.LunarCraftingStation);
                classicRecipe.Register();
            }
            else
            {
                Mod.Logger.Warn("季风：经典版灾厄里缺件（DepthCells / Lumenite / Tenebris），本条配方未注册。");
            }
        }
    }
}
