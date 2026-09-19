using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 星轨粒子（StarTrailParticle，移植自 CalamityEntropy 的 PRT_StarTrailParticle）：每帧把自身位置插到轨迹表头，
    /// 绘制时先画一颗头辉，再沿轨迹拉两层带子（外层 8px 原色、内层 3px 提亮 1.6 倍做芯），
    /// 带子用 Streak1w 条纹贴图沿 U 方向滚动。分形之星的分形渊星用它拖星轨。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>；② CE 的 <c>Opacity</c> 与 <c>Glow</c>
    /// 在本粒子里都只写不读（PreDraw 只用 <c>Color</c> 与 fadeOut 算淡出），一并省掉，<c>Configure</c> 也不再收这两个参数；
    /// ③ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>（本模组不淘汰出屏粒子）；
    /// ④ <c>PRTDrawModeEnum</c> 三态混合简化为 <see cref="UseAdditiveBlend"/>/<see cref="UseHalfTransparency"/> 两个 bool，
    /// 由 <c>Configure(additiveBlend: …)</c> 决定；⑤ 顶点结构从 InnoVault 的 <c>ColoredVertex</c> 换成本模组同源的
    /// <see cref="VertexPosition2DColorTexture"/>（它把 Vector3 纹理坐标拆成 Vector2 + widthCorrectionFactor）；
    /// ⑥ 贴图为 Content/Particles/StarTrailParticle.png，带子贴图放 Assets/ExtraTextures/Streak1w.png（CE 取自
    /// 它自己的 Assets/Extra/Streak1w）；⑦ CE 在自己 End/Begin 出 LinearWrap 批次后靠 <c>PRTLoader.BeginDrawingWithMode</c>
    /// 把批次接回所在桶，本工程对应 <see cref="DRKLoader.BeginDrawingWithMode"/>。
    /// </para>
    /// </summary>
    internal class StarTrailParticle:BaseParticle
    {
        /// <summary>轨迹采样点（最新的在最前）</summary>
        public List<Vector2> odp = new List<Vector2>();
        /// <summary>轨迹最多保留多少点</summary>
        public int maxLength = 8;
        /// <summary>是否每帧采样：关掉就只画头辉、不拉带子（CE 的调用点偶尔这么用）</summary>
        public bool addPoint = true;
        /// <summary>重力加速度（再乘 gA）</summary>
        public float gravity = 0f;
        /// <summary>重力倍率</summary>
        public float gA = 1f;
        /// <summary>淡出档位：-1 表示全程按剩余比例渐隐，大于 0 表示只在最后这些帧里渐出、前段保持全亮</summary>
        public int fadeOut = -1;
        /// <summary>带子与头辉用的条纹贴图（CE 的 PRTExtraTextures.Streak1w）</summary>
        private const string StreakTexture = "CalamityDemutation/Assets/ExtraTextures/Streak1w";
        /// <summary>Configure 决定的混合模式（三态简化为二选一）</summary>
        private bool additiveBlend = true;
        /// <summary>贴图与类同名同目录</summary>
        public override string Texture => "CalamityDemutation/Content/Particles/StarTrailParticle";
        /// <summary>加法混合（默认）；Configure 传 false 时改用半透明混合</summary>
        public override bool UseAdditiveBlend => additiveBlend;
        /// <summary>非加法混合时走半透明那条桶</summary>
        public override bool UseHalfTransparency => !additiveBlend;
        /// <summary>带子要自己 End/Begin 出 LinearWrap 批次，故自行绘制</summary>
        public override bool UseCustomDraw => true;
        /// <summary>按 CE 的 Configure 语义设置混合模式、初始旋转与寿命（要在 NewParticle 之后调用）</summary>
        public void Configure(bool additiveBlend, float rotation = 0f, int lifetime = -1)
        {
            this.additiveBlend = additiveBlend;
            Rotation = rotation;
            if (lifetime > 0)
            {
                Lifetime = lifetime;
            }
        }
        /// <summary>生成时补上默认寿命 30 帧（CE 旧 StarTrail 的默认值）</summary>
        public override void SetDRK()
        {
            if (Lifetime <= 0)
            {
                Lifetime = 30;
            }
        }
        /// <summary>把新采样点插到表头，超出上限就丢掉最老的点</summary>
        public void AddPoint(Vector2 pos)
        {
            odp.Insert(0, pos);
            if (odp.Count > maxLength)
            {
                odp.RemoveAt(odp.Count - 1);
            }
        }
        public override void AI()
        {
            if (addPoint)
            {
                AddPoint(Position);
            }
            Velocity += gravity * Vector2.UnitY * gA;
            Rotation = Velocity.ToRotation();
            Velocity *= 0.94f;              // 衰减系数是 CE 的旧值，改了星轨手感会飘
        }
        /// <summary>
        /// 自绘：先画两层头辉，再沿轨迹拉外层（8px 原色）与内层（3px 提亮 1.6 倍）两条带子。
        /// 带子靠 LinearWrap 让条纹贴图沿 U 滚动，所以这里要先 End 掉所在桶的批次、Begin 一个 LinearWrap 批次，
        /// 画完再用 <see cref="DRKLoader.BeginDrawingWithMode"/> 把批次接回原样（否则同桶后面的粒子会花屏）。
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            // fadeOut 为 -1 时全程按剩余比例渐隐，大于 0 时只在最后 fadeOut 帧里渐出
            float fscale = fadeOut == -1 ? (1f - LifetimeCompletion) : Math.Min(1f, (Lifetime - Time) / (float)fadeOut);
            Texture2D tex = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, additiveBlend ? BlendState.Additive : BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * fscale, Rotation, tex.Size() / 2f, new Vector2(1.4f, 0.8f) * 0.22f * Scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * fscale * 1.2f, Rotation, tex.Size() / 2f, new Vector2(1.4f, 0.8f) * 0.22f * Scale * 0.55f, SpriteEffects.None, 0f);
            if (odp.Count >= 3)
            {
                DrawTrailStrip(8f, Color * fscale);
                DrawTrailStrip(3f, Color * fscale * 1.6f);
            }
            spriteBatch.End();
            DRKLoader.BeginDrawingWithMode(this, spriteBatch);
        }
        /// <summary>沿轨迹拉一条带子：halfWidth 为半宽，b 为顶点色（越靠尾越淡）</summary>
        private void DrawTrailStrip(float halfWidth, Color b)
        {
            List<VertexPosition2DColorTexture> ve = new List<VertexPosition2DColorTexture>();
            ve.Add(new VertexPosition2DColorTexture(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * halfWidth * Scale,
                b, new Vector2(-Main.GlobalTimeWrappedHourly * 2.5f, 1), 1f));
            ve.Add(new VertexPosition2DColorTexture(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * halfWidth * Scale,
                b, new Vector2(-Main.GlobalTimeWrappedHourly * 2.5f, 0), 1f));
            for (int i = 1; i < odp.Count; i++)
            {
                Color tailColor = b * ((odp.Count - i) / (float)odp.Count);
                float u = (i / (float)odp.Count) - Main.GlobalTimeWrappedHourly * 2.5f;
                ve.Add(new VertexPosition2DColorTexture(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * halfWidth * Scale,
                    tailColor, new Vector2(u, 1), 1f));
                ve.Add(new VertexPosition2DColorTexture(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * halfWidth * Scale,
                    tailColor, new Vector2(u, 0), 1f));
            }
            if (ve.Count >= 3)
            {
                GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
                graphicsDevice.Textures[0] = ModContent.Request<Texture2D>(StreakTexture).Value;
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
        }
    }
}
