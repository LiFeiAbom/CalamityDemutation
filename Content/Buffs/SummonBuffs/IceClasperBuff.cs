using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 古冰晶召唤增益（IceClasperBuff）—— 冰灵仆从在场时保持此增益
    ///（按灾厄 2.0.3.9 Buffs/Summon/IceClasperBuff.cs 移植）。
    /// 增益只负责把玩家侧的 <c>iceClasperBool</c> 标志点亮/熄灭——冰灵据此续命、消失时随之移除。
    /// </summary>
    internal class IceClasperBuff:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;   // 常驻显示：图标不显示剩余时间条
            Main.buffNoSave[Type] = true;          // 不随存档保存：是否生效只看冰灵是否在场
        }
        /// <summary>
        /// 冰灵在场时置位 iceClasperBool 并续期；标记仍为 false 说明冰灵已消失，移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.IceClasperMinion>()] > 0)
            {
                modPlayer.iceClasperBool = true;
            }
            if (!modPlayer.iceClasperBool)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 冰灵仍在场：续满剩余时间
            }
        }
    }
}
