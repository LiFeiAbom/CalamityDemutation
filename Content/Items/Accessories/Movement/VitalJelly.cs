using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 活力果冻 - 移动型饰品
    /// 提升移动与跳跃速度。
    /// </summary>
    internal class VitalJelly:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 24;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 12, 0, 0);  // 价值 12 金
            Item.rare = ItemRarityID.Pink;            // 稀有度：粉色
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().vitalJelly = true;
        }
    }
}
