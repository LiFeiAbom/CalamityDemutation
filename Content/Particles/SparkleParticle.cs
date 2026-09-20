using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 星芒闪光（SparkleParticle，移植自 CalamityEntropy 的 PRT_SparkleCal，即灾厄 SparkleParticle 的搬运版）：
    /// 一颗四角星芒叠一层 bloom 光晕，透明度走一条正弦脉冲，逐帧减速并自旋，同时往周围洒光。
    /// 虚影薄锋（Voidshade）普通挥砍命中时在原地炸一颗。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>Opacity</c> 由基类提供，本粒子那份透明度是
    /// 每帧用 <c>LifetimeCompletion</c> 现算的私有量（不跨帧累积），故直接算在绘制里；
    /// ③ CE 的 <c>PRTDrawMode</c> 在唯一调用点恒为加法混合（<c>additiveBlend</c> 参数取默认的 true），
    /// 本粒子直接固定走 <see cref="UseAdditiveBlend"/>（同 <see cref="LineParticleCal"/> 的处理口径）；
    /// ④ <c>Configure</c> 由返回 this 的链式写法改为普通设置方法，调用点在 <c>DRKLoader.NewParticle</c> 之后；
    /// ⑤ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>；
    /// ⑥ 两张贴图按工程做法改读 <c>Assets/Particles/</c> 下同名图（CE 的 Sparkle2 / BloomCircle）。
    /// </para>
    /// </summary>
    internal class SparkleParticle:BaseParticle
    {
        /// <summary>bloom 光晕的颜色（与星芒本体的 <see cref="BaseParticle.Color"/> 可以不同，CE 传的是 Blue）</summary>
        public Color Bloom;
        /// <summary>自旋速度：每帧按速度方向决定正负加到旋转上</summary>
        public float Spin;
        /// <summary>bloom 层的额外缩放</summary>
        public float BloomScale = 1f;
        /// <summary>星芒贴图（CE 的 Assets/Particles/Sparkle2）</summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/Sparkle2";
        /// <summary>bloom 光晕贴图（CE 的 Assets/Particles/BloomCircle），与星芒尺寸不同，绘制时按高度比换算</summary>
        private const string BloomTexture = "CalamityDemutation/Assets/Particles/BloomCircle";
        /// <summary>加法混合（CE 在唯一调用点钉死的模式）</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>需要自绘（三层叠画与尺寸换算在 CustomDraw 里）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>
        /// 按 CE 的 Configure 语义设置参数（调用点在 <c>DRKLoader.NewParticle</c> 之后）：
        /// <paramref name="rotationSpeed"/> 为自旋速度、<paramref name="bloomScale"/> 为光晕层额外缩放。
        /// 初始旋转角取随机角（CE 原样）。
        /// </summary>
        public void Configure(Color bloom, int lifetime, float rotationSpeed = 0f, float bloomScale = 1f)
        {
            Bloom = bloom;
            Spin = rotationSpeed;
            BloomScale = bloomScale;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            if (lifetime > 0)
            {
                Lifetime = lifetime;
            }
        }
        /// <summary>生成时补上默认寿命 30 帧（灾厄原版同名粒子的默认）</summary>
        public override void SetDRK()
        {
            if (Lifetime <= 0)
            {
                Lifetime = 30;
            }
        }
        public override void AI()
        {
            // 正弦脉冲：亮度先升后降；速度方向决定自旋方向
            float opacity = (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
            Velocity *= 0.95f;
            Rotation += Spin * (Velocity.X > 0f ? 1f : -1f);
            Lighting.AddLight(Position, Bloom.R / 255f * opacity, Bloom.G / 255f * opacity, Bloom.B / 255f * opacity);
        }
        /// <summary>
        /// 自绘三层：bloom 光晕（按两图高度比缩回去）→ 转过 45° 的星芒衬底（0.75 倍）→ 正星芒本体。
        /// 三层都乘同一条正弦脉冲透明度。
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D starTexture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>(BloomTexture).Value;
            float opacity = (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
            float properBloomSize = starTexture.Height / (float)bloomTexture.Height;
            Vector2 drawPosition = Position - Main.screenPosition;
            spriteBatch.Draw(bloomTexture, drawPosition, null, Bloom * opacity * 0.5f, 0f, bloomTexture.Size() / 2f, Scale * BloomScale * properBloomSize, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, drawPosition, null, Color * opacity * 0.5f, Rotation + MathHelper.PiOver4, starTexture.Size() / 2f, Scale * 0.75f, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, drawPosition, null, Color * opacity, Rotation, starTexture.Size() / 2f, Scale, SpriteEffects.None, 0f);
        }
    }
}
