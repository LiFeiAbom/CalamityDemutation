using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 海绵 - 综合型饰品
    /// 提升生命与法力上限，并继承吸收者、甘露安瓿等吸收类饰品效果。
    /// </summary>
    internal class Sponge:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、基础防御与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 20;                        // 常驻 +20 防御
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 售价 90 金
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
            // 使用模组自定义的月后稀有度等级
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.beeResist = true;
            modPlayer.sponge = true;
        }
        /// <summary>
        /// 配方（分版本）：吸收者 + 甘露安瓿 + 星辉矿锭 + 星云物质，在星宇锻造台/德拉肯锻造台合成
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<TheAbsorber>();
                recipe.AddIngredient<AmbrosialAmpoule>();
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type, 15);
                recipe.AddIngredient(calamity.Find<ModItem>("Necroplasm").Type, 15);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<TheAbsorber>();
                recipe1.AddIngredient<AmbrosialAmpoule>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 15);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 15);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
