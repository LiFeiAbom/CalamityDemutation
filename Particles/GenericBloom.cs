using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 通用光晕粒子（移植自灾厄的 GenericBloom）：随寿命淡入淡出的柔光。
    /// </summary>
    public class GenericBloom : Particle
    {
        // ── 实例字段 ──
        /// <summary>是否向世界光照系统添加光源</summary>
        private bool ProduceLight;
        /// <summary>是否使用加法混合；写入 UseAltVisual，同时决定 UseAdditiveBlend</summary>
        public bool UseAltVisual = true;
        /// <summary>基准色（不受淡出影响，绘制时再乘 opacity）</summary>
        private Color BaseColor;
        /// <summary>当前透明度（由 Update 按生存进度算出，0→1→0）</summary>
        private float opacity;
        // ── 属性 ──
        /// <summary>寿命到期由 GeneralParticleHandler 自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>使用 Particles/Light 贴图</summary>
        public override string Texture => "CalamityDemutation/Particles/Light";
        /// <summary>跟随 UseAltVisual：加法混合开关</summary>
        public override bool UseAdditiveBlend => UseAltVisual;
        /// <summary>走 CustomDraw 自定义绘制</summary>
        public override bool UseCustomDraw => true;
        // ── 构造函数 ──
        /// <summary>
        /// 构造光晕粒子：写入位置、速度、基准色、缩放与寿命，并随机初始旋转。
        /// </summary>
        /// <param name="position">世界坐标起点</param>
        /// <param name="velocity">每帧叠加到位置的速度</param>
        /// <param name="color">基准颜色（Update 中与透明度相乘得到实际绘制色）</param>
        /// <param name="scale">绘制缩放</param>
        /// <param name="lifeTime">寿命帧数，到期后由 GeneralParticleHandler 自动移除</param>
        /// <param name="produceLight">是否调用 Lighting.AddLight 发光（默认开启）</param>
        /// <param name="AddativeBlend">是否使用加法混合，写入 UseAltVisual 并决定 UseAdditiveBlend（默认开启）</param>
        public GenericBloom(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime, bool produceLight = true, bool AddativeBlend = true)
        {
            Position = position;
            Velocity = velocity;
            BaseColor = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            ProduceLight = produceLight;
            UseAltVisual = AddativeBlend;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 自定义绘制（UseCustomDraw=true 时由绘制层系统调用）：在屏幕坐标处画 Texture 贴图，
        /// 颜色再乘一次 opacity 确保视觉淡出，原点是贴图中心，旋转与缩放取粒子字段
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * opacity, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
        }
        /// <summary>
        /// 每帧由 GeneralParticleHandler 调用：用 sin(生存进度·π) 得到 0→1→0 的淡入淡出系数，
        /// 颜色 = 基准色 × 该系数；ProduceLight 为真时按当前颜色向光照系统加光源；最后对速度做 0.95 阻尼减速
        /// </summary>
        public override void Update()
        {
            opacity = (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
            Color = BaseColor * opacity;
            if (ProduceLight)
            {
                Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            }
            Velocity *= 0.95f;
        }
    }
}
