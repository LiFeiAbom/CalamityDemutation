using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 机械蠕虫召唤增益：弑神者召唤头（GodSlayerHornedHelm）召唤的 MechwormHead 在场时保持此增益
    ///（按经典版灾厄 Buffs/Mechworm.cs 移植）。
    /// </summary>
    internal class Mechworm:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;   // 常驻显示：图标不显示剩余时间条
            Main.buffNoSave[Type] = true;          // 不随存档保存：是否生效只看蠕虫头是否在场
        }
        /// <summary>
        /// 蠕虫头在场时置位 mWorm 并续期；标记仍为 false 说明蠕虫已消失，移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.MechwormHead>()] > 0)
            {
                modPlayer.mWorm = true;
            }
            if (!modPlayer.mWorm)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 蠕虫仍在场：续满剩余时间
            }
        }
    }
}
