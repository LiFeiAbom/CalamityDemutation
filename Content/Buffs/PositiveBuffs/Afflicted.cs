using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 受难增益：由队友的"烦恼"效果传染给同队玩家，提供伤害、防御、生命上限与回复加成
    /// </summary>
    internal class Afflicted:ModBuff
    {
        /// <summary>
        /// 注册为正面增益，允许 PvP 传播且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 非减益：图标按增益（buff）显示
            Main.debuff[Type] = false;
            // 允许在 PvP 中传染给同队队友
            Main.pvpBuff[Type] = true;
            // 不随角色存档保存（每次需重新获得）
            Main.buffNoSave[Type] = true;
            // 专家/大师模式不延长该效果时长
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }
        /// <summary>
        /// 每帧置位 afflicted 标记，具体数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            // 置位"受难"标记，具体属性加成在 ModPlayer.PostUpdateMiscEffects 中统一结算
            player.GetModPlayer<CalamityDemutationPlayer>().afflicted = true;
        }
    }
}
