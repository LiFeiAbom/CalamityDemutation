using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 光晕（LightParticle，移植自 CalamityEntropy 的 PRT_Light）：一团按速度方向压扁的发光（bloom 层 + 主体 + 高光三层叠画），
    /// 透明度走「前半段渐亮、后半段渐暗」的正弦曲线，体积每帧 ×0.95；速度在寿命前 1/3 之后由加速改为减速。
    /// 最终分形全屏斩命中时会炸一圈（<c>Configure(0.15f, lifetime: 60)</c>）。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>Opacity</c> 由基类提供，本模组基类没有该字段，故自持一份；
    /// ③ CE 在 <c>SetProperty</c> 里把混合模式钉死成加法混合，这里直接写成 <see cref="UseAdditiveBlend"/> 为真；
    /// ④ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）、<c>ShouldKillWhenOffScreen</c> 与 CE 自设的
    /// <c>InGame_World_MaxCount = 14000</c>（本模组粒子数上限由 <c>DRKLoader</c> 统一管）；
    /// ⑤ CE 的 bloom 贴图走 <c>[VaultLoaden]</c> 静态字段以便「别在 PreDraw 里 Request」，本模组按既有做法
    /// （同 <c>StarlessNightProj</c> 的绘制路径）直接在绘制时取；
    /// ⑥ CE 绘制时写 <c>Main.spriteBatch</c>（注释标明是历史遗留），本模组基类传进来的就是这个实例，故用形参。
    /// </para>
    /// </summary>
    internal class LightParticle:BaseParticle
    {
        /// <summary>整体透明度（CE 基类的 Opacity），每帧由 AI 按正弦曲线重算</summary>
        public float Opacity = 1f;
        /// <summary>速度对压扁程度的影响系数（CE 默认 1）</summary>
        public float SquishStrenght = 1f;
        /// <summary>压扁程度的上限（CE 默认 3）</summary>
        public float MaxSquish = 3f;
        /// <summary>色相偏移，每帧把自身颜色绕色环推这么多</summary>
        public float HueShift;
        /// <summary>跟随实体时把该实体速度按此比例加到自身位置上（CE 默认 0.9）</summary>
        public float followingRateRatio = 0.9f;
        /// <summary>可选跟随的实体（CE 用 entity；本模组当前无调用方设置它）</summary>
        public Entity entity;
        /// <summary>粒子主贴图（CE 的 PRT_Light）</summary>
        public override string Texture => "CalamityDemutation/Content/Particles/LightParticle";
        /// <summary>bloom 外层贴图（CE 的 PRT_Light2），尺寸与主贴图不同，绘制时按高度比换算</summary>
        private const string BloomTexture = "CalamityDemutation/Content/Particles/LightParticle2";
        /// <summary>加法混合（CE 在 SetProperty 里钉死的模式）</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>需要自绘（三层叠画与压扁换算在 CustomDraw 里）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>按 CE 的 Configure 语义设置参数（调用点在 <c>DRKLoader.NewParticle</c> 之后）</summary>
        public void Configure(float opacity, float squishStrenght = 1f, float maxSquish = 3f,
            float hueShift = 0f, Entity entity = null, float followingRateRatio = 0.9f, int lifetime = -1)
        {
            Opacity = opacity;
            SquishStrenght = squishStrenght;
            MaxSquish = maxSquish;
            HueShift = hueShift;
            this.entity = entity;
            this.followingRateRatio = followingRateRatio;
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
            // 寿命前 1/3 之后由加速转为减速，制造"冲出去再收住"的手感
            Velocity *= LifetimeCompletion >= 0.34f ? 0.93f : 1.02f;
            Opacity = LifetimeCompletion > 0.5f
                ? (float)Math.Sin(LifetimeCompletion * MathHelper.Pi) * 0.2f + 0.8f
                : (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
            Scale *= 0.95f;
            Color = Main.hslToRgb(Main.rgbToHsl(Color).X + HueShift, Main.rgbToHsl(Color).Y, Main.rgbToHsl(Color).Z);
            if (entity != null && entity.active)
            {
                // 跟随由粒子自己算，框架只管 Velocity 位移（CE 同款）
                Position += entity.velocity * followingRateRatio;
            }
        }
        /// <summary>
        /// 自绘三层：bloom 光晕（放大且按两图高度比折算）→ 主体的 1.1 倍加亮层 → 主体的高光层；
        /// 三者都沿速度方向 +90° 旋转、并按速度大小压扁。
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            Texture2D bloomTex = ModContent.Request<Texture2D>(BloomTexture).Value;
            float squish = MathHelper.Clamp(Velocity.Length() / 10f * SquishStrenght, 1f, MaxSquish);
            float rot = Velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 origin = tex.Size() / 2f;
            Vector2 scale = new(Scale - Scale * squish * 0.3f, Scale * squish);
            // 主体和 bloom 图尺寸不同，按高度比缩回去，否则光晕会比主体大出一圈
            float properBloomSize = tex.Height / (float)bloomTex.Height;
            Vector2 drawPosition = Position - Main.screenPosition;
            spriteBatch.Draw(bloomTex, drawPosition, null, Color * Opacity * 0.8f, rot, bloomTex.Size() / 2f, scale * 2 * properBloomSize, SpriteEffects.None, 0f);
            spriteBatch.Draw(tex, drawPosition, null, Color * Opacity * 0.8f, rot, origin, scale * 1.1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(tex, drawPosition, null, Color.White * Opacity * 0.9f, rot, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
