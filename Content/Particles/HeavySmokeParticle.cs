using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 浓烟粒子（HeavySmokeParticle，移植自 CalamityEntropy 的 PRT_HeavySmokeCal，即灾厄 HeavySmokeParticle 的搬运版）：
    /// 一张「6 帧纵向动画 × 7 种变体」的 80×80 烟图，生成后先胀大、随后收缩并逐帧淡出，
    /// 可选受环境光影响、可选走加法混合（发光烟）。星陨与聚魂分形那一系都会用到。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>Opacity</c> 由基类提供且这里真的在用
    /// （逐帧 ×0.98 并与 LifetimeCompletion 的淡出叠乘），本模组基类没有该字段，改为自带同名字段；
    /// ③ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>（本模组不淘汰出屏粒子）；
    /// ④ <c>PRTDrawModeEnum</c> 三态混合简化为由 <c>Configure(glowing)</c> 决定的
    /// <see cref="UseAdditiveBlend"/>/<see cref="UseHalfTransparency"/> 两个 bool；
    /// ⑤ 贴图改为与类同名的 Content/Particles/HeavySmokeParticle.png（CE 取自它自己的 Assets/Particles/HeavySmoke）；
    /// ⑥ <c>Configure</c> 由返回 this 的链式写法改为普通设置方法，调用点在 <c>DRKLoader.NewParticle</c> 之后调它。
    /// </para>
    /// </summary>
    internal class HeavySmokeParticle:BaseParticle
    {
        /// <summary>烟图纵向帧数</summary>
        private const int FrameAmount = 6;
        /// <summary>自旋速度</summary>
        public float Spin;
        /// <summary>是否需要更强表现（CE 旧构造遗留标记，延续原语义只做记录）</summary>
        public bool StrongVisual;
        /// <summary>是否发光（走加法混合）</summary>
        public bool Glowing;
        /// <summary>色相偏移（每帧把当前颜色往这个方向推）</summary>
        public float HueShift;
        /// <summary>是否采样环境光照</summary>
        public bool AffectedByLight;
        /// <summary>当前透明度：逐帧 ×0.98 衰减，绘制时与颜色相乘</summary>
        public float Opacity = 1f;
        /// <summary>Configure 决定的混合模式（三态简化为二选一）</summary>
        private bool additiveBlend;
        /// <summary>贴图与类同名同目录</summary>
        public override string Texture => "CalamityDemutation/Content/Particles/HeavySmokeParticle";
        /// <summary>发光烟走加法混合，否则走半透明混合</summary>
        public override bool UseAdditiveBlend => additiveBlend;
        /// <summary>非发光时走半透明那条桶</summary>
        public override bool UseHalfTransparency => !additiveBlend;
        /// <summary>帧动画与受光采样都要自己算，故自行绘制</summary>
        public override bool UseCustomDraw => true;
        /// <summary>按 CE 的 Configure 语义设置参数（要在 <c>DRKLoader.NewParticle</c> 之后调用）</summary>
        public void Configure(float opacity, int lifetime, float rotationSpeed = 0f,
            bool glowing = false, float hueshift = 0f, bool required = false, bool affectedByLight = false)
        {
            Opacity = opacity;
            Spin = rotationSpeed;
            Glowing = glowing;
            HueShift = hueshift;
            StrongVisual = required;
            AffectedByLight = affectedByLight;
            additiveBlend = glowing;
            if (lifetime > 0)
            {
                Lifetime = lifetime;
            }
        }
        /// <summary>生成时补上默认寿命 200 帧并随机挑一个横向变体（用基类的 Variant 字段）</summary>
        public override void SetDRK()
        {
            if (Lifetime <= 0)
            {
                Lifetime = 200;
            }
            Variant = Main.rand.Next(7);
        }
        public override void AI()
        {
            // 前 20% 用 Time/Lifetime（与灾厄原版写法一致，不用 Completion）胀大，之后一路收缩
            if (Time / (float)Lifetime < 0.2f)
            {
                Scale += 0.01f;
            }
            else
            {
                Scale *= 0.975f;
            }
            Vector3 hsl = Main.rgbToHsl(Color);
            Color = Main.hslToRgb((hsl.X + HueShift) % 1f, hsl.Y, hsl.Z);
            Opacity *= 0.98f;
            Rotation += Spin * (Velocity.X > 0f ? 1f : -1f);
            Velocity *= 0.85f;
            float fade = Utils.GetLerpValue(1f, 0.85f, LifetimeCompletion, clamped: true);
            Color *= fade;
        }
        /// <summary>自绘：按存活进度取纵向帧、按 Variant 取横向变体，颜色再乘透明度与环境光</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            int animationFrame = (int)Math.Floor(Time / (Lifetime / (float)FrameAmount));
            Rectangle frame = new Rectangle(80 * Variant, 80 * animationFrame, 80, 80);
            Color color = Color * Opacity;
            if (AffectedByLight)
            {
                color = color.MultiplyRGBA(Lighting.GetColor((Position / 16f).ToPoint()));
            }
            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, color, Rotation, frame.Size() / 2f, Scale, SpriteEffects.None, 0f);
        }
    }
}
