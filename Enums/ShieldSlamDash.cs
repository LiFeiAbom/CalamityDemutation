namespace CalamityDemutation.Enums
{
    /// <summary>
    /// 盾牌冲刺的种类，对应灾厄 2.0.3.9 的 CalPlayer/Dashes 下四个 CollisionType=ShieldSlam 的 PlayerDashEffect。
    /// None 表示当前没有任何饰品/套装提供冲刺；其余四项按合成链由下位到上位排列。
    /// </summary>
    public enum ShieldSlamDash
    {
        /// <summary>无盾牌冲刺</summary>
        None = 0,
        /// <summary>华丽盾（OrnateShieldDash）</summary>
        OrnateShield,
        /// <summary>阿斯加德之英勇（AsgardsValorDash）</summary>
        AsgardsValor,
        /// <summary>极乐之庇护（ElysianAegisDash）</summary>
        ElysianAegis,
        /// <summary>阿斯加德之庇护（AsgardianAegisDash）</summary>
        AsgardianAegis
    }
}
