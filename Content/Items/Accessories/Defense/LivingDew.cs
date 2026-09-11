using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 生命露 - 防御型饰品
    /// 身处丛林时额外获得生命回复、防御与减伤。
    /// </summary>
    internal class LivingDew:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 12, 0, 0);  // 售价 12 金
            Item.rare = ItemRarityID.Pink;            // 稀有度：粉色
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().livingDew = true;
        }
        /// <summary>
        /// 配方（分版本）：现代版用蜂蜜瓶+蜂蜡+丛林孢子；经典版改用灾厄丛林材料（捕食者球茎等）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.BottledHoney, 10);
                recipe.AddIngredient(ItemID.BeeWax, 3);
                recipe.AddIngredient(ItemID.JungleSpores, 6);
                recipe.AddTile(TileID.Anvils);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("ManeaterBulb").Type, 2);
                recipe1.AddIngredient(calamity1.Find<ModItem>("TrapperBulb").Type, 2);
                recipe1.AddIngredient(calamity1.Find<ModItem>("MurkyPaste").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("GypsyPowder").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("BeetleJuice").Type, 3);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
