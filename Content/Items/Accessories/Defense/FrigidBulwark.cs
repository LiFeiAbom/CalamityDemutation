using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 寒霜壁垒（Frigid Bulwark） - 防御型饰品
    /// +8 防御、免疫击退；生命 >25% 时提供圣骑士护盾（可为附近队友加盾），
    /// 生命 ≤50% 获得冰屏障 buff，≤15% 时额外 +5% 减伤。
    /// </summary>
    internal class FrigidBulwark:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度、防御与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 38;                          // 贴图宽（像素）
            Item.height = 44;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 30, 0, 0);  // 售价 30 金
            Item.rare = ItemRarityID.Cyan;            // 稀有度：青色
            Item.defense = 8;                         // 装备时 +8 防御
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 frigidBulwark 标记；真正的数值（击退免疫、圣骑士护盾、
        /// 冰屏障 buff 与低血减伤）在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.frigidBulwark = true;
        }
        /// <summary>
        /// 配方：圣骑士盾 + 冰冻海龟壳 + CoreofEleum，现代版（CalamityMod）与经典版
        /// （CalamityModClassicPreTrailer）的 CoreofEleum 来源不同，故分两分支注册，均在秘银砧合成
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：用本模组自有的 CoreofEleum 材料
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.PaladinsShield);
                recipe.AddIngredient(ItemID.FrozenTurtleShell);
                recipe.AddIngredient<CoreofEleum>(5);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：从灾厄经典版查找同名 CoreofEleum
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.PaladinsShield);
                recipe1.AddIngredient(ItemID.FrozenTurtleShell);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofEleum").Type, 5);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
