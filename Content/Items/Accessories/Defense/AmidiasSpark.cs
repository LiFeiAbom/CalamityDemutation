using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 阿米迪亚斯火花 - 防御型饰品
    /// 受击后向四周发射火花弹幕反击敌人。
    /// </summary>
    internal class AmidiasSpark:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                         // 贴图宽（像素）
            Item.height = 26;                        // 贴图高（像素）
            Item.value = Item.buyPrice(0, 3, 0, 0);  // 售价 3 金
            Item.rare = ItemRarityID.Blue;           // 稀有度：蓝色
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.amidiasSpark = true;
        }
    }
}
