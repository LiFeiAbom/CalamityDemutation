using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// P90 - 「速射机枪」的配方前件（用户 2026-10-14 指定：**移植 CI 的 `P90Legacy`**，
    /// 类名去掉 CI 式 `Legacy` 后缀，照工程命名口径）。
    /// <para>
    /// 数值（CI）：伤害 6、60×28、使用/动画 **2 帧**、**穿甲 20**、击退 1.5、弹速 18、子弹弹药、
    /// **33% 不消耗弹药**、**每次开火打两颗子弹**（各自 ±15×0.05 抖动）、稀有度 Lime（浅绿）、
    /// 价值 48 金（CI 的 `RarityPriceLime` = `RarityPrice7`）、音 `Item11`、持握偏移 (-14, -1)。
    /// </para>
    /// <para>
    /// 与源的差别：① **不取源的"天顶世界"分支**（`Main.zenithWorld` 下伤害 12 / 使用 1 帧 /
    /// 一次喷 13 颗随机方向弹）——工程既有件（如巨龙之怒）同样把这套 `zenithWorld` 分支砍掉了；
    /// ② `canFirePointBlankShots`（灾厄贴脸射击）按用户 2026-10-14 口径**不做**；
    /// ③ 配方合成一条：原版配方组「铁锭/铅锭」×10 + 冰核×7 @ 秘银砧
    /// （CI 写的是铁、铅两条独立配方，等价于原版 `RecipeGroupID.IronBar`）。
    /// </para>
    /// <para>
    /// **实装 2.2.2 里的 P90 是重做版**（48×24 新图 + 自定义弹幕 `P90Round`、隔发换弹、军火商出售），
    /// 与本件**同名不同物**（工程既定的"自持旧版"口径）。
    /// 配方（双分支，材料按各自版本取）：现代 = 铁锭/铅锭×10 + `EssenceofEleum`×7 @ 秘银砧
    /// （源的 `CoreofEleum` 在现代版已改名为 `EssenceofEleum`——照工程"改名 = 同一件"的先例，
    /// 如同日光精华那对 `EssenceofCinder`/`EssenceofSunlight`）；经典 = 铁锭/铅锭×10 +
    /// `CoreofEleum`×7 @ 秘银砧（照 cal-1.4.2.101）。
    /// </para>
    /// </summary>
    internal class P90:ModItem
    {
        /// <summary>不打散弹的固定省弹率（照 CI：33% 不消耗弹药）</summary>
        private const int AmmoSavePercent = 33;
        /// <summary>每发弹丸的速度抖动幅度（照源：±15 × 0.05 = ±0.75）</summary>
        private const float SpreadJitter = 0.05f;

        /// <summary>
        /// 研究解锁一份
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：60×28、伤害 6、使用/动画 2 帧、穿甲 20、击退 1.5、弹速 18、
        /// 子弹弹药、浅绿稀有度、价值 48 金（全部照 CI）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 60;                          // 贴图宽（像素）
            Item.height = 28;                         // 贴图高（像素）
            Item.damage = 6;                          // 面板伤害（照 CI；2.0 是 8）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;                         // 使用/动画 2 帧（照 CI；2.0 是 5 帧）
            Item.useAnimation = 2;
            Item.ArmorPenetration = 20;               // 穿甲 20（照 CI）
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 挥砍动画不造成伤害
            Item.knockBack = 1.5f;
            Item.value = Item.buyPrice(0, 48, 0, 0);  // 价值 48 金（CI 的 RarityPriceLime）
            Item.rare = ItemRarityID.Lime;            // 稀有度：浅绿（照 CI）
            Item.UseSound = SoundID.Item11;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.PurificationPowder;  // 射的是弹药本体（照源的占位写法）
            Item.shootSpeed = 18f;
            Item.useAmmo = AmmoID.Bullet;             // 吃子弹类弹药
        }
        /// <summary>
        /// 持握偏移：照 CI 的 (-14, -1)
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-14, -1);
        /// <summary>
        /// 33% 不消耗弹药（照 CI：<c>rand(0,100) &lt; 33</c> 时不消耗，这里取等价写法）
        /// </summary>
        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.Next(100) >= AmmoSavePercent;
        /// <summary>
        /// 开火：**一次打两颗**（同一组抖动速度），每颗再各自加 ±0.75 抖动（照 CI）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float SpeedX = velocity.X + Main.rand.Next(-15, 16) * SpreadJitter;
            float SpeedY = velocity.Y + Main.rand.Next(-15, 16) * SpreadJitter;
            Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, type, damage, knockback, player.whoAmI, 0f, 0f);
            Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, type, damage, knockback, player.whoAmI, 0f, 0f);
            return false;
        }
        /// <summary>
        /// 配方（分版本，缺件即退化成单分支）：两版都是 原版配方组「铁锭/铅锭」×10 + 冰核×7 @ 秘银砧，
        /// 只是冰核的类名按各自版本取（现代 `EssenceofEleum` / 经典 `CoreofEleum`）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("EssenceofEleum", out ModItem essenceofEleum))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddRecipeGroup(RecipeGroupID.IronBar, 10);   // 铁锭 / 铅锭（原版组）
                    recipe.AddIngredient(essenceofEleum.Type, 7);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if(classic.TryFind<ModItem>("CoreofEleum", out ModItem coreofEleum))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddRecipeGroup(RecipeGroupID.IronBar, 10);
                    recipeClassic.AddIngredient(coreofEleum.Type, 7);
                    recipeClassic.AddTile(TileID.MythrilAnvil);
                    recipeClassic.Register();
                }
            }
        }
    }
}
