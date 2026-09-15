using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Function
{
    /// <summary>
    /// 霜冻屏障（Frost Barrier）：功能性饰品，提供 4 点防御，
    /// 装备时置位 frostBarrier 标记，受击时冻结周围敌人（结算见 CalamityDemutationPlayer 的受击回调）。
    /// </summary>
    internal class FrostBarrier:ModItem
    {
        /// <summary>
        /// 饰品基础属性：尺寸、价值、稀有度与 4 点防御
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 4;                             // 防御 +4
            Item.width = 20;                              // 贴图宽（像素）
            Item.height = 24;                             // 贴图高（像素）
            Item.value = Item.buyPrice(0, 9, 0, 0);       // 价值 9 金
            Item.rare = ItemRarityID.Orange;              // 稀有度：橙色
            Item.accessory = true;                        // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 frostBarrier 标记；冻结效果在 CalamityDemutationPlayer 的受击回调中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.frostBarrier = true;   // 置位霜冻屏障标记，供受击冻结结算读取
        }
    }
}
