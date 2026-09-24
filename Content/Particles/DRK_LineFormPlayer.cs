using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// DRK 线状跟随粒子（DRK_LineFormPlayer，移植自灾厄大修的 PRT_Line_FormPlayer）——
    /// 外形与 <see cref="DRK_Spark"/> 一样是拉伸成条状的发光丝线，逐帧缩小、减速并向透明淡出；
    /// 唯一区别在跟随方式：它每帧叠加玩家**真实的位移增量**（position - oldPosition），
    /// 而 <see cref="DRK_Spark"/> 叠加的是跟随实体的速度向量。用于熵之飞刃吐出的淡绿色丝线。
    /// </summary>
    internal class DRK_LineFormPlayer : BaseParticle
    {
        // ── 实例字段 ──
        /// <summary>
        /// 是否受重力影响（速度较低时开始下坠并横向减速）
        /// </summary>
        public bool AffectedByGravity;
        /// <summary>
        /// 要跟随的玩家，非空且存活时每帧叠加其位移增量，实现"黏在玩家身上"的效果
        /// </summary>
        public Player Owner;
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
        /// 贴图路径：Assets/Particles/DrainLineBloom
        /// </summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/DrainLineBloom";
        /// <summary>
        /// 使用加法混合，使丝线呈现发光感
        /// </summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>
        /// 使用自定义绘制
        /// </summary>
        public override bool UseCustomDraw => true;
        // ── 构造函数 ──
        /// <summary>
        /// 构造粒子：初始化位置、速度、重力开关、寿命、缩放、颜色与跟随玩家
        /// </summary>
        public DRK_LineFormPlayer(Vector2 relativePosition, Vector2 velocity, bool affectedByGravity, int lifetime, float scale, Color color, Player owner = null)
        {
            Position = relativePosition;
            Velocity = velocity;
            AffectedByGravity = affectedByGravity;
            Scale = scale;
            Lifetime = lifetime;
            // 同时记录初始色与当前色，供淡出插值使用
            Color = InitialColor = color;
            Owner = owner;
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
            // 跟随玩家：叠加本帧的位移增量（而非速度向量，故玩家被击退/悬空时也贴得住）
            if (Owner != null && Owner.active)
            {
                Position += Owner.position - Owner.oldPosition;
            }
        }
        /// <summary>
        /// 自定义绘制：用同一贴图叠画两层（外层拉长 + 内层更细），合成细长丝线外形
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            // 外层：横向压扁、纵向拉长的丝线主体
            Vector2 scale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, scale, 0, 0f);
            // 内层：更细的核心，提亮中心
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, scale * new Vector2(0.45f, 1f), 0, 0f);
        }
    }
}
