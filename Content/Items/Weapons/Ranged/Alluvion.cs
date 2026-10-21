using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 潮涌（Alluvion） - 射手弓，天堂之风链**批 4 的收尾件**（T1 六把武器之一），
    /// ＝灾厄 2.0 的 <c>Items/Weapons/Ranged/Alluvion.cs</c>。
    /// <para>
    /// **口径（用户 2026-10-15 拍板）**：中文名 **潮涌**（弹幕 **洪流箭** <see cref="TorrentialArrow"/>；
    /// 面板 / 机制**全取 2.0**；配方**分版本各注册一条**。
    /// 源 tooltip：<c>Converts wooden arrows into sharks, torrential and typhoon arrows</c> /
    /// <c>Fires a torrent of six arrows at once</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：62×90 / 伤害 165 / 使用 15 帧·动画 30 帧 / 击退 4 / 弹速 12 / 箭类弹药 /
    /// 使用音 `Item5` / **月后 14 档（蓝，＝灾厄 `DarkBlue` 43,96,222）** / 价值 **1 铂金 80 金**。
    /// 2.2.2 砍到 78 伤、经典版是 90 伤 / 使用 9 帧·动画 18 帧 / 弹速 17——**都不取**。
    /// </para>
    /// <para>
    /// 出手（照 2.0 的结构）：一次 **6 支**（速度归一化后 ×35、±π/10 排布、贴墙时整排收回），
    /// **任何弹药都按位次分工**（弹药全转化，用户 2026-10-15 对天堂之风**及其整条下位链**统一的口径）——
    /// 第 1/6 支 → <see cref="TyphoonArrow"/>、第 2/5 支 → <see cref="MiniSharkron"/>、
    /// 第 3/4 支 → <see cref="TorrentialArrow"/>，生成后补 <c>arrow = true</c> 与 <c>extraUpdates += 1</c>。
    /// **2.2.2 给转化出来的箭额外乘了 ×2.3 伤害——按用户口径不取**；
    /// 经典版则是"10 支、逐支掷骰（1/12 洪流箭、1/25 原版鲨鱼、1/100 台风箭）"的另一套写法，也不取。
    /// </para>
    /// </summary>
    internal class Alluvion : ModItem
    {
        /// <summary>一次射出的箭数（照 2.0；经典版是 10）</summary>
        private const int ArrowsPerShot = 6;
        /// <summary>相邻两箭的角间隔（照源 <c>MathHelper.Pi * 0.1f</c>）</summary>
        private const float ArrowSpread = MathHelper.Pi / 10f;
        /// <summary>"排箭"用的位移长度（照源 35——只用于排布与贴墙检测，不影响出手速度）</summary>
        private const float BowSpreadDistance = 35f;
        /// <summary>现代分支配方的材料数量（照 2.0）</summary>
        private const int SeaPrismCount = 20;
        private const int DepthCellsCount = 10;
        private const int LumenylCount = 20;
        private const int TenebrisCount = 5;
        private const int ModernCosmiliteBarCount = 8;
        private const int EndothermicEnergyCount = 20;
        /// <summary>经典分支配方的材料数量（照 经典灾厄）</summary>
        private const int PhantoplasmCount = 5;
        private const int ClassicCosmiliteBarCount = 5;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 165</summary>
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：62×90 / 伤害 165 / 使用 15 帧·动画 30 帧 / 击退 4 / 弹速 12 / 箭类弹药 /
        /// 使用音 `Item5` / 月后 14 档（蓝）/ 1 铂金 80 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 62;                          // 贴图宽（像素）
            Item.height = 90;                         // 贴图高（像素）
            Item.damage = 165;                        // 面板伤害（照 2.0；2.2.2 是 78、经典版 90）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 15;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(1, 80, 0, 0);  // 价值 1 铂金 80 金（照源）
            Item.rare = ItemRarityID.Red;             // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后 14：蓝（＝灾厄 DarkBlue）
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly;  // 真正射出什么由 Shoot 决定
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（6 支全由它派生），会自动跟随。
        /// </summary>
        /// <summary>
        /// 一次射出 <see cref="ArrowsPerShot"/> 支，**任何弹药都按位次分工**
        /// （2 台风箭 + 2 小鲨鱼龙 + 2 洪流箭；弹药全转化，用户 2026-10-15 定）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter);

            velocity.Normalize();
            velocity *= BowSpreadDistance;
            bool canHit = Collision.CanHit(origin, 0, 0, origin + velocity, 0, 0);

            for (int i = 0; i < ArrowsPerShot; i++)
            {
                float offsetAmount = i - (ArrowsPerShot - 1f) / 2f;
                Vector2 offset = velocity.RotatedBy(ArrowSpread * offsetAmount);
                if (!canHit)
                    offset -= velocity;   // 贴墙时把整排箭收回玩家身上（照源）

                int convertedType = i switch
                {
                    0 or 5 => ModContent.ProjectileType<TyphoonArrow>(),
                    1 or 4 => ModContent.ProjectileType<MiniSharkron>(),
                    _ => ModContent.ProjectileType<TorrentialArrow>(),
                };
                int proj = Projectile.NewProjectile(source, origin.X + offset.X, origin.Y + offset.Y, velocity.X, velocity.Y,
                    convertedType, damage, knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(proj))
                {
                    Main.projectile[proj].arrow = true;         // 让这几类也算"箭"（照源）
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
        /// **现代**（照 2.0，@**宇宙砧 `CosmicAnvil`**）= **季风（自持件）** + 海棱晶 `SeaPrism`×20 +
        /// 深渊细胞 `DepthCells`×10 + 流明晶 `Lumenyl`×20 + 暮色碎片×5（2.0 写 `Tenebris`，
        /// 实装 2.2.2 已改名 `PlantyMush`，照 §3 口径取新版名）+ 宇宙锭 `CosmiliteBar`×8 + 吸热能量 `EndothermicEnergy`×20；
        /// **经典**（照 经典灾厄，@**嘉登熔炉 `DraedonsForge`**）= 季风 + 死灵质 `Phantoplasm`×5 +
        /// 宇宙锭×5 + 深渊细胞×10 + 流明晶 `Lumenite`×20 + 暮色碎片 `Tenebris`×5。
        /// 两处"改名对"（流明晶 / 暮色碎片 / 死灵质）见 AGENTS §3。
        /// 2.2.2 把配方砍到 4 味（季风 + 流明晶×20 + 宇宙锭×8 + 吸热能量×20）——**不取**。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("SeaPrism", out ModItem seaPrism) &&
                calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells) &&
                calamity.TryFind<ModItem>("Lumenyl", out ModItem lumenyl) &&
                calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush) &&
                calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar) &&
                calamity.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy) &&
                calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<Monsoon>();
                recipe.AddIngredient(seaPrism.Type, SeaPrismCount);
                recipe.AddIngredient(depthCells.Type, DepthCellsCount);
                recipe.AddIngredient(lumenyl.Type, LumenylCount);
                recipe.AddIngredient(plantyMush.Type, TenebrisCount);
                recipe.AddIngredient(cosmiliteBar.Type, ModernCosmiliteBarCount);
                recipe.AddIngredient(endothermicEnergy.Type, EndothermicEnergyCount);
                recipe.AddTile(cosmicAnvil.Type);
                recipe.Register();
            }
            else
            {
                Mod.Logger.Warn("潮涌：现代版灾厄里缺件（SeaPrism / DepthCells / Lumenyl / PlantyMush / CosmiliteBar / EndothermicEnergy / CosmicAnvil），本条配方未注册。");
            }

            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm) &&
                classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar) &&
                classic.TryFind<ModItem>("DepthCells", out ModItem classicDepthCells) &&
                classic.TryFind<ModItem>("Lumenite", out ModItem lumenite) &&
                classic.TryFind<ModItem>("Tenebris", out ModItem tenebris) &&
                classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                Recipe classicRecipe = CreateRecipe();
                classicRecipe.AddIngredient<Monsoon>();
                classicRecipe.AddIngredient(phantoplasm.Type, PhantoplasmCount);
                classicRecipe.AddIngredient(classicCosmiliteBar.Type, ClassicCosmiliteBarCount);
                classicRecipe.AddIngredient(classicDepthCells.Type, DepthCellsCount);
                classicRecipe.AddIngredient(lumenite.Type, LumenylCount);
                classicRecipe.AddIngredient(tenebris.Type, TenebrisCount);
                classicRecipe.AddTile(draedonsForge.Type);
                classicRecipe.Register();
            }
            else
            {
                Mod.Logger.Warn("潮涌：经典版灾厄里缺件（Phantoplasm / CosmiliteBar / DepthCells / Lumenite / Tenebris / DraedonsForge），本条配方未注册。");
            }
        }
    }
}
