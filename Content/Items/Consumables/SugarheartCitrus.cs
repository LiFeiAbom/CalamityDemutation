using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Consumables
{
    /// <summary>
    /// 糖心柑橘（SugarheartCitrus）—— 永久增益消耗品（本工程新增，非移植）。
    /// 商人处 **1 金**售卖，**击败血肉之墙后**才上架；每个角色**限用一次**。
    /// 效果：永久 **+4% 近战攻击速度**（加成在 <c>CalamityDemutationPlayer.PostUpdateEquips</c> 里按标记每帧叠加）。
    /// 实现照本工程既有的「永久解锁」通路（<see cref="CelestialOnion"/>）：门控 → 置位 → 存档 → 多端同步。
    /// </summary>
    internal class SugarheartCitrus : ModItem
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
        /// <summary>已吃过就禁用：返回 false 时物品不消耗、也不进入 UseItem</summary>
        public override bool CanUseItem(Player player)
        {
            return !player.GetModPlayer<CalamityDemutationPlayer>().sugarheartCitrus;
        }
        /// <summary>
        /// 置位永久标志（<c>itemAnimation &gt; 0 &amp;&amp; !已解锁 &amp;&amp; itemTime == 0</c> 保证本帧只结算一次），
        /// 跨端同步由 <c>SendClientChanges</c> → MsgPermanentUnlock 通路完成
        /// </summary>
        public override bool? UseItem(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.itemAnimation > 0 && !modPlayer.sugarheartCitrus && player.itemTime == 0)
            {
                player.itemTime = Item.useTime;
                modPlayer.sugarheartCitrus = true;
            }
            return true;
        }
    }
}
