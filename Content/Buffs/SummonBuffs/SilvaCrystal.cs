using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 远古叶棱晶召唤增益（SilvaCrystal）：始源林海召唤头（SilvaHelmet）套装召唤的 SilvaCrystal 在场时保持此增益
    ///（按经典版灾厄 Buffs/SilvaCrystal.cs 移植）。
    /// 增益只负责把玩家侧的 <c>sCrystal</c> 标志点亮/熄灭——叶棱晶弹幕据此续命、消失时随之移除。
    /// </summary>
    internal class SilvaCrystal:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;   // 常驻显示：图标不显示剩余时间条
            Main.buffNoSave[Type] = true;          // 不随存档保存：是否生效只看叶棱晶是否在场
        }
        /// <summary>
        /// 叶棱晶在场时置位 sCrystal 并续期；标记仍为 false 说明棱晶已消失，移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SilvaCrystal>()] > 0)
            {
                modPlayer.sCrystal = true;
            }
            if (!modPlayer.sCrystal)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 棱晶仍在场：续满剩余时间
            }
        }
    }
}
