using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged
{
    /// <summary>
    /// 疯魔护符（Psychotic Amulet）：射手（远程）职业饰品，
    /// 装备时置位 psychoticAmulet 标记，数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算：
    /// 远程伤害 +5%、远程暴击率 +5%，并启用蘑菇矿潜行（shroomiteStealth）。
    /// </summary>
    internal class PsychoticAmulet:ModItem
    {
        /// <summary>
        /// 饰品基础属性：尺寸、价值与稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                              // 贴图宽（像素）
            Item.height = 26;                             // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);      // 价值 15 金
            Item.rare = ItemRarityID.LightPurple;         // 稀有度：浅紫
            Item.accessory = true;                        // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 psychoticAmulet 标记；远程加成在 PostUpdateMiscEffects 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.psychoticAmulet = true;   // 置位标记，供 PostUpdateMiscEffects 结算远程加成
        }
    }
}
