using CalamityDemutation.Content.Items.Accessories.Function;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Silva
{
    /// <summary>
    /// 林妖护腿（SilvaLeggings） - 林妖套装的腿部部件
    /// 机动与全能向：+45% 移速、全职业通用伤害 +12%、通用暴击率 +12%。
    /// </summary>
    [AutoloadEquip(EquipType.Legs)]
    internal class SilvaLeggings:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 54, 0, 0);  // 价值 54 金
            Item.defense = 39;                        // 防御力
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;  // 月后自定义稀有度 15 级（名称颜色覆盖见 CalamityDemutationGlobalItem）
        }
        /// <summary>
        /// 穿戴时的属性加成：大幅移速 + 通用伤害/暴击
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.45f;                        // 移速 +45%
            player.GetDamage<GenericDamageClass>() += 0.12f;  // 通用伤害 +12%（全职业增伤）
            player.GetCritChance<GenericDamageClass>() += 12; // 通用暴击率 +12%
        }
        /// <summary>
        /// 配方：现代版与经典版灾厄材料不同，分别注册两套配方，均需本模组材料 LeadCore
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄配方（合成站 CosmicAnvil）
                if (calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush)
                    && calamity.TryFind<ModItem>("EffulgentFeather", out ModItem effulgentFeather)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(plantyMush.Type, 60);
                    recipe.AddIngredient(effulgentFeather.Type, 10);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 3);
                    recipe.AddIngredient<LeadCore>();
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄配方（合成站 DraedonsForge）
                if (calamity1.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && calamity1.TryFind<ModItem>("EffulgentFeather", out ModItem classicEffulgentFeather)
                    && calamity1.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && calamity1.TryFind<ModItem>("Tenebris", out ModItem tenebris)
                    && calamity1.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && calamity1.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(darksunFragment.Type, 7);
                    recipe1.AddIngredient(classicEffulgentFeather.Type, 7);
                    recipe1.AddIngredient(cosmiliteBar.Type, 7);
                    recipe1.AddIngredient(tenebris.Type, 9);
                    recipe1.AddIngredient(nightmareFuel.Type, 15);
                    recipe1.AddIngredient(endothermicEnergy.Type, 15);
                    recipe1.AddIngredient<LeadCore>();
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
