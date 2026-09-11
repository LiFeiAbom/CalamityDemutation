using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Melee
{
    /// <summary>
    /// 血蠕虫牙 - 近战职业专家饰品
    /// 半血以下提供 +10% 近战伤害/攻速与伤害减免，否则各 +5%。
    /// </summary>
    internal class BloodyWormTooth:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、专家专属与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 12;                        // 贴图宽 12 像素
            Item.height = 15;                       // 贴图高 15 像素
            Item.value = Item.buyPrice(0, 9, 0, 0); // 价值 9 金
            Item.expert = true;                     // 专家模式专属物品
            Item.accessory = true;                  // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 bloodyWormTooth 标记，具体数值（含半血翻倍判定）在 CalamityDemutationPlayer 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算统一在 CalamityDemutationPlayer 中完成
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.bloodyWormTooth = true;
        }
    }
}
