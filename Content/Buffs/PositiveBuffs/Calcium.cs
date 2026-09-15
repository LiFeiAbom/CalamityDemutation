using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 钙质增益（Calcium）：由钙质药水（CalciumPotion）赋予。
    /// 每帧仅置位 calcium 标记，效果在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算：
    /// 免疫坠落伤害（noFallDmg）。
    /// </summary>
    internal class Calcium:ModBuff
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
        /// 每帧置位 calcium 标记，供 PostUpdateMiscEffects 关闭坠落伤害
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().calcium = true;   // 置位标记，供 PostUpdateMiscEffects 读取
        }
    }
}
