using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 归虚之灵召唤增益（源里类名叫 <c>CosmicEnergyOld</c>，本工程收成 <c>CosmicEnergyBuff</c>）——
    /// 宇宙之灵在场时保持此增益（按 CI 的 Buffs/Summon/CosmicEnergyOld.cs 移植）。
    /// 增益只负责把玩家侧的 <c>cosmicEnergy</c> 标志点亮/熄灭——宇宙之灵据此续命、消失时随之移除。
    /// </summary>
    internal class CosmicEnergyBuff:ModBuff
    {
        /// <summary>召唤物增益：常驻显示（无时间条）且不随存档保存</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        /// <summary>宇宙之灵在场时置位 cosmicEnergy 并续期；标记仍为 false 说明已消失，移除增益</summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.CosmicEnergySpiral>()] > 0)
            {
                modPlayer.cosmicEnergy = true;
            }
            if (!modPlayer.cosmicEnergy)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 宇宙之灵仍在场：续满剩余时间
            }
        }
    }
}
