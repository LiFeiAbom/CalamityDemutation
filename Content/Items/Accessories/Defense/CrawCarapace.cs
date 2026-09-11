using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 爬行甲壳 - 防御型饰品
    /// 提供减伤与荆棘反伤。
    /// </summary>
    internal class CrawCarapace:ModItem
    {
        /// <summary>
        /// 物品基础属性：防御、尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 3;                        // 装备时 +3 防御
            Item.width = 20;                         // 贴图宽（像素）
            Item.height = 24;                        // 贴图高（像素）
            Item.value = Item.buyPrice(0, 3, 0, 0);  // 售价 3 金
            Item.rare = ItemRarityID.Blue;           // 稀有度：蓝色
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().crawCarapace = true;
        }
    }
}
