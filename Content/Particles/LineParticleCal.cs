using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 细线粒子 · 灾厄熵版（LineParticleCal，移植自 CalamityEntropy 的 PRT_LineCal）：
    /// 一条沿速度方向拉长的发光细线，逐帧缩小、减速，并随寿命按三次方曲线淡出；可选受重力。
    /// <para>
    /// 与工程既有的 <see cref="DRK_Spark"/> 是同源物（两者都是灾厄 LineParticle 的搬运版，AI 逐条一致），
    /// 差别只在贴图：本粒子用 DrainLineBloom（细长渐隐条，CE 的选择），DRK_Spark 用 StarProj（星屑）。
    /// 符文脉冲束命中时撒的火花用的是这一张。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>PRTDrawMode</c> 在调用点恒为 AdditiveBlend，
    /// 本粒子直接固定走 <see cref="UseAdditiveBlend"/>；③ <c>Configure</c> 由返回 this 的链式写法改为普通设置方法，
    /// 调用点在 <c>DRKLoader.NewParticle</c> 之后（它要从已填好的 Color 快照 InitialColor）；
    /// ④ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>。
    /// </para>
    /// </summary>
    internal class LineParticleCal:BaseParticle
    {
        /// <summary>初始颜色：颜色随时间从它往透明插值</summary>
        public Color InitialColor;
        /// <summary>是否受重力：为真且速度足够慢时横向阻尼并下坠</summary>
        public bool AffectedByGravity;
        /// <summary>贴图取灾厄的 DrainLineBloom（细长渐隐条），与 ManaDrainStreak 共用一张</summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/DrainLineBloom";
        /// <summary>加法混合，使细线发光</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>需要自绘（双层叠加）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>按 CE 的 Configure 语义设置重力开关与寿命（调用点在 NewParticle 之后，便于快照颜色）</summary>
        public void Configure(bool affectedByGravity, int lifetime)
        {
            AffectedByGravity = affectedByGravity;
            InitialColor = Color;
            if (lifetime > 0)
            {
                Lifetime = lifetime;
            }
        }
        /// <summary>生成时补上默认寿命 30 帧（灾厄 LineParticle 的原默认）</summary>
        public override void SetDRK()
        {
            if (Lifetime <= 0)
            {
                Lifetime = 30;
            }
        }
        public override void AI()
        {
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>自绘：同一贴图叠两层（外层 0.5×1.6、内层横向再收 0.45）</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            Vector2 drawScale = new Vector2(0.5f, 1.6f) * Scale;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
        }
    }
}
