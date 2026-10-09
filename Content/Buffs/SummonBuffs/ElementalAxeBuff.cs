using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 元素之斧召唤增益（ElementalAxeBuff）—— 元素之斧仆从在场时保持此增益
    ///（按灾厄 2.0.3.9 Buffs/Summon/ElementalAxeBuff.cs 移植）。
    /// 增益只负责把玩家侧的 <c>eAxe</c> 标志点亮/熄灭——仆从据此续命、消失时随之移除。
    /// </summary>
    internal class ElementalAxeBuff:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;   // 常驻显示：图标不显示剩余时间条
            Main.buffNoSave[Type] = true;          // 不随存档保存：是否生效只看仆从是否在场
        }
        /// <summary>
        /// 仆从在场时置位 eAxe 并续期；标记仍为 false 说明仆从已消失，移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.ElementalAxeMinion>()] > 0)
            {
                modPlayer.eAxe = true;
            }
            if (!modPlayer.eAxe)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;   // 仆从仍在场：续满剩余时间
            }
        }
    }
}
