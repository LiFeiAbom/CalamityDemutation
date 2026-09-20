using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 虚影剑气命中特效（VoidImpactParticle，移植自 CalamityEntropy 的 PRT_VoidImpactParticle）：
    /// 一道被横向拉伸成冲击条的发光，逐帧横轴收窄、纵向拉满，同时减速并淡出。
    /// 虚影剑气（<c>VoidImpact</c>）命中敌人时朝四个斜角各炸两条。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>Opacity</c> 由基类提供，本模组基类没有该字段，
    /// 故自持一份；③ **删掉 CE 的 <c>Glow</c> 字段** —— CE 自己的注释就写明「旧字段，PreDraw 没读」，
    /// 只是调用点仍在传，属不产生效果的死字段（本工程警告基线 0，这类字段留不得），
    /// <c>Configure</c> 的签名与调用点随之去掉该参数；
    /// ④ CE 的 <c>PRTDrawMode</c> 在唯一调用点恒为 <c>AlphaBlend</c>，本粒子直接不打开任何混合开关
    /// （即默认的 AlphaBlend 桶），同 <see cref="LineParticleCal"/> 的处理口径；
    /// ⑤ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>。
    /// </para>
    /// </summary>
    internal class VoidImpactParticle:BaseParticle
    {
        /// <summary>整体透明度（CE 基类的 Opacity），由 Configure 给定，绘制时与颜色相乘</summary>
        public float Opacity = 1f;
        /// <summary>横轴收窄系数：每帧 ×0.86，绘制时按 <c>scaleX * 2</c> 做冲击条拉伸</summary>
        public float scaleX = 1f;
        /// <summary>纵向展开系数：从 0.4 往 1 插值，画满后是一条完整的冲击条</summary>
        public float scale2 = 0.4f;
        /// <summary>贴图与类同名同目录</summary>
        public override string Texture => "CalamityDemutation/Content/Particles/VoidImpactParticle";
        /// <summary>需要自绘（冲击条拉伸在 CustomDraw 里）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>按 CE 的 Configure 语义设置透明度、初始旋转与寿命（调用点在 <c>DRKLoader.NewParticle</c> 之后）</summary>
        public void Configure(float opacity, float rotation = 0f, int lifetime = -1)
        {
            Opacity = opacity;
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
            Velocity *= 0.92f;
            scaleX *= 0.86f;
            scale2 = float.Lerp(scale2, 1, 0.14f);
        }
        /// <summary>自绘：一条横向拉伸的冲击条，颜色乘整体透明度</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color * Opacity, Rotation, texture.Size() / 2f, new Vector2(scaleX * 2, 1) * Scale * scale2, SpriteEffects.None, 0f);
        }
    }
}
