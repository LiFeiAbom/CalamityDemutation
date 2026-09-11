using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 亚利姆之力增益：由亚利姆兴奋剂提供，大幅提升伤害、暴击、攻速、移动与生存属性
    /// </summary>
    internal class YharimPower:ModBuff
    {
        /// <summary>
        /// 注册为正面增益，允许 PvP 传播且随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 非减益：图标按增益（buff）显示
            Main.debuff[Type] = false;
            // 允许在 PvP 中传播
            Main.pvpBuff[Type] = true;
            // 随角色存档保存（药水类增益保留至存档）
            Main.buffNoSave[Type] = false;
            // 专家/大师模式不延长该效果时长
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }
        /// <summary>
        /// 每帧置位 yharimPower 标记，具体数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            // 置位"亚利姆之力"标记，具体属性加成在 ModPlayer.PostUpdateMiscEffects 中统一结算
            player.GetModPlayer<CalamityDemutationPlayer>().yharimPower = true;
        }
    }
}
