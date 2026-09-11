using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 真菌团块召唤增益：装备真菌团块（FungalClump）饰品或综合饰品大杂烩（TheAmalgam）时，
    /// 在召唤出的真菌团块（Projectiles.Summon.FungalClump）在场期间保持此增益。
    /// 团块命中敌人会造成伤害并生成治疗孢子（FungalHeal）为玩家吸取生命。
    /// </summary>
    internal class FungalClump:ModBuff
    {
        /// <summary>
        /// 召唤物增益：常驻显示（无时间条）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 常驻显示：图标不显示剩余时间条
            Main.buffNoTimeDisplay[Type] = true;
            // 不随存档保存：是否生效完全取决于召唤物是否在场
            Main.buffNoSave[Type] = true;
        }
        /// <summary>
        /// 每帧检查真菌团块是否在场：在场则续期保留增益，不在场则移除本增益。
        /// "在场"直接以 ownedProjectileCounts（归属玩家实际持有的团块弹幕数）判定，
        /// 不依赖任何 ModPlayer 标记字段；团块弹幕 AI 也不再需要独立的"在场"标志来续命。
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            // 团块仍在场：续满剩余时间（18000 帧 ≈ 5 分钟），使增益常驻
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.FungalClump>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                // 团块已不在场：移除本增益
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
