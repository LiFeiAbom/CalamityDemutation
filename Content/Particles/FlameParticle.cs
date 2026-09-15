using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 火焰粒子（FlameParticle） - 移植自灾厄本体的 CalamityMod.Particles.FlameParticle，
    /// 由 hellfireExplosion 减益（Content/Buffs/NegativeBuffs/HellfireExplosion.cs）挂在实体上喷出。
    /// 贴图朝上，随寿命由亮色渐变到暗色、中段泛白并整体衰减，同时持续上浮，形成摇曳的火苗。
    /// </summary>
    internal class FlameParticle : BaseParticle
    {
        // ── 实例字段 ──
        /// <summary>
        /// 亮色端：寿命起点（LifetimeCompletion = 0）时的颜色
        /// </summary>
        public Color BrightColor;
        /// <summary>
        /// 暗色端：寿命终点（LifetimeCompletion = 1）时的颜色
        /// </summary>
        public Color DarkColor;
        /// <summary>
        /// 相对强度：驱动每帧上浮距离与缩放增长，越大飘得越高、膨胀越快
        /// </summary>
        public float RelativePower;
        // ── 属性 ──
        /// <summary>
        /// 贴图路径：Assets/Particles/Flames
        /// </summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/Flames";
        /// <summary>
        /// 使用加法混合，使火苗呈现发光感
        /// </summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>
        /// 到达 Lifetime 后自动移除
        /// </summary>
        public override bool SetLifetime => true;
        // ── 构造函数 ──
        /// <summary>
        /// 构造粒子：给定位置、寿命、缩放、相对强度与亮/暗两端颜色；水平初速取 [-1, 1] 的随机值
        /// </summary>
        public FlameParticle(Vector2 position, int lifetime, float scale, float relativePower, Color brightColor, Color darkColor)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Velocity.X = Main.rand.NextFloat(1f, -1f);
            Variant = Main.rand.Next(3);   // 灾厄原样保留；本贴图为单帧（FrameVariants 恒为 1），该值不参与绘制
            Scale = scale;
            Lifetime = lifetime;
            RelativePower = relativePower;
            BrightColor = brightColor;
            DarkColor = darkColor;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 每帧更新：按相对强度上浮并放大、再逐步收缩；
        /// 颜色先由亮到暗，中段叠白提亮，最后整体乘一次首尾淡入淡出
        /// </summary>
        public override void AI()
        {
            Scale += RelativePower * 0.01f;
            Position.Y -= RelativePower * 1.25f;
            Scale *= 0.97f;
            Color = Color.Lerp(BrightColor, DarkColor, LifetimeCompletion);
            Color = Color.Lerp(Color, Color.White, Utils.GetLerpValue(0.1f, 0.25f, LifetimeCompletion, true) * Utils.GetLerpValue(0.4f, 0.25f, LifetimeCompletion, true) * 0.7f);
            Color *= Utils.GetLerpValue(0f, 0.15f, LifetimeCompletion, true) * Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true) * 0.6f;
            Color *= 1.5f;
            Color.A = 50;
        }
    }
}
