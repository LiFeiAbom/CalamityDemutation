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
        /// 物品基础属性：尺寸、售价、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 54, 0, 0);  // 售价 54 金
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
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("PlantyMush").Type, 60);
                recipe.AddIngredient(calamity.Find<ModItem>("EffulgentFeather").Type, 10);
                recipe.AddIngredient(calamity.Find<ModItem>("AscendantSpiritEssence").Type, 3);
                recipe.AddIngredient<LeadCore>();
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄配方（合成站 DraedonsForge）
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("DarksunFragment").Type, 7);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EffulgentFeather").Type, 7);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 7);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Tenebris").Type, 9);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 15);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 15);
                recipe1.AddIngredient<LeadCore>();
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
