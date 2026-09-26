using CalamityDemutation.Content.Items.Accessories.Attack;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Melee
{
    /// <summary>
    /// 亚利姆徽章 - 近战职业饰品
    /// +14% 近战伤害/暴击/攻速、烈火手套击退与熔岩免疫时间延长，半血以下额外 +10% 通用伤害；
    /// 近战命中时施加圣焰（现代版）/圣光（经典版）debuff。
    /// </summary>
    internal class YharimsInsignia : ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、饰品标记，并指定模组自定义的月后稀有度等级
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 22;                         // 贴图宽 22 像素
            Item.height = 38;                        // 贴图高 38 像素
            Item.value = Item.buyPrice(0, 30, 0, 0); // 价值 30 金
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
            // 月后物品：稀有度颜色交由全局物品统一渲染
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;
        }
        /// <summary>
        /// 装备时置位 yharimsInsignia 标记，数值结算与命中 debuff 施加在 CalamityDemutationPlayer 中统一完成
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算与 debuff 施加统一在 CalamityDemutationPlayer 中完成
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.yharimsInsignia = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料名/数量不同，需分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名/数量不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.WarriorEmblem);
                recipe.AddIngredient<NecklaceofVexation>();
                recipe.AddIngredient<CoreofCinder>();
                recipe.AddIngredient(ItemID.CrossNecklace);
                recipe.AddIngredient<BadgeofBravery>();
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("CoreofCinder", out ModItem coreofCinder1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.WarriorEmblem);
                    recipe1.AddIngredient<NecklaceofVexation>();
                    recipe1.AddIngredient(coreofCinder1.Type, 5);
                    recipe1.AddIngredient(ItemID.CrossNecklace);
                    recipe1.AddIngredient<BadgeofBravery>();
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
