using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 血肉图腾 - 防御型饰品
    /// 减半敌人接触伤害（每次生效后进入 20 秒冷却）
    /// </summary>
    internal class FleshTotem:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、稀有度、价值与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                          // 贴图宽（像素）
            Item.height = 26;                         // 贴图高（像素）
            Item.rare = ItemRarityID.Yellow;          // 稀有度：黄色
            Item.value = Item.buyPrice(0, 24, 0, 0);  // 价值 24 金
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 fleshTotem 标记；接触伤害减半与 20 秒冷却
        /// 在 CalamityDemutationPlayer.ModifyHitByNPC / PostUpdateMiscEffects 中处理
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，接触伤害减半与冷却逻辑在 CalamityDemutationPlayer.ModifyHitByNPC 中处理
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.fleshTotem = true;
        }
    }
}
