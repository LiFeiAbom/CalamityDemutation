using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories
{
    /// <summary>
    /// 王冠宝石（CrownJewel）—— 灾厄 <b>2.0.3.9</b> 口径（照搬 2.0.3.9 的 <c>CrownJewel</c>）。
    /// 本工程没有同名旧件，故直接用源名。
    /// 是「宝石系」三件的下位：王冠宝石 → 感染宝石 → 无暇粹魂晶，三者**互斥**（源为 else-if 链），
    /// 效果也随之升级。
    /// 装备只置 <c>crownJewel</c> 标记，实际数值在 CalamityDemutationPlayer 中结算。
    /// **源里没有配方**：靠史莱姆王掉落（普通 10% / 专家宝藏袋 10%），本工程同。
    /// </summary>
    internal class CrownJewel:ModItem
    {
        /// <summary>物品基础属性：26×26、防御 +4、稀有度蓝（源为 Rarity1 档，对应 1 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.defense = 4;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
        }
        /// <summary>装备时置位 <c>crownJewel</c>（数值在 CalamityDemutationPlayer 中结算）</summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().crownJewel = true;
        }
        // 源里没有 AddRecipes：获得途径是史莱姆王掉落，见 CalamityDemutationGlobalNPC.ModifyNPCLoot
    }
}
