using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 红魔（RedDevil） - 召唤物关联型增益，仅在对应召唤物存活时维持
    /// 与 Content/Projectiles/Summon/RedDevil.cs 配合：有存活弹幕则持续续时，
    /// 否则立即移除增益；增益是否有效由 ModPlayer 的 redDevil/redDevil2 标记决定。
    /// </summary>
    internal class RedDevil:ModBuff
    {
        /// <summary>
        /// 召唤物关联增益：不显示倒计时、不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 不显示剩余时间：该增益是召唤物存在标记，靠每帧续满维持而非计时
            Main.buffNoTimeDisplay[Type] = true;
            // 不随存档保存：退出游戏后召唤物不会保留
            Main.buffNoSave[Type] = true;
        }
        /// <summary>
        /// 每帧校验召唤物是否存活：存在则置位 redDevil2 并把剩余时间顶回 18000，否则移除该增益
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 场上存在己方红魔弹幕时置位 redDevil2，作为"本次增益合法"的凭据
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.RedDevil>()] > 0)
            {
                modPlayer.redDevil2 = true;
            }
            // 无存活召唤物：移除增益并回退索引（DelBuff 会打乱 buff 数组）
            if (!modPlayer.redDevil2)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // 有召唤物：每帧把剩余时间顶回 18000，实现常驻
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
