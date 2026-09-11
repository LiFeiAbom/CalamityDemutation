using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.StatLife
{
    /// <summary>
    /// 光辉软泥 - 生命型饰品
    /// 夜晚时额外生命回复并发出光芒。
    /// </summary>
    internal class RadiantOoze:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                         // 贴图宽（像素）
            Item.height = 20;                        // 贴图高（像素）
            Item.value = Item.buyPrice(0, 6, 0, 0);  // 售价 6 金
            Item.rare = ItemRarityID.Orange;         // 稀有度：橙色
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().radiantOoze = true;
        }
        /// <summary>
        /// 配方（分版本）：现代版用污化凝胶，经典版用 MurkySludge 替代，均 + 纯凝胶在砧上合成
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("BlightedGel").Type, 5);
                recipe.AddIngredient(calamity.Find<ModItem>("PurifiedGel").Type, 15);
                recipe.AddTile(TileID.Anvils);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("MurkySludge").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("PurifiedGel").Type, 15);
                recipe1.AddTile(TileID.Anvils);
                recipe1.Register();
            }
        }
    }
}
