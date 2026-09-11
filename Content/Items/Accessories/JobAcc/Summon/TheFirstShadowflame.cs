using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Summon
{
    /// <summary>
    /// 第一暗影焰 - 召唤职业饰品；召唤弹幕命中时施加暗影焰（ShadowFlame）debuff。
    /// </summary>
    internal class TheFirstShadowflame:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                         // 贴图宽 26 像素
            Item.height = 26;                        // 贴图高 26 像素
            Item.value = Item.buyPrice(0, 15, 0, 0); // 价值 15 金
            Item.rare = ItemRarityID.Pink;           // 稀有度：粉（Pink）
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 theFirstShadowflame 标记，命中施加暗影焰的结算在 CalamityDemutationPlayer 中完成。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().theFirstShadowflame = true;
        }
    }
}
