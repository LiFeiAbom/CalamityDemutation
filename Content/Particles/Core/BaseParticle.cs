using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace CalamityDemutation.Content.Particles.Core
{
    /// <summary>
    /// 一个粒子的基类，主要移植自灾厄本体的粒子体系
    /// </summary>
    internal abstract class BaseParticle
    {
        public virtual string Texture => "";
        public virtual int FrameVariants => 1;
        public int Variant = 0;
        /// <summary>
        /// 由粒子处理器注册的粒子类型 ID，在粒子处理器 Load 时自动设置
        /// </summary>
        public int Type;
        /// <summary>
        /// 粒子已存在的帧数，一般无需手动更新
        /// </summary>
        public int Time;
        /// <summary>
        /// 达到粒子上限时仍需渲染则设为 true
        /// </summary>
        public virtual bool Important => false;
        /// <summary>
        /// 达到最大寿命时自动移除则设为 true
        /// </summary>
        public virtual bool SetLifetime => false;
        /// <summary>
        /// 使用加法混合（而非 alpha 混合）则设为 true
        /// </summary>
        public virtual bool UseAdditiveBlend => false;
        /// <summary>
        /// 使用半透明混合则设为 true（被 UseAdditiveBlend 覆盖）
        /// </summary>
        public virtual bool UseHalfTransparency => false;
        /// <summary>
        /// 禁用默认绘制、改调 CustomDraw 则设为 true
        /// </summary>
        public virtual bool UseCustomDraw => false;
        /// <summary>
        /// 粒子最大存活时间（tick），需配合 SetLifetime 使用
        /// </summary>
        public int Lifetime = 0;
        /// <summary>
        /// 存活时间比例（0~1）
        /// </summary>
        public float LifetimeCompletion => Lifetime != 0 ? Time / (float)Lifetime : 0;
        /// <summary>
        /// 粒子世界坐标
        /// </summary>
        public Vector2 Position;
        /// <summary>
        /// 粒子速度，用于位置更新
        /// </summary>
        public Vector2 Velocity;
        /// <summary>
        /// 绘制中心
        /// </summary>
        public Vector2 Origin;
        /// <summary>
        /// 全局颜色
        /// </summary>
        public Color Color;
        /// <summary>
        /// 旋转角度
        /// </summary>
        public float Rotation;
        /// <summary>
        /// 体积缩放
        /// </summary>
        public float Scale;
        /// <summary>
        /// 粒子 AI 数据
        /// </summary>
        public float[] ai = new float[3];
        /// <summary>
        /// 自定义绘制（UseCustomDraw 为 true 时调用）
        /// </summary>
        public virtual void CustomDraw(SpriteBatch spriteBatch) { }
        /// <summary>
        /// 生成粒子时执行一次，用于内部初始化
        /// </summary>
        public virtual void SetDRK() { }
        /// <summary>
        /// 每次更新调用（速度已自动叠加到位置，时间已自动增加）
        /// </summary>
        public virtual void AI() { }
        /// <summary>
        /// 从处理器移除粒子
        /// </summary>
        public void Kill() => DRKLoader.RemoveParticle(this);
    }
}
