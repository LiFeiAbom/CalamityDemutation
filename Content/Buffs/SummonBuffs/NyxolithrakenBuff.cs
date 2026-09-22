using CalamityDemutation.Content.Projectiles.Summon;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 沧溟渊龙（NyxolithrakenBuff，移植自 CalamityEntropy 的 Content/Buffs/NyxolithrakenBuff.cs）：
    /// 沧溟龙契召唤的仆从在场时的维系 buff，本身不做事——只按"场上还有没有这条龙"决定把自己顶满还是撤掉，
    /// 龙那边则据"主人身上还有没有这个 buff"来决定是否续命（两边互为条件）。
    /// </summary>
    internal class NyxolithrakenBuff:ModBuff
    {
        /// <summary>不显示剩余时间、不随存档保存（CE 原样）</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<NyxolithrakenDragon>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
