using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 血耀核心 - 专家饰品
    /// 低生命时获得减伤与增伤，低防御时获得额外增伤（具体数值见 CalamityDemutationPlayer）。
    /// </summary>
    internal class BloodflareCore:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、专家物品与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                          // 贴图宽（像素）
            Item.height = 26;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 45, 0, 0);  // 价值 45 金
            Item.expert = true;                       // 标记为专家物品（专家模式专属外观框）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位标记，实际数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，实际数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.bloodflareCore = true;
        }
    }
}
