using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 太阳之灵召唤增益（SolarSpirit）—— 太阳之灵法杖（SunSpiritStaff）召唤的太阳之灵（SolarPixie）在场时保持此增益
    ///（按灾厄 2.0 Buffs/Summon/SolarSpirit.cs 移植）。
    /// 增益只负责把玩家侧的 sunSpirit 标志点亮/熄灭——太阳之灵弹幕据此续命、消失时随之移除。
    /// </summary>
    internal class SolarSpirit:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;   // 常驻显示：图标不显示剩余时间条
            Main.buffNoSave[Type] = true;          // 不随存档保存：是否生效只看太阳之灵是否在场
        }
        /// <summary>
        /// 太阳之灵在场时置位 sunSpirit 并续期；标记仍为 false 说明灵体已消失，移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SolarPixie>()] > 0)
            {
                modPlayer.sunSpirit = true;
            }
            if (!modPlayer.sunSpirit)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 灵体仍在场：续满剩余时间
            }
        }
    }
}
