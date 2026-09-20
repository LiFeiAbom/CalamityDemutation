using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 闪光（ShineParticle，移植自 CalamityEntropy 的 PRT_ShineParticle）：命中处一闪而过的光晕，
    /// 透明度走一条余弦脉冲曲线（前段渐亮、尾段渐暗）。
    /// 最终分形的每一处命中都会炸一颗（<c>Configure(1, true, AdditiveBlend, 0, 12)</c>）。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>PRTDrawMode</c> 三态混合保留为
    /// <see cref="DrawMode"/> 字段，并映射到基类的 <see cref="UseAdditiveBlend"/>/<see cref="UseHalfTransparency"/>
    /// 两个批次开关；③ CE 的 <c>Opacity</c> 由基类提供，本模组基类没有该字段，故自持一份；
    /// ④ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>（本模组粒子系统不淘汰出屏粒子），
    /// 随之去掉 CE 放在 <c>SetProperty</c>/<c>Reset</c> 里那几行「池化复用专用」的重置（字段初始化器已覆盖）；
    /// ⑤ CE 绘制时读共享的 <c>PRTExtraTextures.Glow2</c>，本模组直接读粒子注册表里该类的贴图（同一张图）；
    /// ⑥ <c>CEUtils.randomPointInCircle</c> 内联为 <see cref="RandomPointInCircle"/>；
    /// ⑦ **删掉 CE 的 <c>FollowOwner</c> 跟随实体分支**（连同 <c>ownerLastPos</c>）：本模组唯一调用方不设置它，
    /// 全工程没有任何赋值处（编译器 CS0649 也会据此报「从未赋值」），属不产生效果的死字段。
    /// </para>
    /// </summary>
    internal class ShineParticle:BaseParticle
    {
        /// <summary>CE 的 <c>PRTDrawModeEnum</c>（顺序与取值都对齐：AlphaBlend / NonPremultiplied / AdditiveBlend）</summary>
        public enum DrawModeEnum
        {
            AlphaBlend,
            NonPremultiplied,
            AdditiveBlend,
        }
        /// <summary>混合模式（除批次外还决定绘制时是全色相乘还是只乘 A）</summary>
        public DrawModeEnum DrawMode = DrawModeEnum.AlphaBlend;
        /// <summary>整体透明度（CE 基类的 Opacity），每帧由 AI 按余弦脉冲重算</summary>
        public float Opacity = 1f;
        /// <summary>是否发光：为假时吃地图光照（CE 的 Glow）</summary>
        public bool Glow = true;
        /// <summary>CE 的另一套淡出手感开关：为真时前段缩放回弹、尾段喷尘。本模组当前无调用方置真</summary>
        public bool flag = false;
        /// <summary>flag 分支下记下的初始缩放</summary>
        public float orgScale = -1f;
        /// <summary>绘制时的额外缩放（CE 的 drawScale）</summary>
        public Vector2 drawScale = Vector2.One;
        /// <summary>贴图（CE 的 Assets/Extra/Glow2）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/Glow2";
        /// <summary>加法混合（调用方指定时）</summary>
        public override bool UseAdditiveBlend => DrawMode == DrawModeEnum.AdditiveBlend;
        /// <summary>半透明非预乘混合（调用方指定时）</summary>
        public override bool UseHalfTransparency => DrawMode == DrawModeEnum.NonPremultiplied;
        /// <summary>需要自绘（光照与透明度处理在 CustomDraw 里）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>按 CE 的 Configure 语义设置参数（调用点在 <c>DRKLoader.NewParticle</c> 之后）</summary>
        public void Configure(float opacity, bool glow, DrawModeEnum mode, float rotation = 0f, int lifetime = -1)
        {
            Opacity = opacity;
            Glow = glow;
            DrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
            {
                Lifetime = lifetime;
            }
        }
        /// <summary>生成时补上默认寿命 200 帧（CE 原默认）</summary>
        public override void SetDRK()
        {
            if (Lifetime <= 0)
            {
                Lifetime = 200;
            }
        }
        public override void AI()
        {
            // 余弦脉冲：剩余寿命从 1 走到 0，透明度沿 cos 曲线先升后降
            float remaining = 1f - LifetimeCompletion;
            Opacity = (float)Math.Cos(remaining * MathHelper.Pi - MathHelper.PiOver2);
            if (flag)
            {
                // flag 分支（CE 原样）：前段由剩余帧数推透明度、缩放跟着一起回弹，>32 帧后开始按间隔喷镜尘
                float remTicks = Lifetime - Time;
                if (remTicks > 20)
                {
                    Opacity = 1 - (remTicks - 20f) / (Lifetime - 20f);
                }
                else
                {
                    Opacity = remTicks / 20f;
                }
                if (orgScale < 0)
                {
                    orgScale = Scale;
                }
                Scale = orgScale * Opacity;
                Opacity = 1;
                if (Time > 32)
                {
                    for (int i = 0; i < (Time - 32) / 16; i++)
                    {
                        Main.dust[Dust.NewDust(Position, 0, 0, DustID.MagicMirror)].velocity = RandomPointInCircle(Scale * 3);
                    }
                }
            }
        }
        /// <summary>自绘：不发光时先取地块光照，再按混合模式处理透明度（非预乘只乘 A），最后画在粒子中心</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Color clr = Color;
            if (!Glow)
            {
                clr = Lighting.GetColor((int)(Position.X / 16f), (int)(Position.Y / 16f), clr);
            }
            if (DrawMode == DrawModeEnum.NonPremultiplied)
            {
                clr.A = (byte)(clr.A * Opacity);
            }
            else
            {
                clr *= Opacity;
            }
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, clr, Rotation, texture.Size() / 2f, Scale * drawScale, SpriteEffects.None, 0f);
        }
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
}
