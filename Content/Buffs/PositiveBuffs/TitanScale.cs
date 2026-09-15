using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 泰坦鳞片增益（Titan Scale）：由泰坦鳞片药水（TitanScalePotion）赋予的防御增益。
    /// 每帧仅置位 titanScale 标记，数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算：
    /// 伤害减免 +5%、防御 +5、抗击退。
    /// </summary>
    internal class TitanScale:ModBuff
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
        /// 每帧置位 titanScale 标记，供 PostUpdateMiscEffects 结算防御加成
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().titanScale = true;   // 置位标记，供 PostUpdateMiscEffects 读取
        }
    }
}
