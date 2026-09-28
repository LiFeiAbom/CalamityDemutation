using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 污浊糊（MurkyPaste）—— 照搬灾厄 2.0.3.9 的 <c>Items/Materials/MurkyPaste.cs</c>。
    /// 本工程自有的丛林前期材料，供「（古）」链的蜜露（古）当配方材料用
    /// （原方案是软依赖回引灾厄的同名材料，但那件在 1.4.4 世系里不存在，故改为自持）。
    /// **无配方**：来源是丛林敌人掉落，见 CalamityDemutationGlobalNPC.ModifyNPCLoot。
    /// </summary>
    internal class MurkyPaste : ModItem
    {
        /// <summary>研究所解锁数量 5（照源）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }
        /// <summary>基础属性：24×32、堆叠 9999、售出价 2 银、稀有度蓝</summary>
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 32;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(0, 0, 2, 0);
            Item.rare = ItemRarityID.Blue;
        }
    }
}
