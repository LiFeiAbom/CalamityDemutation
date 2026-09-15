using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 复苏增益（Revivify）：由复苏药水（RevivifyPotion）赋予的受击回复效果。
    /// 每帧仅置位 revivify 标记，效果在 CalamityDemutationPlayer.OnHurt 中结算：
    /// 每次受到伤害时按本次伤害的 1/15 回复生命。
    /// </summary>
    internal class Revivify:ModBuff
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
        /// 每帧置位 revivify 标记，供 OnHurt 结算受击回血
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().revivify = true;   // 置位标记，供 OnHurt 读取
        }
    }
}
