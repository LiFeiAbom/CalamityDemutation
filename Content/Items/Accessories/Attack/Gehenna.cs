using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Attack
{
    /// <summary>
    /// 地狱火 - 攻击型专家饰品（硫磺火元素宝藏袋掉落）
    /// 装备后每 10 秒（600 帧）自动从天空朝玩家位置降下一排地狱火弹幕轰击敌人。
    /// </summary>
    internal class Gehenna:ModItem
    {
        /// <summary>
        /// 基础属性：中尺寸贴图、专家限定、价值 15 金、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.expert = true;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时仅置位标记，周期性地狱火弹幕的发射节奏统一在 CalamityDemutationPlayer 中结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().gehenna = true;
        }
    }
}
