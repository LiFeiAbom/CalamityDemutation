using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 通用闪光粒子（移植自灾厄 Particles.GenericSparkle）：一颗随时间旋转、自带光晕底的星形闪光。
    /// 亮度按寿命取正弦（出生与消亡时最暗、中段最亮），并每帧按自身颜色投光；速度每帧乘 0.95 逐渐停下。
    /// 自绘时先铺一层按贴图比例缩放的 BloomCircle 光晕，再叠两层 Sparkle 贴图（一层转 45°、一层不转）。
    /// </summary>
    internal class GenericSparkle : Particle
    {
        /// <summary>使用 Particles/Sparkle 贴图</summary>
        public override string Texture => "CalamityDemutation/Particles/Sparkle";
        /// <summary>加法混合，呈现发光感</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>走 CustomDraw 自定义绘制</summary>
        public override bool UseCustomDraw => true;
        /// <summary>寿命到期由 GeneralParticleHandler 自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>是否标记为重要粒子（重要粒子在达到数量上限时仍会被加入）</summary>
        public override bool Important => important;
        /// <summary>重要粒子标记</summary>
        public bool important;
        /// <summary>每帧自转速度</summary>
        private float Spin;
        /// <summary>当前亮度（按寿命取正弦）</summary>
        private float opacity;
        /// <summary>光晕底颜色</summary>
        private Color Bloom;
        /// <summary>投光用的颜色（光晕色乘当前亮度）</summary>
        private Color LightColor => Bloom * opacity;
        /// <summary>光晕底的缩放倍率</summary>
        private float BloomScale;
        /// <summary>
        /// 构造闪光粒子：写入位置/速度/本体色/光晕色/缩放/寿命，初始旋转取随机角，
        /// 并可选指定自转速度、光晕缩放与"重要粒子"标记
        /// </summary>
        public GenericSparkle(Vector2 position, Vector2 velocity, Color color, Color bloom, float scale, int lifeTime, float rotationSpeed = 1f, float bloomScale = 1f, bool needed = false)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Bloom = bloom;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            BloomScale = bloomScale;
            important = needed;
        }
        /// <summary>每帧：按寿命算亮度并投光，速度衰减停住，并按水平速度方向决定自转正负</summary>
        public override void Update()
        {
            opacity = (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
            Lighting.AddLight(Position, LightColor.R / 255f, LightColor.G / 255f, LightColor.B / 255f);
            Velocity *= 0.95f;
            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);
        }
        /// <summary>自绘：光晕底（按星形贴图与光晕贴图的高度比换算尺寸）＋ 两层星形贴图（一层转 45°）</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D starTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityDemutation/Particles/BloomCircle").Value;
            float properBloomSize = (float)starTexture.Height / (float)bloomTexture.Height;
            spriteBatch.Draw(bloomTexture, Position - Main.screenPosition, null, Bloom * opacity * 0.5f, 0, bloomTexture.Size() / 2f, Scale * BloomScale * properBloomSize, SpriteEffects.None, 0);
            spriteBatch.Draw(starTexture, Position - Main.screenPosition, null, Color * opacity * 0.5f, Rotation + MathHelper.PiOver4, starTexture.Size() / 2f, Scale * 0.75f, SpriteEffects.None, 0);
            spriteBatch.Draw(starTexture, Position - Main.screenPosition, null, Color * opacity, Rotation, starTexture.Size() / 2f, Scale, SpriteEffects.None, 0);
        }
    }
}
