using CalamityDemutation.Content.Items.Accessories.Function;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Silva
{
    /// <summary>
    /// 林妖胸甲（SilvaArmor） - 林妖套装的胸部部件
    /// 生存与全能向：+300 生命上限、+200 魔力上限、+20% 移速，
    /// 全职业通用伤害 +18%、通用暴击率 +18%。
    /// </summary>
    [AutoloadEquip(EquipType.Body)]
    internal class SilvaArmor : ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 72, 0, 0);  // 价值 72 金
            Item.defense = 44;                        // 防御力
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;  // 月后自定义稀有度 15 级（名称颜色覆盖见 CalamityDemutationGlobalItem）
        }
        /// <summary>
        /// 穿戴时的属性加成：生命/魔力上限、移速与通用伤害/暴击
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 += 300;                       // 生命上限 +300
            player.statManaMax2 += 200;                       // 魔力上限 +200
            player.moveSpeed += 0.2f;                         // 移速 +20%
            player.GetDamage<GenericDamageClass>() += 0.18f;  // 通用伤害 +18%（全职业增伤）
            player.GetCritChance<GenericDamageClass>() += 18; // 通用暴击率 +18%
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
                recipe.AddIngredient(calamity.Find<ModItem>("PlantyMush").Type, 90);
                recipe.AddIngredient(calamity.Find<ModItem>("EffulgentFeather").Type, 12);
                recipe.AddIngredient(calamity.Find<ModItem>("AscendantSpiritEssence").Type, 4);
                recipe.AddIngredient<LeadCore>();
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄配方（合成站 DraedonsForge）
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("DarksunFragment").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EffulgentFeather").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Tenebris").Type, 12);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 16);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 16);
                recipe1.AddIngredient<LeadCore>();
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
