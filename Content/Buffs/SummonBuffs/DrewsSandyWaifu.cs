using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// Drew 版沙元素召唤增益：DrewsSandyWaifu 召唤物在场时保持此增益
    /// </summary>
    internal class DrewsSandyWaifu:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 常驻显示：图标不显示剩余时间条
            Main.buffNoTimeDisplay[Type] = true;
            // 不随存档保存：是否生效完全取决于召唤物是否在场
            Main.buffNoSave[Type] = true;
        }
        /// <summary>
        /// 召唤物存在时置位标记并续期，召唤物消失（标记被 ResetEffects 重置）时移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 稀有沙元素（DrewsSandyWaifu）在场时置位标记
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.DrewsSandyWaifu>()] > 0)
            {
                modPlayer.drewsSandyWaifu = true;
            }
            // 标记仍为 false 说明召唤物已不在场，移除增益
            if (!modPlayer.drewsSandyWaifu)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // 召唤物仍在场：续满剩余时间（18000 帧 ≈ 5 分钟），使增益常驻
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
