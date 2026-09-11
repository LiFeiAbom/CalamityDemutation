using CalamityDemutation.Players;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Magic
{
    /// <summary>
    /// 魔法果冻 - 魔法职业饰品；+20 最大魔力，站定时额外 +2 魔力回复。
    /// </summary>
    internal class ManaJelly:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                       // 贴图宽 20 像素
            Item.height = 24;                      // 贴图高 24 像素
            Item.value = Item.buyPrice(0, 6, 0, 0); // 价值 6 金
            Item.rare = ItemRarityID.Blue;         // 稀有度：蓝（Blue）
            Item.accessory = true;                 // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 manaJelly 标记，实际数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().manaJelly = true;
        }
    }
}
