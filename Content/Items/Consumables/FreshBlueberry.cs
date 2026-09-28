using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Consumables
{
    /// <summary>
    /// 新鲜蓝莓（FreshBlueberry）—— 永久增益消耗品（本工程新增，非移植）。
    /// 商人处 **1 金**售卖，**击败血肉之墙后**才上架；每个角色**限用一次**。
    /// 效果：永久 **+4 点护甲穿透**（通用职业；加成在 <c>CalamityDemutationPlayer.PostUpdateEquips</c> 里按标记每帧叠加）。
    /// 实现照本工程既有的「永久解锁」通路（<see cref="CelestialOnion"/>）。
    /// </summary>
    internal class FreshBlueberry : ModItem
    {
        /// <summary>基础属性：24×24、可堆叠、举起使用、用后消耗</summary>
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 99;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item4;
            Item.consumable = true;
        }
        /// <summary>已吃过就禁用</summary>
        public override bool CanUseItem(Player player)
        {
            return !player.GetModPlayer<CalamityDemutationPlayer>().freshBlueberry;
        }
        /// <summary>置位永久标志（本帧只结算一次），跨端同步走 MsgPermanentUnlock 通路</summary>
        public override bool? UseItem(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.itemAnimation > 0 && !modPlayer.freshBlueberry && player.itemTime == 0)
            {
                player.itemTime = Item.useTime;
                modPlayer.freshBlueberry = true;
            }
            return true;
        }
    }
}
