using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Tarragon
{
    /// <summary>
    /// 龙蒿护腿（TarragonLeggings） - 龙蒿套装的腿部部件
    /// 机动与全能向：+20% 移速（生命值在半血及以下时额外 +15%），
    /// 全职业通用伤害 +10%、通用暴击率 +10%。
    /// </summary>
    [AutoloadEquip(EquipType.Legs)]
    internal class TarragonLeggings:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 30, 0, 0);  // 价值 30 金
            Item.defense = 32;                        // 防御力
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;  // 月后自定义稀有度 12 级（名称颜色覆盖见 CalamityDemutationGlobalItem）
        }
        /// <summary>
        /// 穿戴时的属性加成：移速（残血时额外提升）与通用伤害/暴击
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.20f;  // 移速 +20%
            if (player.statLife <= (int)((double)player.statLifeMax2 * 0.5))
            {
                player.moveSpeed += 0.15f;  // 生命值 ≤ 50% 时额外移速 +15%
            }
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
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("UelibloomBar").Type, 18);
                recipe.AddIngredient(calamity.Find<ModItem>("DivineGeode").Type, 12);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄配方（UeliaceBar + DivineGeode）
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("UeliaceBar").Type, 11);
                recipe1.AddIngredient(calamity1.Find<ModItem>("DivineGeode").Type, 12);
                recipe1.AddTile(TileID.LunarCraftingStation);
                recipe1.Register();
            }
        }
    }
}
