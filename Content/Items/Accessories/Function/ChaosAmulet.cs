using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Function
{
    /// <summary>
    /// 混沌护符 - 功能型饰品
    /// 提供 +2 生命回复，并永久点亮周围宝藏（findTreasure，即探宝 Spelunker 效果）。
    /// </summary>
    internal class ChaosAmulet:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、生命回复、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 24;                         // 贴图高（像素）
            Item.lifeRegen = 2;                       // 装备时 +2 生命回复
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 价值 15 金
            Item.rare = ItemRarityID.Yellow;          // 稀有度：黄色
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时直接置位原版 Player.findTreasure，开启探宝（Spelunker）效果，
        /// 无需经过 CalamityDemutationPlayer 结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 直接开启原版探宝效果，无需经过 ModPlayer 结算
            player.findTreasure = true;
        }
        /// <summary>
        /// 配方（分版本）：现代版用狱石锭 ×2 + 灾厄灰烬 AshesofCalamity ×2 + 探宝药水 ×7，
        /// 经典版改用 CruptixBar ×2 + 探宝药水 ×7，均在秘银砧合成
        /// </summary>
        public override void AddRecipes()
        {
            // 分别适配现代版与经典版灾厄的合成配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.HellstoneBar, 2);
                recipe.AddIngredient(calamity.Find<ModItem>("AshesofCalamity").Type, 2);
                recipe.AddIngredient(ItemID.SpelunkerPotion, 7);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CruptixBar").Type, 2);
                recipe1.AddIngredient(ItemID.SpelunkerPotion, 7);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
