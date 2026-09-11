using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Melee
{
    /// <summary>
    /// 勇士徽章 - 近战职业饰品
    /// +10% 近战伤害、+10% 近战暴击、+5 近战穿透、+5% 近战攻速。
    /// </summary>
    internal class BadgeofBravery : ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、售价、饰品标记，并指定模组自定义的月后稀有度等级
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 30;                         // 贴图宽 30 像素
            Item.height = 30;                        // 贴图高 30 像素
            Item.value = Item.buyPrice(0, 21, 0, 0); // 售价 21 金
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
            // 月后物品：稀有度颜色交由全局物品统一渲染
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;
        }
        /// <summary>
        /// 装备时置位 badgeOfBravery 标记，具体数值在 CalamityDemutationPlayer 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算统一在 CalamityDemutationPlayer 中完成
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.badgeOfBravery = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料名不同，需分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.FeralClaws);
                recipe.AddIngredient(ItemID.WarriorEmblem);
                recipe.AddIngredient(calamity.Find<ModItem>("UelibloomBar").Type, 4);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.FeralClaws);
                recipe1.AddIngredient(ItemID.WarriorEmblem);
                recipe1.AddIngredient(calamity1.Find<ModItem>("UeliaceBar").Type, 4);
                recipe1.AddTile(TileID.LunarCraftingStation);
                recipe1.Register();
            }
        }
    }
}
