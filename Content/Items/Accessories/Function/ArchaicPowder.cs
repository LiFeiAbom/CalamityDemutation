using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Function
{
    /// <summary>
    /// 古老粉末 - 功能型饰品
    /// 在地下/洞穴/地狱层时额外获得防御、减伤与挖掘速度。
    /// </summary>
    internal class ArchaicPowder:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                         // 贴图宽（像素）
            Item.height = 20;                        // 贴图高（像素）
            Item.value = Item.buyPrice(0, 9, 0, 0);  // 售价 9 金
            Item.rare = ItemRarityID.Orange;         // 稀有度：橙色
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().archaicPowder = true;
        }
        /// <summary>
        /// 配方（分版本）：现代版用远古化石+远古凿子+远古骨粉+骨头；经典版用恶魔骨灰/腐肉替代
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<AncientFossil>();
                recipe.AddIngredient(ItemID.AncientChisel);
                recipe.AddIngredient(calamity.Find<ModItem>("AncientBoneDust").Type, 3);
                recipe.AddIngredient(ItemID.Bone, 15);
                recipe.AddTile(TileID.Anvils);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<AncientFossil>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("DemonicBoneAsh").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("AncientBoneDust").Type, 3);
                recipe1.AddIngredient(ItemID.RottenChunk, 10);
                recipe1.AddTile(TileID.Anvils);
                recipe1.Register();
            }
        }
    }
}
