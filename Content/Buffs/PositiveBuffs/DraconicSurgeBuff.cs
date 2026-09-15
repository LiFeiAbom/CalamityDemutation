using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 龙之涌动（Draconic Surge）：由龙之药剂（DraconicElixir）赋予的强力飞行增益。
    /// 该增益为正面效果（Main.debuff = false），故归入 PositiveBuffs。
    /// 每帧仅置位 draconicSurge 标记，数值分布在两处结算：
    /// CalamityDemutationPlayer.PostUpdateMiscEffects（翅膀飞行时间 ×1.35、防御 +16）、
    /// CalamityDemutationGlobalItem.HorizontalWingSpeeds（翅膀速度与加速各 +15%）；
    /// 药剂本身另有 draconicSurgeCooldown 冷却限制（见 DraconicElixir.CanUseItem）。
    /// </summary>
    internal class DraconicSurgeBuff:ModBuff
    {
        /// <summary>
        /// 注册为正面增益：允许 PvP 传播且随存档保存，专家/大师模式不延长时长
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;                      // 非减益：按增益图标显示（并非字面意义的 debuff）
            Main.pvpBuff[Type] = true;                      // 允许在 PvP 中传播
            Main.buffNoSave[Type] = false;                  // 随存档保存
            BuffID.Sets.LongerExpertDebuff[Type] = false;   // 专家/大师模式不延长时长
        }
        /// <summary>
        /// 每帧置位 draconicSurge 标记，供 PostUpdateMiscEffects 与翅膀速度结算读取
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().draconicSurge = true;   // 置位标记，供 PostUpdateMiscEffects / 翅膀速度读取
        }
    }
}
