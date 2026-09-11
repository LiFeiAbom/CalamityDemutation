using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 真菌甲壳 - 防御型饰品
    /// 受击后释放孢子弹幕反击敌人。
    /// </summary>
    internal class FungalCarapace:ModItem
    {
        /// <summary>
        /// 物品基础属性：防御、尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 2;                         // 装备时 +2 防御
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 24;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 价值 15 金
            Item.rare = ItemRarityID.Pink;            // 稀有度：粉色
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.fungalCarapace = true;
        }
    }
}
