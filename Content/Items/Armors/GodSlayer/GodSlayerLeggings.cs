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
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 45, 0, 0);  // 价值 45 金
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
                if (calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(cosmiliteBar.Type, 15);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 3);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                    && calamity1.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && calamity1.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(classicCosmiliteBar.Type, 18);
                    recipe1.AddIngredient(nightmareFuel.Type, 9);
                    recipe1.AddIngredient(endothermicEnergy.Type, 9);
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
