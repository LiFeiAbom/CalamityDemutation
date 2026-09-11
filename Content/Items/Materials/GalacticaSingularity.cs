using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 银河奇点：高阶合成材料，由四种月亮碎片合成，用于制作元素方舟等终局武器
    /// </summary>
    internal class GalacticaSingularity:ModItem
    {
        /// <summary>
        /// 注册物品动画与渲染特性：灵魂式动画、无重力悬浮、最高材料排序优先级
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(4, 24));
            ItemID.Sets.AnimatesAsSoul[Type] = true;         // 以"灵魂"动画方式渲染
            ItemID.Sets.ItemNoGravity[Type] = true;          // 掉落物无重力（悬浮）
            ItemID.Sets.SortingPriorityMaterials[Type] = 99; // 最高材料排序优先级，使本材料排在合成材料前列
        }
        /// <summary>
        /// 基础属性：中尺寸、普通堆叠上限、红色稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(silver: 80);
            Item.rare = ItemRarityID.Red;
        }
        /// <summary>
        /// 掉落物特效：发出随机的微红色光芒（闪烁效果）
        /// </summary>
        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            float brightness = Main.rand.Next(90, 111) * 0.01f;
            brightness *= Main.essScale;
            Lighting.AddLight((int)((Item.position.X + Item.width / 2) / 16f), (int)((Item.position.Y + Item.height / 2) / 16f), 1f * brightness, 0.3f * brightness, 0.3f * brightness);
        }
        /// <summary>
        /// 仅在灾厄模组存在时，用四种月亮碎片在远古操纵机合成
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.FragmentSolar);
                recipe.AddIngredient(ItemID.FragmentVortex);
                recipe.AddIngredient(ItemID.FragmentNebula);
                recipe.AddIngredient(ItemID.FragmentStardust);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
        }
    }
}
