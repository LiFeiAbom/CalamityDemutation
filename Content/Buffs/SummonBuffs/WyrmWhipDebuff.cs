using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 噬渊标记（WyrmWhipDebuff） - 噬渊鞭挞（Ystralyn）命中敌人时挂上的召唤 tag。
    /// 由鞭子命中挂在**敌人**身上，之后该敌人的**仆从弹幕**命中时额外吃到：平伤 +90、乘算 +15%、
    /// 以及 1/8（12.5%）的强制暴击——加成在 CalamityDemutationGlobalProjectile.ModifyHitNPC 里结算。
    /// <para>
    /// 与 CE 原版的差异：CE 把它写在 Content/Buffs/WhipDebuff.cs 里、与另外 11 个鞭痕 buff 挤同一文件，
    /// 且贴图统一指向那张通用鞭痕图；本模组按工程做法拆成独立文件、贴图即同目录同名 png。
    /// <c>Main.debuff</c> 未置位是 **CE 原样**（它只登记 IsATagBuff），照抄别"修"。
    /// </para>
    /// </summary>
    internal class WyrmWhipDebuff:ModBuff
    {
        /// <summary>平伤 tag 值：仆从弹幕命中时按该弹幕的 SummonTagDamageMultiplier 折算后加进 FlatBonusDamage</summary>
        public static readonly int TagDamage = 90;
        /// <summary>乘算 tag 值：同样按弹幕倍率折算后加进 SourceDamage（即 +15% 伤害）</summary>
        public static readonly float TagDamageMul = 0.15f;
        /// <summary>
        /// 登记为"鞭的召唤 tag"：原版与本工程的 tag 结算都靠这个标记识别，
        /// 同时让该 debuff 能挂到平时免疫一切减益的敌怪身上
        /// </summary>
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
}
