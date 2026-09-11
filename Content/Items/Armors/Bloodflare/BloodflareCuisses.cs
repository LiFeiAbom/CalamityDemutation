using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Bloodflare
{
    /// <summary>
    /// 炎血护腿（BloodflareCuisses） - 炎血套（Bloodflare）腿部防具
    /// 提供移动速度与通用伤害、暴击加成，是炎血套的第三件。
    /// </summary>
    [AutoloadEquip(EquipType.Legs)]
    internal class BloodflareCuisses:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 36, 0, 0);  // 售价 36 金
            Item.defense = 29;        // 防御 29
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后稀有度 13 级，名称颜色为荧光绿
        }
        /// <summary>
        /// 单件装备加成：移动速度、通用伤害与暴击
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.3f;                           // 移动速度 +30%
            player.GetDamage<GenericDamageClass>() += 0.1f;     // 全类型伤害 +10%
            player.GetCritChance<GenericDamageClass>() += 10;   // 全类型暴击率 +10%
        }
        /// <summary>
        /// 注册配方：现代版灾厄（CalamityMod）与经典预发布版灾厄（CalamityModClassicPreTrailer）
        /// 材料不同，故分别注册两套配方，均在远古操纵机（LunarCraftingStation）处合成。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄材料：Bloodstone×35、BloodOrb×15、RuinousSoul×3
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("Bloodstone").Type, 35);
                recipe.AddIngredient(calamity.Find<ModItem>("BloodOrb").Type, 15);
                recipe.AddIngredient(calamity.Find<ModItem>("RuinousSoul").Type, 3);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄材料：BloodstoneCore×13、RuinousSoul×3
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("BloodstoneCore").Type, 13);
                recipe1.AddIngredient(calamity1.Find<ModItem>("RuinousSoul").Type, 3);
                recipe1.AddTile(TileID.LunarCraftingStation);
                recipe1.Register();
            }
        }
    }
}
