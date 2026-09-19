using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 虚无碎片（NihilityFragments，移植自 CalamityEntropy）：月后档材料，CE 用它承接灾厄 <c>Necroplasm</c> 那一档的材料
    /// （CE 的虚无系护甲原本就是吃 Necroplasm×6）。聚魂分形的配方要用它。
    /// <para>
    /// 与 CE 原版的差异：① CE 的稀有度是它自研的 <c>NihilityBlue</c>（带动态描边的自定义稀有度），本工程不新建稀有度目录、
    /// 按既有惯例用 <c>ItemRarityID.Red</c>；② <b>获取途径改为灾厄 Boss 掉落</b>：CE 里它由「虚无双子」宝袋必掉 32–40 个，
    /// 那个 Boss 本工程没有，按用户口径挂到同档位的灾厄 Boss —— **神明吞噬者**的宝袋上（见
    /// <c>CalamityDemutationGlobalItem.ModifyItemLoot</c>），掉落量与 CE 保持一致。
    /// </para>
    /// </summary>
    internal class NihilityFragments:ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 120;
            ItemID.Sets.SortingPriorityMaterials[Type] = 98;
        }
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Red;
        }
    }
}
