using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.OmegaBlue
{
    /// <summary>
    /// 奥米加蓝护腿（OmegaBlueLeggings） - 奥米加蓝套腿部
    /// 单件：+16% 全伤害、+12% 暴击、+30% 移速。
    /// </summary>
    [AutoloadEquip(EquipType.Legs)]
    internal class OmegaBlueLeggings:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                             // 贴图宽（像素）
            Item.height = 18;                            // 贴图高（像素）
            Item.value = Item.sellPrice(0, 35, 25, 0);   // 售价 35 金 25 银
            Item.rare = ItemRarityID.Red;                // 基础稀有度红色
            Item.defense = 22;                           // 防御 22
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后稀有度 13（荧光绿名）
        }
        /// <summary>
        /// 单件属性：全伤害 / 暴击 / 移速
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<GenericDamageClass>() += 0.16f;  // 全伤害 +16%
            player.GetCritChance<GenericDamageClass>() += 12;  // 全暴击 +12%
            player.moveSpeed += 0.3f;                          // 移速 +30%
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，分别注册，均在原版月球工作台合成。
        /// 现代版用 ReaperTooth(4) + DepthCells(20) + RuinousSoul(2)；
        /// 经典版用 ReaperTooth(13) + Lumenite(6) + Tenebris(6) + RuinousSoul(3)。
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ReaperTooth", out ModItem reaperTooth)
                    && calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells)
                    && calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(reaperTooth.Type, 4);
                    recipe.AddIngredient(depthCells.Type, 20);
                    recipe.AddIngredient(ruinousSoul.Type, 2);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("ReaperTooth", out ModItem classicReaperTooth)
                    && calamity1.TryFind<ModItem>("Lumenite", out ModItem lumenite)
                    && calamity1.TryFind<ModItem>("Tenebris", out ModItem tenebris)
                    && calamity1.TryFind<ModItem>("RuinousSoul", out ModItem classicRuinousSoul))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(classicReaperTooth.Type, 13);
                    recipe1.AddIngredient(lumenite.Type, 6);
                    recipe1.AddIngredient(tenebris.Type, 6);
                    recipe1.AddIngredient(classicRuinousSoul.Type, 3);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
