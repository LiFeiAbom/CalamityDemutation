using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 变速度火花粒子（VelChangingSpark） - 移植自灾厄 <c>Particles/VelChangingSpark.cs</c>：
    /// 与 <see cref="CustomSpark"/> 同族，区别是它的速度会在存活期内**从起始速度插值到结束速度**
    /// （`LerpRate` 逐帧插值），所以看上去是"先冲出去、再拐弯飘走"的一枚火花。
    /// <para>
    /// 本工程与源的三处差异同 <see cref="CustomSpark"/>：基类换成本工程 <see cref="Particle"/>、
    /// 删掉"天顶世界 + 星期二换 MammothParticle"的彩蛋分支、`AffectedByLight` 自带一个字段。
    /// </para>
    /// </summary>
    internal class VelChangingSpark:Particle
    {
        /// <summary>初始颜色（构造时写入，更新时由它向透明插值）</summary>
        public Color InitialColor;
        /// <summary>是否按所在格的环境光照调暗（灾厄基类字段，本类自带）</summary>
        public bool AffectedByLight;
        /// <summary>是否为"先放大再缩回"的淡入型火花</summary>
        public bool FadeIn = false;
        /// <summary>淡入型火花的目标缩放（构造时记下初始 <c>scale</c>）</summary>
        public float FadeInScale = 0f;
        /// <summary>是否在火花中心额外叠一枚更小的亮芯</summary>
        public bool GlowCenter = false;
        /// <summary>调用方传入的贴图资源路径</summary>
        public string NewTexture;
        /// <summary>额外的绘制旋转（叠在"速度方向 + 90°"之上）</summary>
        public float ExtraRotation;
        /// <summary>绘制时的拉伸向量（X 横、Y 纵），由 <see cref="ShrinkSpeed"/> 逐帧挤长</summary>
        public Vector2 Stretch = new Vector2(0.5f, 1.6f);
        /// <summary>挤长速度：X 每帧乘 (1 - 0.2×它)、Y 每帧乘 (1 + 0.2×它)</summary>
        public float ShrinkSpeed = 0;
        /// <summary>速度插值的终点（本族的核心：速度会朝它靠拢）</summary>
        public Vector2 EndVelocity;
        /// <summary>每帧把当前速度朝 <see cref="EndVelocity"/> 插值的比例（照源默认 0.1）</summary>
        public float LerpRate = 0.1f;
        /// <summary>是否用加法混合（照源：由构造参数 <c>useAddativeBlend</c> 决定）</summary>
        public bool AltVisual = true;
        /// <summary>走自定义绘制；本类贴图由调用方给</summary>
        public override bool UseCustomDraw => true;
        /// <summary>加法混合开关</summary>
        public override bool UseAdditiveBlend => AltVisual;
        /// <summary>寿命到期由 <see cref="GeneralParticleHandler"/> 自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>与灾厄相同的隐形占位贴图（实际绘制用的是 <see cref="NewTexture"/>）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 构造一枚变速度火花（参数顺序与灾厄源逐项一致）。
        /// </summary>
        /// <param name="relativePosition">世界坐标起点</param>
        /// <param name="startVelocity">起始速度</param>
        /// <param name="endVelocity">速度插值的终点</param>
        /// <param name="texture">贴图资源路径</param>
        /// <param name="lifetime">寿命帧数</param>
        /// <param name="scale">绘制缩放</param>
        /// <param name="color">初始颜色</param>
        /// <param name="stretch">拉伸向量</param>
        /// <param name="useAddativeBlend">是否加法混合</param>
        /// <param name="glowCenter">是否叠亮芯</param>
        /// <param name="extraRotation">额外旋转</param>
        /// <param name="fadeIn">是否为淡入型</param>
        /// <param name="shrinkSpeed">挤长速度</param>
        /// <param name="lerpRate">速度插值比例（照源默认 0.1）</param>
        public VelChangingSpark(Vector2 relativePosition, Vector2 startVelocity, Vector2 endVelocity, string texture, int lifetime, float scale, Color color, Vector2 stretch, bool useAddativeBlend = true, bool glowCenter = false, float extraRotation = 0, bool fadeIn = false, float shrinkSpeed = 0, float lerpRate = 0.1f)
        {
            Position = relativePosition;
            Velocity = startVelocity;
            EndVelocity = endVelocity;
            NewTexture = texture;
            ExtraRotation = extraRotation;
            AffectedByLight = false;
            Scale = scale;
            Stretch = stretch;
            FadeInScale = scale;
            Lifetime = lifetime;
            Color = InitialColor = color;
            ShrinkSpeed = shrinkSpeed;

            AltVisual = useAddativeBlend;
            GlowCenter = glowCenter;

            FadeIn = fadeIn;
            if (FadeIn)
            {
                Scale = 0f;
            }
            LerpRate = lerpRate;
        }
        /// <summary>每帧更新：缩水/淡出、两个速度各自衰减、再把当前速度朝终点插值（照源，一行不动）</summary>
        public override void Update()
        {
            if (!FadeIn)
            {
                if ((float)Time / (float)Lifetime < 0.5f)
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
            // 生命的前 80% 两个速度都按 0.95 衰减（之后就冻结速度、只淡出）
            if ((float)Time / (float)Lifetime < 0.8f)
            {
                Velocity *= 0.95f;
                EndVelocity *= 0.95f;
            }

            Velocity = new Vector2(MathHelper.Lerp(Velocity.X, EndVelocity.X, LerpRate), MathHelper.Lerp(Velocity.Y, EndVelocity.Y, LerpRate));

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2 + ExtraRotation;

            Stretch.X *= (1 - 0.2f * ShrinkSpeed);
            Stretch.Y *= (1 + 0.2f * ShrinkSpeed);
        }
        /// <summary>自定义绘制：按 <see cref="Stretch"/> × <see cref="Scale"/> 画在屏幕坐标处（原点贴图中心），
        /// 开了 <see cref="GlowCenter"/> 时再叠一枚更小、更偏白的亮芯</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Vector2 scale = Stretch * Scale;
            Texture2D texture = ModContent.Request<Texture2D>(NewTexture).Value;

            Color col = Color;
            if (AffectedByLight)
            {
                col = Lighting.GetColor((Position / 16).ToPoint()).MultiplyRGB(Color);
            }

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, col, Rotation, texture.Size() * 0.5f, scale, 0, 0f);
            if (GlowCenter)
            {
                spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.Lerp(Color.Lerp(col, Color.White, 0.8f), Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D)), Rotation, texture.Size() * 0.5f, scale * 0.8f, 0, 0f);
            }
        }
    }
}
