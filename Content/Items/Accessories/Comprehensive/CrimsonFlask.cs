using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 猩红药剂 - 防御型饰品
    /// 身处猩红之地时额外获得 +3 防御与 +7% 减伤。
    /// </summary>
    internal class CrimsonFlask:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 3, 0, 0);   // 价值 3 金
            Item.rare = ItemRarityID.Green;           // 稀有度：绿（Green）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().crimsonFlask = true;
        }
        /// <summary>
        /// 配方（分版本）：现代版用猩红粉，经典版用放血精粹替代，均 + 椎骨在铁/铅砧合成
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.ViciousPowder, 3);
                recipe.AddIngredient(ItemID.Vertebrae, 10);
                recipe.AddTile(TileID.Anvils);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("BloodlettingEssence", out ModItem bloodlettingEssence))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(bloodlettingEssence.Type, 3);
                    recipe1.AddIngredient(ItemID.Vertebrae, 10);
                    recipe1.AddTile(TileID.Anvils);
                    recipe1.Register();
                }
            }
        }
    }
}
