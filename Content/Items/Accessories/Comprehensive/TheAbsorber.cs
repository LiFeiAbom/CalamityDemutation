using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Content.Items.Accessories.Movement;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 吸收者 - 综合型饰品
    /// 提供生命/法力上限、移速、跳跃、荆棘反伤与减伤，受击时按伤害回复生命。
    /// </summary>
    internal class TheAbsorber:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度、基础防御与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 6;                         // 常驻 +6 防御
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 24;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金
            Item.rare = ItemRarityID.Red;             // 稀有度：红（Red）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.theAbsorber = true;
        }
        /// <summary>
        /// 配方（分版本）：大凝胶 + 海贝 + 一系列海洋防御饰品（爬行甲壳或巨型贝壳二选一作替代路线）
        /// + 深渊材料，在月亮工作台合成
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄 ──
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 路线一：爬行甲壳版（深渊材料取自现代版：深海细胞/流明石/菌菇）
                if(calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells1) && calamity.TryFind<ModItem>("Lumenyl", out ModItem lumenyl1) && calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<GrandGelatin>();      // 大凝胶（本模组移植物）
                    recipe.AddIngredient<SeaShell>();          // 海贝
                    recipe.AddIngredient<CrawCarapace>();      // 爬行甲壳
                    recipe.AddIngredient<FungalCarapace>();    // 真菌甲壳
                    recipe.AddIngredient<GiantTortoiseShell>();// 巨型陆龟壳
                    recipe.AddIngredient<AmidiasSpark>();      // 阿米迪亚斯火花
                    recipe.AddIngredient(depthCells1.Type, 15); // 深渊材料：深海细胞 ×15
                    recipe.AddIngredient(lumenyl1.Type, 15);    // 深渊材料：流明石 ×15
                    recipe.AddIngredient(plantyMush1.Type, 5);  // 深渊材料：菌菇 ×5
                    recipe.AddTile(TileID.LunarCraftingStation);// 月亮工作台
                    recipe.Register();
                }
                // 路线二：巨型贝壳版（以 GiantShell 替换 CrawCarapace，其余相同）
                if(calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells2) && calamity.TryFind<ModItem>("Lumenyl", out ModItem lumenyl2) && calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<GrandGelatin>();
                    recipe.AddIngredient<SeaShell>();
                    recipe.AddIngredient<GiantShell>();        // 巨型贝壳（替代爬行甲壳的路线分支）
                    recipe.AddIngredient<FungalCarapace>();
                    recipe.AddIngredient<GiantTortoiseShell>();
                    recipe.AddIngredient<AmidiasSpark>();
                    recipe.AddIngredient(depthCells2.Type, 15);
                    recipe.AddIngredient(lumenyl2.Type, 15);
                    recipe.AddIngredient(plantyMush2.Type, 5);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            // ── 经典版灾厄：深渊材料换名（Lumenyl→Lumenite、PlantyMush→Tenebris） ──
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 路线一：爬行甲壳版
                if(calamity1.TryFind<ModItem>("DepthCells", out ModItem depthCells3) && calamity1.TryFind<ModItem>("Lumenite", out ModItem lumenite1) && calamity1.TryFind<ModItem>("Tenebris", out ModItem tenebris1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<GrandGelatin>();
                    recipe1.AddIngredient<SeaShell>();
                    recipe1.AddIngredient<CrawCarapace>();
                    recipe1.AddIngredient<FungalCarapace>();
                    recipe1.AddIngredient<GiantTortoiseShell>();
                    recipe1.AddIngredient<AmidiasSpark>();
                    recipe1.AddIngredient(depthCells3.Type, 15); // 深渊材料：深海细胞 ×15
                    recipe1.AddIngredient(lumenite1.Type, 15);  // 经典版对应材料 ×15
                    recipe1.AddIngredient(tenebris1.Type, 5);   // 经典版对应材料 ×5
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
                // 路线二：巨型贝壳版
                if(calamity1.TryFind<ModItem>("DepthCells", out ModItem depthCells4) && calamity1.TryFind<ModItem>("Lumenite", out ModItem lumenite2) && calamity1.TryFind<ModItem>("Tenebris", out ModItem tenebris2))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<GrandGelatin>();
                    recipe1.AddIngredient<SeaShell>();
                    recipe1.AddIngredient<GiantShell>();
                    recipe1.AddIngredient<FungalCarapace>();
                    recipe1.AddIngredient<GiantTortoiseShell>();
                    recipe1.AddIngredient<AmidiasSpark>();
                    recipe1.AddIngredient(depthCells4.Type, 15);
                    recipe1.AddIngredient(lumenite2.Type, 15);
                    recipe1.AddIngredient(tenebris2.Type, 5);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
