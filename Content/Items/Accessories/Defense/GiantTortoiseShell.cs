using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 巨型陆龟甲 - 防御型饰品
    /// 提供荆棘反伤，但会降低移动速度。
    /// </summary>
    internal class GiantTortoiseShell:ModItem
    {
        /// <summary>
        /// 物品基础属性：防御、尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 8;                        // 装备时 +8 防御
            Item.width = 20;                         // 贴图宽（像素）
            Item.height = 24;                        // 贴图高（像素）
            Item.value = Item.buyPrice(0, 9, 0, 0);  // 售价 9 金
            Item.rare = ItemRarityID.Pink;           // 稀有度：粉色
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().giantTortoiseShell = true;
        }
    }
}
