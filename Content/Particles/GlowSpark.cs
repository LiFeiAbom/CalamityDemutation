using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 发光火花粒子（GlowSpark） - 移植自 CalamityEntropy 的 PRT_GlowSpark：
    /// 生成后按存活进度淡出，受重力时下坠并把旋转对齐到速度方向，默认用加法混合渲染成发光点。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组的 <see cref="BaseParticle"/>（对应关系为
    /// <c>SetProperty → SetDRK</c>、<c>PreDraw → UseCustomDraw + CustomDraw</c>）；
    /// ② CE 用基类的 <c>Opacity</c> 字段承载淡出，本模组基类没有该字段，改为在绘制时把
    /// <c>(1 - LifetimeCompletion)</c> 乘进颜色；③ CE 的 <c>PRTDrawModeEnum</c> 三态混合
    /// 简化为 <see cref="UseAdditiveBlend"/>（本粒子只被 FractalShoot 以加法混合使用）；
    /// ④ 去掉 CE 的对象池（<c>CanPool</c>）与 <c>ShouldKillWhenOffScreen</c>（本模组粒子系统不淘汰出屏粒子）。
    /// </para>
    /// </summary>
    internal class GlowSpark:BaseParticle
    {
        /// <summary>是否受重力影响：为真时持续下坠并把旋转对齐到速度方向，为假时只淡出</summary>
        public bool grav = true;
        /// <summary>是否按发光绘制；为假时改为采样所在格的环境光照（CE 原字段）</summary>
        public bool glowing = true;
        /// <summary>贴图与类同名同目录</summary>
        public override string Texture => "CalamityDemutation/Content/Particles/GlowSpark";
        /// <summary>寿命到期后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>加法混合，使光点呈现发光感</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>淡出需要按存活进度逐帧计算，故自行绘制</summary>
        public override bool UseCustomDraw => true;
        /// <summary>生成时补上默认寿命 26 帧（CE 在 SetProperty 里做同样的事）</summary>
        public override void SetDRK()
        {
            if (Lifetime <= 0)
            {
                Lifetime = 26;
            }
        }
        /// <summary>受重力时缓慢下坠并转向速度方向；淡出由绘制阶段按存活进度完成</summary>
        public override void AI()
        {
            if (grav)
            {
                Velocity += Vector2.UnitY * 0.2f;
                Rotation = Velocity.ToRotation();
            }
        }
        /// <summary>自绘：颜色乘以 (1 - 存活进度) 实现淡出；非发光模式改为采样环境光照</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            Color clr = glowing ? Color : Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), Color);
            clr *= 1f - LifetimeCompletion;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, clr, Rotation, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
        }
    }
}
