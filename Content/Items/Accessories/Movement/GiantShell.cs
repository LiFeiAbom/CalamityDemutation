using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 巨型贝壳 - 移动型饰品
    /// 降低移动速度，但受击后触发大幅加速的增益。
    /// </summary>
    internal class GiantShell:ModItem
    {
        /// <summary>
        /// 物品基础属性：防御、尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 6;                        // 装备时 +6 防御
            Item.width = 20;                         // 贴图宽（像素）
            Item.height = 24;                        // 贴图高（像素）
            Item.value = Item.buyPrice(0, 3, 0, 0);  // 价值 3 金
            Item.rare = ItemRarityID.Blue;           // 稀有度：蓝色
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.giantShell = true;
        }
    }
}
