using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.StatLife
{
    /// <summary>
    /// 血契 - 生命型饰品
    /// 最大生命翻倍，但代价是有 25% 概率被"暴击"（受到 2.5 倍伤害）。
    /// </summary>
    internal class BloodPact:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、稀有度、售价与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                          // 贴图宽（像素）
            Item.height = 26;                         // 贴图高（像素）
            Item.rare = ItemRarityID.Yellow;          // 稀有度：黄色
            Item.value = Item.buyPrice(0, 24, 0, 0);  // 售价 24 金
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 bloodPact 标记；最大生命翻倍在 CalamityDemutationPlayer.PostUpdateMiscEffects
        /// 中结算，受击时的 25% 概率 2.5 倍伤害在 ModifyHurt 中处理
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算与受击惩罚统一在 CalamityDemutationPlayer 中完成
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.bloodPact = true;
        }
    }
}
