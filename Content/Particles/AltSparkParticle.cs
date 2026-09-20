using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 星屑火花 · 半透明版（AltSparkParticle，移植自 CalamityEntropy 的 PRT_AltSpark）：
    /// 一枚沿速度方向拉长的星屑，逐帧缩小、减速，颜色按三次方曲线淡出，可选受重力。
    /// 虚影薄锋（Voidshade）普通挥砍命中时撒的一圈火花里，一半走这粒、一半走
    /// <see cref="LineParticleCal"/>。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 把它落进 <c>AlphaBlend</c> 桶，本模组两个
    /// 混合开关都不打开即为默认的 AlphaBlend 桶，故不覆写；③ <c>Configure</c> 由返回 this 的链式写法
    /// 改为普通设置方法，调用点在 <c>DRKLoader.NewParticle</c> 之后（它要从已填好的 Color 快照 InitialColor）；
    /// ④ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>；
    /// ⑤ 贴图走工程既有的 <c>Assets/Particles/StarProj</c>（CE 取自它自己的 Assets/Particles/StarProj，同一张图）。
    /// </para>
    /// <para>
    /// 与工程既有的 <see cref="DRK_Spark"/> 是同源物（同图、AI 逐条一致、同样是双层 0.5/1.6 + 0.45 的叠法），
    /// **唯一差别是落桶**（本粒子走 AlphaBlend，DRK_Spark 走加法混合），故两者不可互替。
    /// </para>
    /// </summary>
    internal class AltSparkParticle:BaseParticle
    {
        /// <summary>初始颜色：颜色随时间从它往透明插值</summary>
        public Color InitialColor;
        /// <summary>是否受重力：为真且速度足够慢时横向阻尼并下坠</summary>
        public bool AffectedByGravity;
        /// <summary>贴图取工程既有的 StarProj（与 DRK_Spark 同一张）</summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/StarProj";
        /// <summary>需要自绘（双层叠加）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>按 CE 的 Configure 语义设置重力开关与寿命（调用点在 <c>NewParticle</c> 之后，便于快照颜色）</summary>
        public void Configure(bool affectedByGravity, int lifetime)
        {
            AffectedByGravity = affectedByGravity;
            InitialColor = Color;
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
            Scale *= 0.95f;
            // 随寿命进度按三次方曲线向透明插值，末段淡出更快（与 DRK_Spark 同款）
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            // 速度低于阈值且受重力：横向阻尼并持续下坠
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            // 贴图长轴默认朝上，故旋转需额外加 90°
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>自绘两层：外层横向压扁、纵向拉长，内层更细 — 合成细长星屑</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Vector2 drawScale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
        }
    }
}
