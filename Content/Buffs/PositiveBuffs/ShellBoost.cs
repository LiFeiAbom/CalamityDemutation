using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 甲壳加速增益（Shell Speed Boost）：玩家受到伤害时由巨壳（GiantShell）/ 海绵（Sponge）/
    /// 吞噬者（TheAbsorber）的受伤特效赋予（ModPlayer.OnHurt 中 AddBuff），
    /// 效果期间获得大幅移动速度加成（+0.9）。
    /// </summary>
    internal class ShellBoost:ModBuff
    {
        /// <summary>
        /// 注册为正面增益，允许 PvP 传播且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 非减益：图标按增益（buff）显示
            Main.debuff[Type] = false;
            // 允许在 PvP 中传播
            Main.pvpBuff[Type] = true;
            // 不随存档保存：由受伤事件临时赋予，无需跨存档保留
            Main.buffNoSave[Type] = true;
            // 专家/大师模式不延长该效果时长
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }
        /// <summary>
        /// 每帧置位 shellBoost 标记；实际移速加成在 ModPlayer.UpdateBadLifeRegen 中结算
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            // 置位甲壳加速标记，供 ModPlayer 判断增益是否仍生效
            player.GetModPlayer<CalamityDemutationPlayer>().shellBoost = true;
        }
    }
}
