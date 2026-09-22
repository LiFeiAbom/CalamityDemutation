using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 犽戎之子（SonYharonBuff，移植自 CalamityInheritance 的 Buffs/Summon/SonYharonBuff.cs）：
    /// 巨龙七星灯召唤的仆从在场时挂在主人头上的增益，本身不做事，只负责维持玩家的 <c>ownSonYharon</c> 标记
    /// （该标记由主类 ResetEffects 每帧复位，真正的作用是让 SonYharon 弹幕据此续命）。
    /// <para>
    /// 与 CI 原版的差异：CI 把标记存在它的 <c>CalamityInheritancePlayer</c>（字段定义在 CIPlayer/BoolBuff.cs 里，
    /// 那是一份"给各种 buff 用的布尔标记"汇总文件），本模组按各移植件各带一个 partial 的既有做法，
    /// 把 <c>ownSonYharon</c> 放在 <see cref="CalamityDemutationPlayer"/> 的 SonYharon partial 里。
    /// </para>
    /// </summary>
    internal class SonYharonBuff:ModBuff
    {
        /// <summary>不显示剩余时间（靠仆从在场维持）、不随存档保存</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        /// <summary>仆从在场就给主人置位并顶满时长；仆从没了且标记为假就自行撤掉这个 buff</summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SonYharon>()] > 0)
            {
                modPlayer.ownSonYharon = true;
            }
            if (!modPlayer.ownSonYharon)
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
