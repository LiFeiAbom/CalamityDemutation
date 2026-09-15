using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Attack
{
    /// <summary>
    /// 烦恼项链 - 通用攻击饰品
    /// 提供 +5% 通用伤害，半血以下额外 +15%（合计 +20%）。
    /// </summary>
    internal class NecklaceofVexation : ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                          // 贴图宽（像素）
            Item.height = 28;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 价值 15 金
            Item.rare = ItemRarityID.LightPurple;     // 稀有度：亮紫色
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 necklaceOfVexation 标记；通用伤害加成（半血以下额外 +15%）
        /// 在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，实际数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.necklaceOfVexation = true;
        }
        /// <summary>
        /// 配方（分版本）：现代版用灾厄材料 PerennialBar ×2 + 复仇者徽章，
        /// 经典版改用 DraedonBar ×2 + 复仇者徽章，均在秘银砧合成
        /// </summary>
        public override void AddRecipes()
        {
            // 分别适配现代版与经典版灾厄的合成配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("PerennialBar", out ModItem perennialBar1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(perennialBar1.Type, 2);
                    recipe.AddIngredient(ItemID.AvengerEmblem);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("DraedonBar", out ModItem draedonBar1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(draedonBar1.Type, 2);
                    recipe1.AddIngredient(ItemID.AvengerEmblem);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
