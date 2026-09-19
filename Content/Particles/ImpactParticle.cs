using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 冲击波（ImpactParticle，移植自 CalamityEntropy 的 PRT_ImpactParticle，即灾厄旧版 Impact 粒子的搬运版）：
    /// 一张炸开的光环贴图，每帧胀大（增速本身按 ×0.9 衰减，所以是先猛后缓），颜色每帧 ×0.96 淡出，
    /// 默认 120 帧后由粒子系统回收。聚魂之影锁定目标时会炸一发。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>PRTDrawMode</c> 三态混合保留为
    /// <see cref="DrawMode"/> 字段，并映射到基类的 <see cref="UseAdditiveBlend"/>/<see cref="UseHalfTransparency"/>
    /// 两个批次开关（CE 在绘制里也按它区分「只乘 A」还是「整体相乘」）；
    /// ③ CE 的 <c>Opacity</c> 由基类提供，本模组基类没有该字段，故自持一份（CE 的对象池 Reset 会把它清零，
    /// 而唯一的调用方 <c>FractalGhostBlade</c> 每次都用 Configure 显式传 1，故这里默认取 1）；
    /// ④ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>（本模组粒子系统不淘汰出屏粒子）；
    /// ⑤ <c>Configure</c> 由返回 this 的链式写法改为普通设置方法，调用点在 <c>DRKLoader.NewParticle</c> 之后。
    /// </para>
    /// </summary>
    internal class ImpactParticle:BaseParticle
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
        /// <summary>是否发光：为假时吃地图光照（CE 的 Glow）</summary>
        public bool Glow = true;
        /// <summary>整体透明度（CE 基类的 Opacity）</summary>
        public float Opacity = 1f;
        /// <summary>每帧的胀大量，自身按 ×0.9 衰减</summary>
        private float sadd = 0.1f;
        /// <summary>冲击波贴图（CE 的 PRTExtraTextures.Impact2）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/Impact2";
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
        /// <summary>生成时补上默认寿命 120 帧（CE 旧版 Impact 的原默认）</summary>
        public override void SetDRK()
        {
            if (Lifetime <= 0)
            {
                Lifetime = 120;
            }
        }
        public override void AI()
        {
            Scale += sadd;
            sadd *= 0.9f;
            Color *= 0.96f;
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
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, clr, Rotation, texture.Size() / 2f, Scale, SpriteEffects.None, 0f);
        }
    }
}
