using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 天狼星召唤增益（SiriusBuff）—— 天狼星（Sirius）召唤的星灵在场时保持此增益
    ///（按灾厄 2.0 Buffs/Summon/SiriusBuff.cs 移植）。
    /// 增益只负责把玩家侧的 <c>sirius</c> 标志点亮/熄灭——星灵弹幕据此续命、消失时随之移除。
    /// </summary>
    internal class SiriusBuff:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;   // 常驻显示：图标不显示剩余时间条
            Main.buffNoSave[Type] = true;          // 不随存档保存：是否生效只看星灵是否在场
        }
        /// <summary>
        /// 星灵在场时置位 sirius 并续期；标记仍为 false 说明星灵已消失，移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SiriusMinion>()] > 0)
            {
                modPlayer.sirius = true;
            }
            if (!modPlayer.sirius)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 星灵仍在场：续满剩余时间
            }
        }
    }
}
