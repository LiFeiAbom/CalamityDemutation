using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 自定义火花粒子（CustomSpark） - 移植自灾厄 <c>Particles/CustomSpark.cs</c>：
    /// 一枚沿速度方向拉伸、随存活进度按 <c>LifetimeCompletion³</c> 淡出的火花，贴图由调用方传入
    /// （灾厄自己在这族里传 <c>Particles/GlowSpark</c> 与 <c>Particles/BloomCircle</c> 两张）。
    /// <para>
    /// 本工程的三处差异：① 基类换成本工程 <see cref="Particle"/>（字段与灾厄 <c>Particle</c> 一致）；
    /// ② 删掉源里"天顶世界 + 星期二 → 把贴图换成 MammothParticle"的彩蛋分支（用户口径：天顶世界内容一律不做）；
    /// ③ <c>AffectedByLight</c> 在灾厄那边是 <c>Particle</c> 基类上的字段，本工程基类没有，故在本类里自带一个。
    /// </para>
    /// <para>
    /// 更新口径（照源）：<c>FadeIn</c> 为假时按 0.95 倍逐帧缩水并整体淡出；为真时前半个寿命放大到
    /// <see cref="FadeInScale"/>、后半个寿命再缩回 0。速度每帧乘 0.95；开了重力的粒子在速度降到
    /// 12 以下后 X 分量再乘 0.94、Y 分量每帧 +0.25（用于火花坠地感）。
    /// </para>
    /// </summary>
    internal class CustomSpark:Particle
    {
        /// <summary>初始颜色（构造时写入，更新时由它向透明插值）</summary>
        public Color InitialColor;
        /// <summary>是否受重力：速度降到 12 以下后开始下坠</summary>
        public bool AffectedByGravity;
        /// <summary>是否按所在格的环境光照调暗（灾厄基类字段，本类自带）</summary>
        public bool AffectedByLight;
        /// <summary>是否为"先放大再缩回"的淡入型火花</summary>
        public bool FadeIn = false;
        /// <summary>淡入型火花的目标缩放（构造时记下初始 <c>scale</c>）</summary>
        public float FadeInScale = 0f;
        /// <summary>是否在火花中心额外叠一枚更小的亮芯</summary>
        public bool GlowCenter = false;
        /// <summary>亮芯相对本体的缩放系数</summary>
        public float GlowCenterScale = 1;
        /// <summary>亮芯的额外不透明度</summary>
        public float GlowOpacity = 1;
        /// <summary>调用方传入的贴图资源路径</summary>
        public string NewTexture;
        /// <summary>额外的绘制旋转（叠在"速度方向 + 90°"之上）</summary>
        public float ExtraRotation;
        /// <summary>绘制时的拉伸向量（X 横、Y 纵），由 <see cref="ShrinkSpeed"/> 逐帧挤长</summary>
        public Vector2 Stretch = new Vector2(0.5f, 1.6f);
        /// <summary>挤长速度：X 每帧乘 (1 - 0.2×它)、Y 每帧乘 (1 + 0.2×它)</summary>
        public float ShrinkSpeed = 0;
        /// <summary>是否水平翻转贴图</summary>
        public bool FlipHorizontal = false;
        /// <summary>是否禁用"每帧缩水 0.95"这一步</summary>
        public bool NoShrink = false;
        /// <summary>每帧额外叠加的旋转（自旋）</summary>
        public float Spin = 0;
        /// <summary>是否用加法混合（照源：由构造参数 <c>useAddativeBlend</c> 决定）</summary>
        public bool AltVisual = true;
        /// <summary>走自定义绘制；本类贴图由调用方给，默认绘制路径不适用</summary>
        public override bool UseCustomDraw => true;
        /// <summary>加法混合开关</summary>
        public override bool UseAdditiveBlend => AltVisual;
        /// <summary>寿命到期由 <see cref="GeneralParticleHandler"/> 自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>与灾厄相同的隐形占位贴图（本类实际绘制用的是 <see cref="NewTexture"/>）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 构造一枚火花（参数顺序与灾厄源逐项一致）。
        /// </summary>
        /// <param name="relativePosition">世界坐标起点</param>
        /// <param name="velocity">每帧叠加到位置的速度</param>
        /// <param name="texture">贴图资源路径</param>
        /// <param name="affectedByGravity">是否受重力</param>
        /// <param name="lifetime">寿命帧数</param>
        /// <param name="scale">绘制缩放</param>
        /// <param name="color">初始颜色</param>
        /// <param name="stretch">拉伸向量</param>
        /// <param name="useAddativeBlend">是否加法混合</param>
        /// <param name="glowCenter">是否叠亮芯</param>
        /// <param name="extraRotation">额外旋转</param>
        /// <param name="fadeIn">是否为淡入型</param>
        /// <param name="affectedByLight">是否采样环境光照</param>
        /// <param name="shrinkSpeed">挤长速度</param>
        /// <param name="glowCenterScale">亮芯缩放系数</param>
        /// <param name="glowOpacity">亮芯不透明度</param>
        /// <param name="flipHorizontal">是否水平翻转</param>
        /// <param name="noShrink">是否禁用逐帧缩水</param>
        /// <param name="spin">自旋速度</param>
        public CustomSpark(Vector2 relativePosition, Vector2 velocity, string texture, bool affectedByGravity, int lifetime, float scale, Color color, Vector2 stretch, bool useAddativeBlend = true, bool glowCenter = false, float extraRotation = 0, bool fadeIn = false, bool affectedByLight = false, float shrinkSpeed = 0, float glowCenterScale = 1, float glowOpacity = 1, bool flipHorizontal = false, bool noShrink = false, float spin = 0)
        {
            Position = relativePosition;
            Velocity = velocity;
            NewTexture = texture;
            ExtraRotation = extraRotation;
            AffectedByGravity = affectedByGravity;
            AffectedByLight = affectedByLight;
            Scale = scale;
            Stretch = stretch;
            FadeInScale = scale;
            Lifetime = lifetime;
            Color = InitialColor = color;
            ShrinkSpeed = shrinkSpeed;

            AltVisual = useAddativeBlend;
            GlowCenter = glowCenter;
            GlowCenterScale = glowCenterScale;
            GlowOpacity = glowOpacity;
            FlipHorizontal = flipHorizontal;
            NoShrink = noShrink;

            FadeIn = fadeIn;
            if (FadeIn)
            {
                Scale = 0f;
            }
            Spin = spin;
        }
        /// <summary>每帧更新：缩水/淡入淡出、速度衰减、重力与挤长（照源，一行不动）</summary>
        public override void Update()
        {
            if (!FadeIn)
            {
                if (!NoShrink)
                {
                    Scale *= 0.95f;
                }
                Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            }
            else
            {
                if ((float)Time / (float)Lifetime < 0.5f)
                {
                    Scale = MathHelper.Lerp(Scale, FadeInScale, 0.2f);
                }
                else
                {
                    Scale = MathHelper.Lerp(Scale, FadeInScale, -0.21f);
                }
            }
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            ExtraRotation += Spin;
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2 + ExtraRotation;

            Stretch.X *= (1 - 0.2f * ShrinkSpeed);
            Stretch.Y *= (1 + 0.2f * ShrinkSpeed);
        }
        /// <summary>
        /// 自定义绘制：把贴图按 <see cref="Stretch"/> × <see cref="Scale"/> 画在屏幕坐标处（原点贴图中心），
        /// 颜色再乘一次 <c>LifetimeCompletion³</c> 的淡出；开了 <see cref="GlowCenter"/> 时再叠一枚
        /// 更小、更偏白的亮芯。
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Vector2 scale = Stretch * Scale;
            Texture2D texture = ModContent.Request<Texture2D>(NewTexture).Value;

            Color col = Color;
            if (AffectedByLight)
            {
                col = Lighting.GetColor((Position / 16).ToPoint()).MultiplyRGB(Color);
            }

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.Lerp(col, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D)), Rotation, texture.Size() * 0.5f, scale, FlipHorizontal ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            if (GlowCenter)
            {
                spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.Lerp(Color.Lerp(col, Color.White, 0.8f), Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D)) * GlowOpacity, Rotation, texture.Size() * 0.5f, scale * 0.8f * GlowCenterScale, FlipHorizontal ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            }
        }
    }
}
