using CalamityDemutation.Content.Items.Accessories.JobAcc.Magic;
using CalamityDemutation.Content.Items.Accessories.Movement;
using CalamityDemutation.Content.Items.Accessories.StatLife;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 大凝胶 - 综合型饰品
    /// 结合生命/法力/活力果冻效果，提供移速、跳跃、生命与法力上限加成，静止时额外回复。
    /// </summary>
    internal class GrandGelatin:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 24;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 售价 15 金
            Item.rare = ItemRarityID.LightPurple;     // 稀有度：亮紫（LightPurple）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().grandGelatin = true;
        }
        /// <summary>
        /// 配方：三种果冻（魔力/生命/活力）在秘银砧合成（两版灾厄共用，均为本模组物品）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<ManaJelly>();
                recipe.AddIngredient<LifeJelly>();
                recipe.AddIngredient<VitalJelly>();
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }
}
