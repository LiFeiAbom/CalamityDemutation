using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 星律之握览召唤增益（SarosPossessionBuff）—— 辐光光环在场时保持此增益
    ///（按灾厄 2.0 `Buffs/Summon/SarosPossessionBuff.cs` 移植）。
    /// 增益只负责把玩家侧的 <c>radiantResolution</c> 标志点亮/熄灭——光环弹幕据此续命、消失时随之移除。
    /// </summary>
    internal class SarosPossessionBuff:ModBuff
    {
        /// <summary>召唤物增益：常驻显示（无时间条）且不随存档保存</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        /// <summary>
        /// 光环在场时置位 radiantResolution 并续期；标记仍为 false 说明光环已消失，移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SarosAura>()] > 0)
            {
                modPlayer.radiantResolution = true;
            }
            if (!modPlayer.radiantResolution)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
