using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 神圣之怒增益（Holy Wrath）：由神圣之怒药水（HolyWrathPotion）赋予的输出/机动增益。
    /// 每帧仅置位 holyWrath 标记，数值分布在三处结算：
    /// CalamityDemutationPlayer.PostUpdateMiscEffects（伤害 +12%、移速 +5%）、
    /// CalamityDemutationGlobalItem.HorizontalWingSpeeds（翅膀速度 +5%）。
    /// </summary>
    internal class HolyWrath: ModBuff
    {
        /// <summary>
        /// 注册为正面增益：允许 PvP 传播且随存档保存（该增益不参与专家/大师模式时长延长）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;      // 非减益：按增益图标显示
            Main.pvpBuff[Type] = true;      // 允许在 PvP 中传播
            Main.buffNoSave[Type] = false;  // 随存档保存
        }
        /// <summary>
        /// 每帧置位 holyWrath 标记，供 PostUpdateMiscEffects 与翅膀速度结算读取
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().holyWrath = true;   // 置位标记，供 PostUpdateMiscEffects / 翅膀速度读取
        }
    }
}
