using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 弹跳增益（Bounding）：由弹跳药水（BoundingPotion）赋予的跳跃强化。
    /// 本类每帧仅置位 bounding 标记，数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算：
    /// 跳跃速度 +0.5、跳跃高度 +10、坠落伤害减免 +25。
    /// </summary>
    internal class Bounding:ModBuff
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
        /// 每帧置位 bounding 标记，供 PostUpdateMiscEffects 结算跳跃加成
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().bounding = true;   // 置位标记，供 PostUpdateMiscEffects 读取
        }
    }
}
