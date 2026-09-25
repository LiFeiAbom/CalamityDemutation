using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 利维坦龙涎香（LeviathanAmbergris） - 综合型专家饰品（灾厄利维坦的困难模式宝藏袋掉落）
    /// 提供水下作战能力：无视水体移动、全地形常驻 +5% 通用伤害；
    /// 处于水中（溺水判定区域）时额外 +20 防御与 +75% 移速；
    /// 移动时朝周围释放毒海水弹，潜水中受击时使附近敌人中毒。
    /// </summary>
    internal class LeviathanAmbergris:ModItem
    {
        /// <summary>
        /// 基础属性：小尺寸贴图、专家限定、价值 30 金、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 22;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 30, 0, 0);  // 价值 30 金（专家饰品档）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
            Item.expert = true;                       // 标记为专家物品（利维坦宝藏袋专属）
        }
        /// <summary>
        /// 装备时仅置位标记，水下增伤/防御/移速、毒海水弹与中毒光环均统一在 CalamityDemutationPlayer 中结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().levianthanAmbergris = true;
        }
        /// <summary>
        /// 互斥：大杂烩已包含利维坦龙涎香效果，禁止反向与大杂烩同装（与大杂烩侧互为双向）
        /// </summary>
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) => incomingItem.type != ModContent.ItemType<TheAmalgam>();
    }
}
