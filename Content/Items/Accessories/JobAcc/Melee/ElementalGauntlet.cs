using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Melee
{
    /// <summary>
    /// 元素手套 - 顶级近战职业饰品
    /// +20% 近战伤害/暴击/攻速，附带自动挥舞、烈火手套与熔岩免疫时间延长，命中时施加多种元素 debuff。
    /// </summary>
    internal class ElementalGauntlet : ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、饰品标记，并指定模组自定义的月后稀有度等级
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 22;                         // 贴图宽 22 像素
            Item.height = 38;                        // 贴图高 38 像素
            Item.value = Item.buyPrice(0, 90, 0, 0); // 价值 90 金
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
            // 月后物品：稀有度颜色交由全局物品统一渲染
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        /// <summary>
        /// 装备时置位 elementalGauntlet 标记，数值结算与命中 debuff 施加在 CalamityDemutationPlayer 中统一完成
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算统一在 CalamityDemutationPlayer 中完成
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.elementalGauntlet = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料名与合成站不同，需分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.FireGauntlet);
                recipe.AddIngredient<YharimsInsignia>();
                recipe.AddIngredient(calamity.Find<ModItem>("Necroplasm").Type, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("NightmareFuel").Type, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("EndothermicEnergy").Type, 20);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.FireGauntlet);
                recipe1.AddIngredient<YharimsInsignia>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 20);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
