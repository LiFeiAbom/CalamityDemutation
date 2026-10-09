using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 空灵征服者召唤增益（源里类名就叫 <c>Phantom</c>，本工程加 Buff 后缀，免得与别的同名件混淆）——
    /// 幻影仆从在场时保持此增益（按灾厄 2.0.3.9 Buffs/Summon/Phantom.cs 移植）。
    /// 增益只负责把玩家侧的 <c>pGuy</c> 标志点亮/熄灭——幻影据此续命、消失时随之移除。
    /// </summary>
    internal class PhantomBuff:ModBuff
    {
        /// <summary>召唤物增益：常驻显示（无时间条）且不随存档保存</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        /// <summary>幻影在场时置位 pGuy 并续期；标记仍为 false 说明幻影已消失，移除增益</summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.PhantomGuy>()] > 0)
            {
                modPlayer.pGuy = true;
            }
            if (!modPlayer.pGuy)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 幻影仍在场：续满剩余时间
            }
        }
    }
}
