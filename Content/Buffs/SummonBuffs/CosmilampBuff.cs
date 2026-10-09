using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 宇宙灯笼召唤增益（CosmilampBuff）—— 宇宙灯笼在场时保持此增益
    ///（按灾厄 2.0.3.9 Buffs/Summon/CosmilampBuff.cs 移植）。
    /// 增益只负责把玩家侧的 <c>cLamp</c> 标志点亮/熄灭——灯笼据此续命、消失时随之移除。
    /// </summary>
    internal class CosmilampBuff:ModBuff
    {
        /// <summary>召唤物增益：常驻显示（无时间条）且不随存档保存</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        /// <summary>灯笼在场时置位 cLamp 并续期；标记仍为 false 说明灯笼已消失，移除增益</summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.CosmilampMinion>()] > 0)
            {
                modPlayer.cLamp = true;
            }
            if (!modPlayer.cLamp)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 灯笼仍在场：续满剩余时间
            }
        }
    }
}
