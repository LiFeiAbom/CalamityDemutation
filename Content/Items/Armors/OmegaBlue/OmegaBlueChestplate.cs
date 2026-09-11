using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.OmegaBlue
{
    /// <summary>
    /// 奥米加蓝胸甲（OmegaBlueChestplate） - 奥米加蓝套胸部
    /// 单件：+18% 全伤害、+16% 暴击、+18% 近战攻速。
    /// 同时置位 omegaBlueChestplate，在 CalamityDemutationPlayer 中结算：
    /// 远程武器 25% 概率不消耗弹药，且近战/弹幕命中时对敌施加灾厄 debuff
    /// （现代版 HadopelagicPressure，经典版 CrushDepth）。
    /// </summary>
    [AutoloadEquip(EquipType.Body)]
    internal class OmegaBlueChestplate : ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                             // 贴图宽（像素）
            Item.height = 18;                            // 贴图高（像素）
            Item.value = Item.sellPrice(0, 38, 0, 0);    // 售价 38 金
            Item.rare = ItemRarityID.Red;                // 基础稀有度红色
            Item.defense = 28;                           // 防御 28
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后稀有度 13（荧光绿名）
        }
        /// <summary>
        /// 单件属性与标记置位；omegaBlueChestplate 由 CalamityDemutationPlayer 消费（节省弹药 + 命中上 debuff）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<GenericDamageClass>() += 0.18f;      // 全伤害 +18%
            player.GetCritChance<GenericDamageClass>() += 16;      // 全暴击 +16%
            player.GetAttackSpeed<MeleeDamageClass>() += 0.18f;    // 近战攻速 +18%
            player.GetModPlayer<CalamityDemutationPlayer>().omegaBlueChestplate = true;
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，分别注册，均在原版月球工作台合成。
        /// 现代版用 ReaperTooth(5) + DepthCells(25) + RuinousSoul(3)；
        /// 经典版用 ReaperTooth(16) + Lumenite(8) + Tenebris(8) + RuinousSoul(4)。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("ReaperTooth").Type, 5);
                recipe.AddIngredient(calamity.Find<ModItem>("DepthCells").Type, 25);
                recipe.AddIngredient(calamity.Find<ModItem>("RuinousSoul").Type, 3);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("ReaperTooth").Type, 16);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Lumenite").Type, 8);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Tenebris").Type, 8);
                recipe1.AddIngredient(calamity1.Find<ModItem>("RuinousSoul").Type, 4);
                recipe1.AddTile(TileID.LunarCraftingStation);
                recipe1.Register();
            }
        }
    }
}
