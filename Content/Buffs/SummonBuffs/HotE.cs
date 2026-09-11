using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 元素之心召唤增益：装备元素之心（allWaifus 标记置位）时保持此增益
    /// </summary>
    internal class HotE:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 常驻显示：图标不显示剩余时间条
            Main.buffNoTimeDisplay[Type] = true;
            // 不随存档保存：是否生效完全取决于饰品是否装备
            Main.buffNoSave[Type] = true;
        }
        /// <summary>
        /// 与其它召唤增益不同，本增益直接依据 allWaifus 标记（由元素之心饰品置位）判断是否保留
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (!modPlayer.allWaifus)
            {
                // allWaifus 未置位说明元素之心已卸下或玩家死亡，移除增益
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // 元素之心仍在生效：续满剩余时间（18000 帧 ≈ 5 分钟），使增益常驻
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
