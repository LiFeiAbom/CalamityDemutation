using CalamityDemutation.Players;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.StatLife
{
    /// <summary>
    /// 生命果冻 - 生命型饰品
    /// 提升最大生命，静止时额外生命回复。
    /// </summary>
    internal class LifeJelly:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                         // 贴图宽（像素）
            Item.height = 24;                        // 贴图高（像素）
            Item.value = Item.buyPrice(0, 6, 0, 0);  // 价值 6 金
            Item.rare = ItemRarityID.Blue;           // 稀有度：蓝色
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().lifeJelly = true;
        }
    }
}
