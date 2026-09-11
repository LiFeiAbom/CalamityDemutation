namespace CalamityDemutation
{
    /// <summary>
    /// 本模组资源路径常量与通用常量
    /// </summary>
    public static class CalamityDemutationConstant
    {
        /// <summary>
        /// 颜色条贴图完整路径前缀（配 ModContent.Request 使用）
        /// </summary>
        public const string ColorBar = "CalamityDemutation/Assets/ColorBar/";
        /// <summary>
        /// 遮罩贴图完整路径前缀（配 ModContent.Request 使用）
        /// </summary>
        public const string Masking = "CalamityDemutation/Assets/Masking/";
        /// <summary>
        /// 粒子上限（DRK 粒子系统使用）
        /// </summary>
        public const int MaxParticleCount = 10000;
        /// <summary>
        /// Effect 资源相对路径（配 Mod.Assets.Request 使用，不含模组名前缀）
        /// </summary>
        public const string noEffects = "Effects/";
    }
}
