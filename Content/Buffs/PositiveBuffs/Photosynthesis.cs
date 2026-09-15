using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 光合作用增益（Photosynthesis）：由光合作用药水（PhotosynthesisPotion）赋予。
    /// 每帧仅置位 photosynthesis 标记，效果在 CalamityDemutationPlayer.UpdateLifeRegen 中结算：
    /// 玩家静止不动且未挥动物品时大幅提升生命回复速率，白天按全额加成、夜晚衰减为 1/5。
    /// </summary>
    internal class Photosynthesis:ModBuff
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
        /// 每帧置位 photosynthesis 标记，供 UpdateLifeRegen 结算静止回血
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().photosynthesis = true;   // 置位标记，供 UpdateLifeRegen 读取
        }
    }
}
