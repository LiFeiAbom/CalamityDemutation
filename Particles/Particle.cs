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
        public int Time;
        public int Lifetime = 0;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Rotation;
        public float Scale;
        public GeneralDrawLayer DrawLayer = GeneralDrawLayer.AfterDusts;
        /// <summary>0-1 的生存进度插值：粒子存活时间占其寿命的比例。</summary>
        public float LifetimeCompletion => Lifetime != 0 ? Time / (float)Lifetime : 0;
        /// <summary>粒子贴图路径（仅自定义绘制粒子可能需要，默认绘制路径已裁剪）。</summary>
        public virtual string Texture => "";
        /// <summary>是否关键粒子（达到粒子上限时仍尝试生成）。</summary>
        public virtual bool Important => false;
        /// <summary>是否在时间到达 Lifetime 时自动移除。</summary>
        public virtual bool SetLifetime => false;
        /// <summary>是否使用加法混合（否则用 Alpha 混合）。</summary>
        public virtual bool UseAdditiveBlend => false;
        /// <summary>是否自定义绘制（调用 CustomDraw 而非默认绘制）。</summary>
        public virtual bool UseCustomDraw => false;
        public virtual void CustomDraw(SpriteBatch spriteBatch) { }
        public virtual void Update() { }
        public void Kill() => GeneralParticleHandler.RemoveParticle(this);
    }
}
