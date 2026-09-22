using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 巨龙七星灯召唤的「犽戎之子」（SonYharon）所需的玩家侧标记，由
    /// <see cref="Content.Buffs.SummonBuffs.SonYharonBuff"/> 每帧置位、主类 ResetEffects 每帧复位，
    /// SonYharon 弹幕据此把自身 timeLeft 顶住（Buff 撤销后弹幕随即自然消亡）。
    /// <para>
    /// 与 CI 的对应：CI 把该标记叫 <c>OwnSonYharon</c> 并定义在它的 CIPlayer/BoolBuff.cs（一份给各种 buff 用的布尔标记汇总文件）里；
    /// 本模组按"每个移植件各带一个 partial"的既有做法（同 <c>CalamityDemutationPlayer.WyrmPhantom.cs</c>）单独放这里。
    /// </para>
    /// </summary>
    internal partial class CalamityDemutationPlayer:ModPlayer
    {
        /// <summary>犽戎之子是否在场（由 SonYharonBuff 每帧置位，主类 ResetEffects 每帧复位）</summary>
        public bool ownSonYharon = false;
    }
}
