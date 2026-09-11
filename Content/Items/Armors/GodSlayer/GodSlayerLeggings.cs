using CalamityDemutation.Content.Items.Accessories.Function;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.GodSlayer
{
    /// <summary>
    /// 神裁者护腿（GodSlayerLeggings） - 弑神者套腿部
    /// 单件：+35% 移速、+11% 全伤害、+11% 暴击。
    /// </summary>
    [AutoloadEquip(EquipType.Legs)]
    internal class GodSlayerLeggings:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 45, 0, 0);  // 售价 45 金
            Item.defense = 35;                        // 防御 35
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后稀有度 14（蓝色名）
        }
        /// <summary>
        /// 单件属性：移速 / 全伤害 / 暴击
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.35f;                       // 移速 +35%
            player.GetDamage<GenericDamageClass>() += 0.11f;  // 全伤害 +11%
            player.GetCritChance<GenericDamageClass>() += 11;  // 全暴击 +11%
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，分别注册。
        /// 现代版用 CosmiliteBar(15) + AscendantSpiritEssence(3)，于 CosmicAnvil（宇宙砧）合成；
        /// 经典版用 CosmiliteBar(18) + NightmareFuel(9) + EndothermicEnergy(9)，于 DraedonsForge（德雷顿熔炉）合成。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type, 15);
                recipe.AddIngredient(calamity.Find<ModItem>("AscendantSpiritEssence").Type, 3);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 18);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 9);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 9);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
