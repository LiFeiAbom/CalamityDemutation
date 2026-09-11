using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 苦难 - 专家饰品
    /// 提供 +15% 通用伤害、+45 防御、+8% 减伤、+20% 最大生命与 +8 生命回复，
    /// 并将"苦难"debuff 传染给同队玩家。
    /// </summary>
    internal class Affliction:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、饰品与专家物品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 售价 60 金
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
            Item.expert = true;                       // 标记为专家物品（专家模式专属外观框）
        }
        /// <summary>
        /// 装备时置位标记，数值与队友"苦难"debuff 传播均在 CalamityDemutationPlayer 中结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，数值与队友 debuff 传播均在 CalamityDemutationPlayer 中结算
            player.GetModPlayer<CalamityDemutationPlayer>().affliction = true;
        }
    }
}
