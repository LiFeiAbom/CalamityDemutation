using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 亵渎之怒增益（Profaned Rage）：由亵渎之怒药水（ProfanedRagePotion）赋予的输出/机动增益。
    /// 每帧仅置位 profanedRage 标记，数值分布在两处结算：
    /// CalamityDemutationPlayer.PostUpdateMiscEffects（暴击率 +12%、移速 +5%）、
    /// CalamityDemutationGlobalItem.HorizontalWingSpeeds（翅膀速度 +5%）。
    /// </summary>
    internal class ProfanedRage:ModBuff
    {
        /// <summary>
        /// 注册为正面增益：允许 PvP 传播且随存档保存，专家/大师模式不延长时长
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;                      // 非减益：按增益图标显示
            Main.pvpBuff[Type] = true;                      // 允许在 PvP 中传播
            Main.buffNoSave[Type] = false;                  // 随存档保存
            BuffID.Sets.LongerExpertDebuff[Type] = false;   // 专家/大师模式不延长时长
        }
        /// <summary>
        /// 每帧置位 profanedRage 标记，供 PostUpdateMiscEffects 与翅膀速度结算读取
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().profanedRage = true;   // 置位标记，供 PostUpdateMiscEffects / 翅膀速度读取
        }
    }
}
