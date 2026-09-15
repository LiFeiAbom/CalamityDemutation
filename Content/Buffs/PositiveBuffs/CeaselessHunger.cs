using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 无尽饥渴增益（Ceaseless Hunger）：由无尽饥渴药水（CeaselessHungerPotion）赋予。
    /// 每帧仅置位 ceaselessHunger 标记，效果在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算：
    /// 将大范围内的掉落物强行吸附向玩家。
    /// </summary>
    internal class CeaselessHunger:ModBuff
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
        /// 每帧置位 ceaselessHunger 标记，供 PostUpdateMiscEffects 结算物品吸附
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().ceaselessHunger = true;   // 置位标记，供 PostUpdateMiscEffects 读取
        }
    }
}
