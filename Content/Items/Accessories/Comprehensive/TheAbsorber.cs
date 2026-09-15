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
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells1) && calamity.TryFind<ModItem>("Lumenyl", out ModItem lumenyl1) && calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<GrandGelatin>();
                    recipe.AddIngredient<SeaShell>();
                    recipe.AddIngredient<CrawCarapace>();
                    recipe.AddIngredient<FungalCarapace>();
                    recipe.AddIngredient<GiantTortoiseShell>();
                    recipe.AddIngredient<AmidiasSpark>();
                    recipe.AddIngredient(depthCells1.Type, 15);
                    recipe.AddIngredient(lumenyl1.Type, 15);
                    recipe.AddIngredient(plantyMush1.Type, 5);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
                if(calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells2) && calamity.TryFind<ModItem>("Lumenyl", out ModItem lumenyl2) && calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<GrandGelatin>();
                    recipe.AddIngredient<SeaShell>();
                    recipe.AddIngredient<GiantShell>();
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
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("DepthCells", out ModItem depthCells3) && calamity1.TryFind<ModItem>("Lumenite", out ModItem lumenite1) && calamity1.TryFind<ModItem>("Tenebris", out ModItem tenebris1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<GrandGelatin>();
                    recipe1.AddIngredient<SeaShell>();
                    recipe1.AddIngredient<CrawCarapace>();
                    recipe1.AddIngredient<FungalCarapace>();
                    recipe1.AddIngredient<GiantTortoiseShell>();
                    recipe1.AddIngredient<AmidiasSpark>();
                    recipe1.AddIngredient(depthCells3.Type, 15);
                    recipe1.AddIngredient(lumenite1.Type, 15);
                    recipe1.AddIngredient(tenebris1.Type, 5);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
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
