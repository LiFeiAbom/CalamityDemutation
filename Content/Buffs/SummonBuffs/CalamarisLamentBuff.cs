using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 灾厄挽歌召唤增益（CalamarisLamentBuff）—— 小鱿鱼在场时保持此增益
    ///（按灾厄 2.0.3.9 Buffs/Summon/CalamarisLamentBuff.cs 移植）。
    /// 增益只负责把玩家侧的 <c>calamarisLament</c> 标志点亮/熄灭——小鱿鱼据此续命、消失时随之移除。
    /// </summary>
    internal class CalamarisLamentBuff:ModBuff
    {
        /// <summary>召唤物增益：常驻显示（无时间条）且不随存档保存</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        /// <summary>小鱿鱼在场时置位标志并续期；标记仍为 false 说明已消失，移除增益</summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.CalamarisLamentMinion>()] > 0)
            {
                modPlayer.calamarisLament = true;
            }
            if (!modPlayer.calamarisLament)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 小鱿鱼仍在场：续满剩余时间
            }
        }
    }
}
