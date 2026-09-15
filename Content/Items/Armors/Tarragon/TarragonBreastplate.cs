using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Tarragon
{
    /// <summary>
    /// 龙蒿胸甲（TarragonBreastplate） - 龙蒿套装的胸部部件
    /// 生存与全能向：物品自带 +2 生命回复；穿戴后 +150 生命上限、+100 魔力上限、
    /// +6 生命回复，全职业通用伤害 +10%、通用暴击率 +10%。
    /// </summary>
    [AutoloadEquip(EquipType.Body)]
    internal class TarragonBreastplate:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、自带生命回复、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.lifeRegen = 2;                       // 物品自带生命回复 +2
            Item.value = Item.buyPrice(0, 40, 0, 0);  // 价值 40 金
            Item.defense = 37;                        // 防御力
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;  // 月后自定义稀有度 12 级（名称颜色覆盖见 CalamityDemutationGlobalItem）
        }
        /// <summary>
        /// 穿戴时的属性加成：生命/魔力上限、生命回复与通用伤害/暴击
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 += 150;                       // 生命上限 +150
            player.statManaMax2 += 100;                       // 魔力上限 +100
            player.lifeRegen += 6;                            // 生命回复 +6
            player.GetDamage<GenericDamageClass>() += 0.1f;   // 通用伤害 +10%（全职业增伤）
            player.GetCritChance<GenericDamageClass>() += 10; // 通用暴击率 +10%
        }
        /// <summary>
        /// 配方：现代版与经典版灾厄材料不同，分别注册两套配方
        /// （均使用原版合成站 TileID.LunarCraftingStation）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄配方（UelibloomBar + DivineGeode）
                if (calamity.TryFind<ModItem>("UelibloomBar", out ModItem uelibloomBar)
                    && calamity.TryFind<ModItem>("DivineGeode", out ModItem divineGeode))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(uelibloomBar.Type, 24);
                    recipe.AddIngredient(divineGeode.Type, 18);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄配方（UeliaceBar + DivineGeode）
                if (calamity1.TryFind<ModItem>("UeliaceBar", out ModItem ueliaceBar)
                    && calamity1.TryFind<ModItem>("DivineGeode", out ModItem classicDivineGeode))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ueliaceBar.Type, 15);
                    recipe1.AddIngredient(classicDivineGeode.Type, 18);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
