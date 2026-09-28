using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 捕蝇草球茎（TrapperBulb）—— 照搬灾厄 2.0.3.9 的 <c>Items/Materials/TrapperBulb.cs</c>。
    /// 本工程自有的丛林材料，供「（古）」链的生命露（古）当配方材料用
    /// （原方案是软依赖回引灾厄的同名材料，但那件在 1.4.4 世系里不存在，故改为自持）。
    /// **无配方**：来源是愤怒捕蝇草掉落，见 CalamityDemutationGlobalNPC.ModifyNPCLoot。
    /// </summary>
    internal class TrapperBulb : ModItem
    {
        /// <summary>研究所解锁数量 25（照源）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
        }
        /// <summary>基础属性：20×20、堆叠 9999、售出价 16 银、稀有度浅红</summary>
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(0, 0, 16, 0);
            Item.rare = ItemRarityID.LightRed;
        }
    }
}
