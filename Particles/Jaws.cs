using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 龙颚粒子（移植自灾厄的 Jaws）：弑神者冲刺前 20 帧贴在身后的一对"龙口"光片。
    /// 寿命极短（默认 2 帧），随寿命按 PolyOut(4) 缓动放大、透明度按 sin 曲线衰减，
    /// 绘制时横向压扁、纵向拉长（Squish）并朝向速度方向。
    /// </summary>
    public class Jaws : Particle
    {
        // ── 实例字段 ──
        /// <summary>基准颜色（绘制时与透明度相乘得到实际颜色）</summary>
        private Color BaseColor;
        /// <summary>缩放终点（起点见 OriginalScale）</summary>
        private float FinalScale;
        /// <summary>缩放起点</summary>
        private float OriginalScale;
        /// <summary>横向/纵向压扁系数</summary>
        private Vector2 Squish;
        // ── 属性 ──
        /// <summary>寿命到期自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>贴图使用 Assets/Particles/Jaws</summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/Jaws";
        /// <summary>使用加法混合</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>走 CustomDraw 自定义绘制</summary>
        public override bool UseCustomDraw => true;
        // ── 构造函数 ──
        /// <summary>
        /// 构造龙颚粒子：记录位置、速度、基准色、缩放起止值、压扁系数与朝向，初始缩放取 originalScale。
        /// </summary>
        public Jaws(Vector2 position, Vector2 velocity, Color color, Vector2 squish, float rotation, float originalScale, float finalScale, int lifeTime)
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
        }
        // ── 公开方法 ──
        /// <summary>
        /// 自定义绘制：以贴图中心为原点，按 Scale × Squish 缩放、按 Rotation 旋转绘制。
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color, Rotation, tex.Size() / 2f, Scale * Squish, SpriteEffects.None, 0);
        }
        /// <summary>
        /// 每帧更新：按 PolyOut(4) 缓动在 OriginalScale → FinalScale 之间插值放大，
        /// 透明度按 sin(π/2 + 生存进度·π/2) 衰减，并把当前颜色加到世界光照上；速度每帧 ×0.95。
        /// </summary>
        public override void Update()
        {
            // 灾厄原文是 PiecewiseAnimation(LifetimeCompletion, new CurveSegment(PolyOut, 0, 0, 1, 4))，
            // 单段曲线等价于 PolyOutEasing(progress, 4) = 1 - (1 - progress)^4
            float pulseProgress = 1f - (float)Math.Pow(1f - LifetimeCompletion, 4f);
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);
            float opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);
            Color = BaseColor * opacity;
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }
    }
}
