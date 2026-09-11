using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 海贝 - 防御型饰品
    /// 在水中时额外获得防御、减伤与移速，并可无视水体移动。
    /// </summary>
    internal class SeaShell:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度、基础防御与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 2;                         // 常驻 +2 防御（水中额外加成另在玩家侧结算）
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 24;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 3, 0, 0);   // 售价 3 金
            Item.rare = ItemRarityID.Blue;            // 稀有度：蓝（Blue）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().seaShell = true;
        }
    }
}
