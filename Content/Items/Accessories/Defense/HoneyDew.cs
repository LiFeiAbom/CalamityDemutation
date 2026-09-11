using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 蜜露 - 防御型饰品
    /// 身处丛林时强化属性，免疫毒液等 debuff，提供蜂蜜效果与生命回复并减半蜜蜂类弹幕伤害。
    /// </summary>
    internal class HoneyDew:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 售价 15 金
            Item.rare = ItemRarityID.Lime;            // 稀有度：黄绿色
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.beeResist = true;
            modPlayer.honeyDew = true;
        }
        /// <summary>
        /// 配方：生命露 + 10 蜂蜜瓶 + 10 蜂蜡 + 牛黄，秘银砧合成（两版灾厄材料相同，共用一条配方）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<LivingDew>();
                recipe.AddIngredient(ItemID.BottledHoney, 10);
                recipe.AddIngredient(ItemID.BeeWax, 10);
                recipe.AddIngredient(ItemID.Bezoar);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }
}
