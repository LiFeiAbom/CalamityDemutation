using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 细节爆炸粒子（移植自灾厄的 DetailedExplosion）：庇护系冲刺的撞击爆炸（阿斯加德之庇护的洋红爆炸、极乐之庇护的双层橙/灰爆炸）。
    /// 空心爆破贴图按 Squish 压扁并朝向 Rotation，随寿命按 PolyOut(4) 缓动由 OriginalScale 放大到 FinalScale，
    /// 同时透明度按 sin 曲线衰减。灾厄另有一个 UseAltVisual 开关同时控制混合模式，本工程直接由构造参数控制。
    /// </summary>
    public class DetailedExplosion : Particle
    {
        // ── 实例字段 ──
        /// <summary>基准颜色（绘制时与透明度相乘得到实际颜色）</summary>
        private Color BaseColor;
        /// <summary>缩放终点（起点见 OriginalScale）</summary>
        private float FinalScale;
        /// <summary>当前透明度（由 Update 按生存进度算出）</summary>
        private float opacity;
        /// <summary>缩放起点</summary>
        private float OriginalScale;
        /// <summary>横向/纵向压扁系数</summary>
        private Vector2 Squish;
        /// <summary>是否使用加法混合（对应灾厄原文的 UseAltVisual）</summary>
        private bool altVisual;
        // ── 属性 ──
        /// <summary>寿命到期自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>贴图使用 Assets/Particles/DetailedExplosion</summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/DetailedExplosion";
        /// <summary>混合模式由构造参数决定</summary>
        public override bool UseAdditiveBlend => altVisual;
        /// <summary>走 CustomDraw 自定义绘制</summary>
        public override bool UseCustomDraw => true;
        // ── 构造函数 ──
        /// <summary>
        /// 构造爆炸粒子：记录位置、速度、基准色、压扁系数、朝向与缩放起止值，初始缩放取 originalScale。
        /// </summary>
        public DetailedExplosion(Vector2 position, Vector2 velocity, Color color, Vector2 squish, float rotation, float originalScale, float finalScale, int lifeTime, bool additiveBlend = true)
        {
            Position = position;
            Velocity = velocity;
            BaseColor = color;
            OriginalScale = originalScale;
            FinalScale = finalScale;
            Scale = originalScale;
            Lifetime = lifeTime;
            Squish = squish;
            Rotation = rotation;
            altVisual = additiveBlend;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 自定义绘制：以贴图中心为原点，按 Scale × Squish 缩放、按 Rotation 旋转绘制。
        /// 注意此处再乘一次 opacity（与灾厄原文一致）：Update 已把 opacity 乘进 Color，故实际是不透明度平方。
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * opacity, Rotation, tex.Size() / 2f, Scale * Squish, SpriteEffects.None, 0);
        }
        /// <summary>
        /// 每帧更新：按 PolyOut(4) 缓动在 OriginalScale → FinalScale 之间插值放大，
        /// 透明度按 sin(π/2 + 生存进度·π/2) 衰减并乘进颜色，随后把当前颜色加到世界光照上；速度每帧 ×0.95。
        /// </summary>
        public override void Update()
        {
            // 灾厄原文是 PiecewiseAnimation(LifetimeCompletion, new CurveSegment(PolyOut, 0, 0, 1, 4))，
            // 单段曲线等价于 PolyOutEasing(progress, 4) = 1 - (1 - progress)^4
            float pulseProgress = 1f - (float)Math.Pow(1f - LifetimeCompletion, 4f);
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);
            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);
            Color = BaseColor * opacity;
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }
    }
}
