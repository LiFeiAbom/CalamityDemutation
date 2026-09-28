using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 自定义脉冲粒子（移植自灾厄的 CustomPulse）：带自定义贴图的扩散脉冲环，
    /// 随寿命按 PolyOut(4) 缓动由 OriginalScale 放大到 FinalScale，透明度按 sin 曲线衰减。
    /// 与 <see cref="DirectionalPulseRing"/> 的唯一区别是贴图由构造参数指定（而非固定成工程内的空心圆环）——
    /// 灾厄用它播「火焰爆炸」那张图，本工程对应 Particles/FlameExplosion。
    /// </summary>
    public class CustomPulse : Particle
    {
        // ── 实例字段 ──
        /// <summary>基准颜色（绘制时与透明度相乘得到实际颜色）</summary>
        private Color BaseColor;
        /// <summary>缩放终点（起点见 OriginalScale）</summary>
        private float FinalScale;
        /// <summary>本粒子要画的贴图路径（构造时传入，绘制期才请求资源）</summary>
        private string NewTexture;
        /// <summary>当前透明度（由 Update 按生存进度算出）</summary>
        private float opacity;
        /// <summary>缩放起点</summary>
        private float OriginalScale;
        /// <summary>横向/纵向压扁系数</summary>
        private Vector2 Squish;
        /// <summary>是否走加法混合（即构造参数 useAdditiveBlend 的落点，可在构造后改写）</summary>
        public bool UseAltVisual = true;
        // ── 属性 ──
        /// <summary>跟随 UseAltVisual</summary>
        public override bool UseAdditiveBlend => UseAltVisual;
        /// <summary>寿命到期自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>走 CustomDraw 自定义绘制</summary>
        public override bool UseCustomDraw => true;
        /// <summary>本体不画贴图（真正的贴图见 NewTexture；灾厄原码同样指向隐形贴图）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        // ── 构造函数 ──
        /// <summary>
        /// 构造脉冲：记录位置、速度、基准色、贴图路径、压扁系数、朝向与缩放起止值，初始缩放取 originalScale。
        /// </summary>
        public CustomPulse(Vector2 position, Vector2 velocity, Color color, string texture, Vector2 squish, float rotation, float originalScale, float finalScale, int lifeTime, bool useAdditiveBlend = true)
        {
            Position = position;
            Velocity = velocity;
            BaseColor = color;
            NewTexture = texture;
            OriginalScale = originalScale;
            FinalScale = finalScale;
            Scale = originalScale;
            Lifetime = lifeTime;
            Squish = squish;
            Rotation = rotation;
            UseAltVisual = useAdditiveBlend;
        }
        // ── 公开方法 ──
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
        /// <summary>
        /// 自定义绘制：以贴图中心为原点，按 Scale × Squish 缩放、按 Rotation 旋转绘制。
        /// 注意此处再乘一次 opacity（与灾厄原文一致）：Update 已把 opacity 乘进 Color，故实际是不透明度平方。
        /// 与灾厄原文的唯一偏离：这里用 AssetRequestMode.ImmediateLoad 取贴图——绘制期现请求若走默认的异步加载
        /// 会拿到空引用（本工程既有教训），且本工程其他绘制期取图处也都用 ImmediateLoad。请求结果按路径缓存，只在首帧开销。
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = ModContent.Request<Texture2D>(NewTexture, AssetRequestMode.ImmediateLoad).Value;
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * opacity, Rotation, tex.Size() / 2f, Scale * Squish, SpriteEffects.None, 0);
        }
    }
}
