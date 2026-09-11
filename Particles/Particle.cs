using CalamityDemutation.Enums;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 粒子基类（移植自灾厄的 Particle）。
    /// 裁剪掉像素化、自定义着色器、粒子集合（BaseParticleSet）、帧变体、Type/纹理自动注册等本工程用不到的部分。
    /// </summary>
    public abstract class Particle
    {
        /// <summary>绘制颜色（Update 里可自行覆盖）。</summary>
        public Color Color;
        /// <summary>挂接到泰拉整体绘制顺序的锚点，默认在尘土之后。</summary>
        public GeneralDrawLayer DrawLayer = GeneralDrawLayer.AfterDusts;
        /// <summary>寿命帧数；0 表示不受寿命控制（需子类自行处理移除）。</summary>
        public int Lifetime = 0;
        /// <summary>世界坐标位置。</summary>
        public Vector2 Position;
        /// <summary>绘制旋转弧度。</summary>
        public float Rotation;
        /// <summary>绘制缩放。</summary>
        public float Scale;
        /// <summary>已存活帧数，每帧由 GeneralParticleHandler 自增。</summary>
        public int Time;
        /// <summary>每帧叠加到 Position 的速度。</summary>
        public Vector2 Velocity;
        /// <summary>是否关键粒子（达到粒子上限时仍尝试生成）。</summary>
        public virtual bool Important => false;
        /// <summary>0-1 的生存进度插值：粒子存活时间占其寿命的比例。</summary>
        public float LifetimeCompletion => Lifetime != 0 ? Time / (float)Lifetime : 0;
        /// <summary>是否在时间到达 Lifetime 时自动移除。</summary>
        public virtual bool SetLifetime => false;
        /// <summary>粒子贴图路径（仅自定义绘制粒子可能需要，默认绘制路径已裁剪）。</summary>
        public virtual string Texture => "";
        /// <summary>是否使用加法混合（否则用 Alpha 混合）。</summary>
        public virtual bool UseAdditiveBlend => false;
        /// <summary>是否自定义绘制（调用 CustomDraw 而非默认绘制）。</summary>
        public virtual bool UseCustomDraw => false;
        /// <summary>自定义绘制入口：UseCustomDraw 为真时由绘制层调用。</summary>
        public virtual void CustomDraw(SpriteBatch spriteBatch) { }
        /// <summary>每帧更新入口：由 GeneralParticleHandler 在推进 Time 后调用。</summary>
        public virtual void Update() { }
        /// <summary>从 GeneralParticleHandler 中移除本粒子。</summary>
        public void Kill() => GeneralParticleHandler.RemoveParticle(this);
    }
}
