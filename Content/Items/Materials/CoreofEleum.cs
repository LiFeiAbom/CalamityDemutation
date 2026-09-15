using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 极寒核心：由极寒精华与灵气合成，用于合成诅咒凝滞腰带等饰品
    /// </summary>
    internal class CoreofEleum:ModItem
    {
        /// <summary>
        /// 基础属性：小尺寸、可堆叠 999、黄色稀有度、价值 5 金币
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
        /// 掉落时悬浮（maxFallSpeed=0）并发出蓝紫色微光
        /// </summary>
        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            maxFallSpeed = 0f;
            float num = (float)Main.rand.Next(90, 111) * 0.01f;
            num *= Main.essScale;
            Lighting.AddLight((int)((Item.position.X + (float)(Item.width / 2)) / 16f), (int)((Item.position.Y + (float)(Item.height / 2)) / 16f), 0.15f * num, 0.05f * num, 0.5f * num);
        }
        /// <summary>
        /// 仅在灾厄模组存在时，用极寒精华 + 灵气在秘银砧合成 3 个
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("EssenceofEleum", out ModItem essenceofEleum))
                {
                    Recipe recipe = CreateRecipe(3);
                    recipe.AddIngredient(essenceofEleum.Type);
                    recipe.AddIngredient(ItemID.Ectoplasm);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
        }
    }
}
