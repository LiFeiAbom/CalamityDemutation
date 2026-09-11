using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Function
{
    /// <summary>
    /// 远古化石 - 功能型饰品
    /// 在地下/洞穴层时大幅提升挖掘速度。
    /// </summary>
    internal class AncientFossil:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                         // 贴图宽（像素）
            Item.height = 26;                        // 贴图高（像素）
            Item.value = Item.buyPrice(0, 3, 0, 0);  // 售价 3 金
            Item.rare = ItemRarityID.Blue;           // 稀有度：蓝色
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().ancientFossil = true;
        }
        /// <summary>
        /// 配方：100 个泥沙类方块（Silt 组）在熔炉熔炼（需任一版灾厄加载）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddRecipeGroup("AnySiltBlock", 100);
                recipe.AddTile(TileID.Furnaces);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddRecipeGroup("SiltGroup", 100);
                recipe.AddTile(TileID.Furnaces);
                recipe.Register();
            }
        }
    }
}
