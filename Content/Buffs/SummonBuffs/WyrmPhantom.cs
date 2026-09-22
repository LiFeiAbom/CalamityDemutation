using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 虚无幻象（WyrmPhantom） - 噬渊鞭挞（Ystralyn）命中敌人时给自己挂的增益。
    /// 本身不做事，只每帧把玩家的 <c>wyrmPhantom</c> 标记置位，
    /// 由 CalamityDemutationPlayer 据此在场上没有幻影妖龙时补生成一只（见该玩家的 WyrmPhantom partial）。
    /// </summary>
    internal class WyrmPhantom:ModBuff
    {
        /// <summary>不随存档保存；但**显示**剩余时间（CE 原样，buffNoTimeDisplay 显式置 false）</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        /// <summary>每帧置位玩家标记：玩家侧据此维持幻影妖龙在场</summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().wyrmPhantom = true;
        }
    }
}
