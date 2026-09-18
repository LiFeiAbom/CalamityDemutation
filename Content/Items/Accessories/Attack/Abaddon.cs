using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Attack
{
    /// <summary>
    /// 阿巴顿（Abaddon） - 攻击型饰品，掉落自红魔鬼（普通 1/12、专家 1/7）。
    /// 效果：+8% 通用暴击率；暴击命中时在命中点炸出一次硫磺爆炸（附近敌人燃起硫磺火）；
    /// 免疫硫磺火减益 —— 灾厄原版是"大幅降低硫磺火 DoT 伤害"（30 → 10），本工程按用户口径改为完全免疫。
    /// 外观：物品贴图 + 脸部装备层（EquipType.Face，灾厄原版的"面部印记"）。
    /// 属性与爆炸触发统一在 CalamityDemutationPlayer 中结算，见玩家文件的 abaddon 标记。
    /// </summary>
    [AutoloadEquip(EquipType.Face)]   // 装备时在玩家脸部叠加印记外观（需 Abaddon_Face.png）
    internal class Abaddon:ModItem
    {
        /// <summary>
        /// 基础属性：26x26、价值 24 金（灾厄 Rarity5BuyPrice）、稀有度粉（5）、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.value = Item.buyPrice(0, 24, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时只置位标记：暴击加成、硫磺火免疫与暴击爆炸的触发都写在 CalamityDemutationPlayer 里
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().abaddon = true;
        }
    }
}
