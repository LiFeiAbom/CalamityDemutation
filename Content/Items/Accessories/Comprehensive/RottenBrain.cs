using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 腐化脑（RottenBrain） - 专家饰品（腐巢意志宝藏袋掉落）
    /// 受击进入无敌帧时概率召唤灵气雨（AuraRain）反击；
    /// 生命低于 75% 时 +15% 通用伤害，低于 50% 时再受 -5% 移速惩罚。
    /// </summary>
    internal class RottenBrain:ModItem
    {
        /// <summary>
        /// 基础属性：中尺寸贴图、专家限定、价值 15 金、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 34;                          // 贴图宽（像素）
            Item.height = 34;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 价值 15 金（专家饰品档）
            Item.expert = true;                       // 标记为专家物品（腐巢意志宝藏袋专属）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时仅置位标记，受击反击与低血增减益统一在 CalamityDemutationPlayer 中结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().rottenBrain = true;
        }
    }
}
