using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Attack
{
    /// <summary>
    /// 灾厄之戒 - 攻击型专家饰品（灾厄（克隆体）宝藏袋掉落）
    /// 提供 +15% 通用伤害；受击处于无敌帧时，有 10% 概率在**玩家上方**（水平 ±400、高 500~800 像素，
    /// 见 CalamityDemutationPlayer 的 calamityRing 分支）生成一团朝玩家方向落下的站火。
    /// 与灾厄之戒/虚空灭绝等同类增伤戒互相排斥。
    /// </summary>
    internal class CalamityRing:ModItem
    {
        /// <summary>
        /// 基础属性：小尺寸贴图、青柠稀有度、专家限定、价值 24 金、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 22;
            Item.value = Item.buyPrice(0, 24, 0, 0);
            Item.rare = ItemRarityID.Lime;
            Item.accessory = true;
            Item.expert = true;
        }
        /// <summary>
        /// 装备时仅置位标记，增伤与受击烈焰统一在 CalamityDemutationPlayer 中结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().calamityRing = true;
        }
        /// <summary>
        /// 互斥判定：本饰品已并入虚空灭绝/大杂烩（其效果含灾厄之戒），
        /// 禁止与灾厄之戒/虚空灭绝/大杂烩同装备，避免 +15% 通用伤害叠加
        /// </summary>
        public override bool CanEquipAccessory(Player player, int slot, bool modded)/* tModPorter Suggestion: Consider using new hook CanAccessoryBeEquippedWith */
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.calamityRing || modPlayer.voidofExtinction || modPlayer.theAmalgam)
            {
                return false;
            }
            return true;
        }
    }
}
