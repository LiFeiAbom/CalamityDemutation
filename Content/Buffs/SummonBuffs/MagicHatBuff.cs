using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 光阴时流伞召唤增益（MagicHatBuff，按 CI 的 `MagicHatBuffOld` 移植；类名去掉 Old 后缀）——
    /// 魔法礼帽（<see cref="Projectiles.Summon.Umbrella.MagicHat"/>）在场时保持此增益。
    /// 增益只负责把玩家侧的 <c>magicHat</c> 标志点亮/熄灭——礼帽弹幕据此续命、消失时随之移除。
    /// </summary>
    internal class MagicHatBuff:ModBuff
    {
        /// <summary>召唤物增益：常驻显示（无时间条）且不随存档保存</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        /// <summary>
        /// 礼帽在场时置位 magicHat 并续期；标记仍为 false 说明礼帽已消失，移除增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Umbrella.MagicHat>()] > 0)
            {
                modPlayer.magicHat = true;
            }
            if (!modPlayer.magicHat)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
