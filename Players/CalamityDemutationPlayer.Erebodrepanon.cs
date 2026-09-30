using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 泓渊亡铭（Erebodrepanon）「三次挥砍一轮」所需的玩家侧计数：
    /// 由 <see cref="Content.Items.Weapons.Melee.Erebodrepanon"/> 在出手时读写——读它决定这一刀是不是第三段，写完自增并按 3 取模。
    /// <para>
    /// 与 CE 原版的差异：CE 把这个计数放在 ModItem 的字段上（<c>Erebodrepanon.UseCount</c>），
    /// 而 ModItem 实例是全类型共享的——单机看不出问题，联机时两名玩家同拿一把镰刀会互相打乱段位。
    /// 挪到 ModPlayer 后每名玩家各有一份，本模组按"每个移植件各带一个 partial"的既有做法
    /// （同 <c>CalamityDemutationPlayer.SonYharon.cs</c>）单独放这里。
    /// </para>
    /// <para>
    /// 不需要额外的网络同步：段位只由出手的客户端此刻决定，而且决定的是弹幕的 <c>ai[0]</c> 与伤害——
    /// 两者都随弹幕的生成包一起发出去，别的端拿到的就是已经定好段位的镰刀。
    /// 跨世界/重生也不清空（与 CE 的共享字段行为一致：段位循环一直延续）。
    /// </para>
    /// </summary>
    internal partial class CalamityDemutationPlayer:ModPlayer
    {
        /// <summary>泓渊亡铭当前是第几段（0/1/2，第三段伤害翻倍），每次出手后自增并按 3 取模</summary>
        public int erebodrepanonUseCount = 0;
    }
}
