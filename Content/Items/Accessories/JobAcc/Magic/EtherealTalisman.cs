using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Magic
{
    /// <summary>
    /// 以太护符 - 顶级魔法职业饰品
    /// 提供 +20% 魔法伤害、+20% 魔法暴击、+150 最大魔力、-20% 魔力消耗，
    /// 附带探宝（findTreasure）、减少药水疾病时间（pStone）与自动喝蓝（manaFlower）效果。
    /// </summary>
    internal class EtherealTalisman:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、饰品标记，并指定模组自定义的月后稀有度等级
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                        // 贴图宽 28 像素
            Item.height = 32;                       // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 90, 0, 0); // 价值 90 金
            Item.accessory = true;                  // 标记为饰品，可装备于饰品栏
            // 使用模组自定义的月后稀有度等级
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        /// <summary>
        /// 装备时置位 etherealTalisman 标记，具体数值在 CalamityDemutationPlayer 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，实际数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.etherealTalisman = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料与合成站不同，需分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 分别适配现代版（宇宙砧）与经典版（德雷顿熔炉）灾厄的合成配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<SigilofCalamitas>();
                recipe.AddIngredient(ItemID.ManaFlower);
                recipe.AddIngredient(calamity.Find<ModItem>("Necroplasm").Type, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("NightmareFuel").Type, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("EndothermicEnergy").Type, 20);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<SigilofCalamitas>();
                recipe1.AddIngredient(ItemID.ManaFlower);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 20);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
