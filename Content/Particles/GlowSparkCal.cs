using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 灾厄版发光火花（GlowSparkCal，移植自 CalamityEntropy 的 PRT_GlowSparkCal，即灾厄 GlowSparkParticle 的搬运版）：
    /// 与本模组的 <see cref="GlowSpark"/> 是近亲，区别是多了初始颜色 / 挤压（Squash）/ 重力开关 / 收缩速度等可配置项，
    /// 贴图用的是灾厄那张 GlowSpark。星熠分形与分形之星命中时都会撒一片。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>Opacity</c> 由基类提供且本粒子只写不读，
    /// 本模组基类没有该字段，直接省掉；③ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）、<c>ShouldKillWhenOffScreen</c>
    /// （本模组粒子系统不淘汰出屏粒子）与 <c>InGame_World_MaxCount</c>（本模组有全局粒子上限）；
    /// ④ 贴图改为与类同名的 Content/Particles/GlowSparkCal.png（CE 取自它自己的 Assets/Particles/GlowSpark）；
    /// ⑤ 天顶世界周二彩蛋（把火花换成猛犸象贴图）照搬，贴图为 Content/Particles/MammothParticle.png；
    /// ⑥ <c>PRTDrawMode</c> 三态混合简化：本粒子恒为加法混合，直接用 <see cref="UseAdditiveBlend"/>；
    /// ⑦ <c>Configure</c> 由返回 this 的链式写法改为普通设置方法，调用点在 <c>DRKLoader.NewParticle</c> 之后调它
    /// （它要从已填好的 Velocity/Color 派生 Rotation 与 InitialColor）。
    /// </para>
    /// </summary>
    internal class GlowSparkCal:BaseParticle
    {
        /// <summary>初始颜色：透明度与颜色都从它往透明渐变</summary>
        public Color InitialColor;
        /// <summary>是否受重力：为真且速度足够慢时下坠</summary>
        public bool AffectedByGravity;
        /// <summary>是否快速收缩（逐帧拉扁 Squash）</summary>
        public bool QuickShrink;
        /// <summary>是否额外叠一层提亮的芯</summary>
        public bool Glowing = true;
        /// <summary>收缩速度倍率（QuickShrink 为真时生效，1 走原版那套固定比例）</summary>
        public float ShrinkSpeed = 1f;
        /// <summary>XY 方向的挤压比例</summary>
        public Vector2 Squash = new Vector2(0.5f, 1.6f);
        /// <summary>贴图与类同名同目录</summary>
        public override string Texture => "CalamityDemutation/Content/Particles/GlowSparkCal";
        /// <summary>加法混合，呈现发光感</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>需要自绘（颜色渐变与两层叠加都在 CustomDraw 里）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>
        /// 按 CE 的 Configure 语义设置参数：注意必须在 <c>DRKLoader.NewParticle</c> 之后调用，
        /// 因为它要从已经填好的 Velocity 与 Color 派生旋转与初始颜色。
        /// </summary>
        public void Configure(bool affectedByGravity, int lifetime, Vector2 squash, bool quickShrink = false, bool glow = true, float shrinkSpeed = 1f)
        {
            AffectedByGravity = affectedByGravity;
            Squash = squash;
            QuickShrink = quickShrink;
            Glowing = glow;
            ShrinkSpeed = shrinkSpeed;
            InitialColor = Color;
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
            if (lifetime > 0)
            {
                Lifetime = lifetime;
            }
        }
        /// <summary>生成时补默认寿命 30 帧</summary>
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
            if (QuickShrink)
            {
                // 收缩速度不为 1 时按倍率改 XY，否则走原版那套固定比例
                if (ShrinkSpeed == 1f)
                {
                    Squash.X *= 0.8f;
                    Squash.Y *= 1.2f;
                }
                else
                {
                    Squash.X *= 1f - 0.2f * ShrinkSpeed;
                    Squash.Y *= 1f + 0.2f * ShrinkSpeed;
                }
            }
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>自绘：先画挤压后的本体颜色，需要发光时再叠一层提亮的芯（天顶世界周二替换成猛犸象彩蛋贴图）</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Vector2 drawScale = Squash * Scale;
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            float scaleMult = 1f;
            if (Main.zenithWorld)
            {
                DateTime day = DateTime.Now;
                if (day.DayOfWeek == DayOfWeek.Tuesday)
                {
                    // 天顶世界周二的猛犸象彩蛋（灾厄原版彩蛋，照搬，不是 bug）
                    Texture2D joke = ModContent.Request<Texture2D>("CalamityDemutation/Content/Particles/MammothParticle").Value;
                    scaleMult = MathHelper.Lerp(texture.Size().X / joke.Size().X, texture.Size().Y / joke.Size().Y, 0.5f);
                    texture = joke;
                }
            }
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale * scaleMult, SpriteEffects.None, 0f);
            if (Glowing)
            {
                spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.Lerp(Color.White, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D)), Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f) * scaleMult, SpriteEffects.None, 0f);
            }
        }
    }
}
