using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 余烬核心：由烈日精华与灵气合成，用于合成亚利姆徽章
    /// </summary>
    internal class CoreofCinder : ModItem
    {
        /// <summary>
        /// 基础属性：小尺寸、黄色稀有度、价值 5 金币
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 999;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Yellow;
        }
        /// <summary>
        /// 掉落时悬浮并发出微弱橙色光芒
        /// </summary>
        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            maxFallSpeed = 0f;
            float num = (float)Main.rand.Next(90, 111) * 0.01f;
            num *= Main.essScale;
            Lighting.AddLight((int)((Item.position.X + (float)(Item.width / 2)) / 16f), (int)((Item.position.Y + (float)(Item.height / 2)) / 16f), 0.3f * num, 0.3f * num, 0.05f * num);
        }
        /// <summary>
        /// 仅在灾厄模组存在时，用烈日精华 + 灵气在秘银砧合成 3 个
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceofSunlight))
                {
                    Recipe recipe = CreateRecipe(3);
                    recipe.AddIngredient(essenceofSunlight.Type);
                    recipe.AddIngredient(ItemID.Ectoplasm);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
        }
    }
}
