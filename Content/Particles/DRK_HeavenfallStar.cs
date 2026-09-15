using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// DRK 天降星屑粒子（DRK_HeavenfallStar） - 拉伸成条状的发光星屑，用于技能/命中特效。
    /// 与 DRK_Spark 的判据逐条相同（同贴图、同衰减曲线），区别仅在于不带 entity 跟随参数。
    /// </summary>
    internal class DRK_HeavenfallStar : BaseParticle
    {
        // ── 实例字段 ──
        /// <summary>
        /// 是否受重力影响（速度较低时开始下坠并横向减速）
        /// </summary>
        public bool AffectedByGravity;
        /// <summary>
        /// 初始颜色，用于随时间向透明插值
        /// </summary>
        public Color InitialColor;
        // ── 属性 ──
        /// <summary>
        /// 到达 Lifetime 后自动移除
        /// </summary>
        public override bool SetLifetime => true;
        /// <summary>
        /// 贴图路径：Assets/Particles/StarProj（与 DRK_Spark 共用同一张星屑贴图）
        /// </summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/StarProj";
        /// <summary>
        /// 使用加法混合，使星屑呈现发光感
        /// </summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>
        /// 使用自定义绘制
        /// </summary>
        public override bool UseCustomDraw => true;
        // ── 构造函数 ──
        /// <summary>
        /// 构造粒子：初始化位置、速度、重力开关、寿命、缩放与颜色
        /// </summary>
        public DRK_HeavenfallStar(Vector2 relativePosition, Vector2 velocity, bool affectedByGravity, int lifetime, float scale, Color color)
        {
            Position = relativePosition;
            Velocity = velocity;
            AffectedByGravity = affectedByGravity;
            Scale = scale;
            Lifetime = lifetime;
            // 同时记录初始色与当前色，供淡出插值使用
            Color = InitialColor = color;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 每帧更新：缩小、淡出、减速，受重力时缓慢下坠，并朝向速度方向旋转
        /// </summary>
        public override void AI()
        {
            Scale *= 0.95f;
            // 随寿命进度按三次方曲线向透明插值，末段淡出更快
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            // 速度低于阈值且受重力：横向阻尼并持续下坠
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            // 贴图长轴默认朝上，故旋转需额外加 90°
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>
        /// 自定义绘制：用同一贴图叠画两层（外层拉长 + 内层更细），合成细长星屑外形
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            // 外层：横向压扁、纵向拉长的星屑主体
            Vector2 scale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, scale, 0, 0f);
            // 内层：更细的核心，提亮星屑中心
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, scale * new Vector2(0.45f, 1f), 0, 0f);
        }
    }
}
