using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 焦油生命回复（TarraLifeRegen） - 正面增益，提供快速的持续生命回复
    /// 由焦油套相关机制随机赋予（90~180 tick），
    /// 效果在 CalamityDemutationPlayer 中结算：+10 生命回复并伴随绿色回血粒子。
    /// </summary>
    internal class TarraLifeRegen:ModBuff
    {
        /// <summary>
        /// 注册为正面增益：允许 PvP 传播、不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 非减益：图标按增益（buff）显示
            Main.debuff[Type] = false;
            // 允许在 PvP 中传播
            Main.pvpBuff[Type] = true;
            // 不随存档保存：由套装效果临时赋予
            Main.buffNoSave[Type] = true;
            // 专家/大师模式下不延长时长
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }
        /// <summary>
        /// 每帧置位 ModPlayer.tarraLifeRegen；实际生命回复在 ModPlayer 中结算
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            // 置位标记：ModPlayer 每帧 lifeRegen += 10，并按 1/10 概率生成绿色回血粒子
            player.GetModPlayer<CalamityDemutationPlayer>().tarraLifeRegen = true;
        }
    }
}
