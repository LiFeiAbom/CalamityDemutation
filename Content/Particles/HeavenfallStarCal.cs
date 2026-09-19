using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 天降星屑 · 灾厄熵版（HeavenfallStarCal，移植自 CalamityEntropy 的 PRT_HeavenfallStar）：
    /// 一枚被拉成条状的白色星屑，逐帧缩小、减速，并随寿命按三次方曲线淡出；
    /// 朝向由生成时给定的角度固定（CE 传的是速度方向），绘制时额外转 90° 让长轴对上。
    /// <para>
    /// 与工程既有的 <see cref="DRK_HeavenfallStar"/> 是两个模组的同名物，**不可互相替代**：
    /// 那张是灾厄本体版（贴图 Assets/Particles/StarProj、每帧 ×0.95、默认挤压 (0.5, 1.6)）；
    /// 这张是 CE 版（贴图 StarTexture_White、每帧 ×0.92、默认挤压 (0.2, 1.6×xScale)）。
    /// 无星之夜的虚空新星与符文之歌的命中星屑用的都是这一张。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>Opacity</c>/<c>Glow</c>/<c>PRTDrawMode</c>
    /// 在本粒子上未被绘制读取（CE 只传 opacity=1、glow=true、恒 AdditiveBlend），不保留；
    /// ③ <c>Configure</c> 由返回 this 的链式写法改为普通设置方法，只留下调用点真正用到的角度与寿命；
    /// ④ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>；
    /// ⑤ <c>xScale</c> 照 CE 保留为公开字段，在 <c>Configure</c> 之后单独赋值（CE 也是这么用的）。
    /// </para>
    /// </summary>
    internal class HeavenfallStarCal:BaseParticle
    {
        /// <summary>初始颜色：颜色随时间从它往透明插值</summary>
        public Color InitialColor;
        /// <summary>竖向拉伸系数（CE 的 xScale）：与 Scale 相乘后作用在贴图长轴上，0.14 能把它压成近方形的小点</summary>
        public float xScale = 1f;
        /// <summary>贴图与类同名同目录（CE 的 Assets/Extra/StarTexture_White）</summary>
        public override string Texture => "CalamityDemutation/Content/Particles/HeavenfallStarCal";
        /// <summary>加法混合，使星屑发光</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>需要自绘（双层叠加，且旋转要多转 90°）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>按 CE 的 Configure 语义设置朝向与寿命（其余参数在参数填入 <c>NewParticle</c> 时已给定）</summary>
        public void Configure(float rotation, int lifetime)
        {
            Rotation = rotation;
            if (lifetime > 0)
            {
                Lifetime = lifetime;
            }
        }
        /// <summary>生成时快照当前颜色供淡出插值，并补上默认寿命 200 帧</summary>
        public override void SetDRK()
        {
            InitialColor = Color;
            if (Lifetime <= 0)
            {
                Lifetime = 200;
            }
        }
        public override void AI()
        {
            Scale *= 0.92f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.92f;
        }
        /// <summary>自绘：同一贴图叠两层（外层拉长、内层按 0.45 横向收窄），长轴再补 90°</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            Vector2 drawScale = new Vector2(0.2f, 1.6f * xScale) * Scale;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
        }
    }
}
