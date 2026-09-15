using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 韵律增益（Cadence）：由韵律药水（CadencePotion）赋予的生活类增益。
    /// 每帧仅置位 cadence 标记，数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算：
    /// 商店打折、生命磁铁、平静、爱意状态，生命回复 +4、最大生命 +20%。
    /// </summary>
    internal class Cadence:ModBuff
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
        /// 每帧置位 cadence 标记，供 PostUpdateMiscEffects 结算生活类加成
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().cadence = true;   // 置位标记，供 PostUpdateMiscEffects 读取
        }
    }
}
