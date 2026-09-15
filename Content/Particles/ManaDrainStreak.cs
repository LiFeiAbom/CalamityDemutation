using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 玛那吸取拉线粒子（移植自灾厄的 ManaDrainStreak）——变身动画期间从主人身上拉出的细线。
    /// 起点固定挂在主人身上（或 OverridePosition），沿线朝 startVector 的方向由 StartDistance 滑向 FinalDistance
    /// （进度按 LifetimeCompletion 的平方推进），颜色从 StartColor 插值到 EndColor，并随线移动施加微光。
    /// 贴图取灾厄的 DrainLineBloom（细长渐隐条），加法混合。
    /// </summary>
    internal class ManaDrainStreak:BaseParticle
    {
        // ── 实例字段 ──
        /// <summary>挂线的主人：每帧取它的位置作为线的起点（死亡/失效则不再更新位置）</summary>
        public Player Owner;
        /// <summary>线的起点距离主人的距离（即构造时 startVector 的长度）</summary>
        public float StartDistanceFromPlayer;
        /// <summary>线的终点距离主人的距离</summary>
        public float FinalDistanceFromPlayer;
        /// <summary>起点颜色（初始色）与终点颜色（寿命末端的插值目标）</summary>
        public Color StartColor;
        public Color EndColor;
        /// <summary>非默认值时改用这个固定点当线的起点（不再跟随主人）</summary>
        public Vector2 OverridePosition;
        /// <summary>末端是否淡出（插值到 A=0 的终点色）</summary>
        public bool FadeOut;
        // ── 属性 ──
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>贴图路径：Assets/Particles/DrainLineBloom（源自灾厄）</summary>
        public override string Texture => "CalamityDemutation/Assets/Particles/DrainLineBloom";
        /// <summary>使用加法混合，使拉线发光</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>使用自定义绘制（按当前/上一段位移裁剪出线长）</summary>
        public override bool UseCustomDraw => true;
        // ── 构造函数 ──
        /// <summary>
        /// 构造粒子：thickness 为线宽缩放，startVector 决定线的起点距离与朝向（长度与角度都被取用），
        /// finalDistance 为终点距离，寿命 lifetime，颜色 start→end 插值
        /// </summary>
        public ManaDrainStreak(Player owner, float thickness, Vector2 startVector, float finalDistance, Color colorStart, Color colorEnd, int lifetime, Vector2 overridePosition = default, bool fadeOut = false)
        {
            Owner = owner;
            Scale = thickness;
            Velocity = Vector2.Zero;
            Rotation = startVector.ToRotation();
            StartDistanceFromPlayer = startVector.Length();
            FinalDistanceFromPlayer = finalDistance;
            StartColor = colorStart;
            EndColor = colorEnd;
            Color = colorStart;
            Lifetime = lifetime;
            OverridePosition = overridePosition;
            FadeOut = fadeOut;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 每帧更新：主人失效则原地不动；否则把位置钉在"主人中心 + 朝向 × 当前距离"
        /// （进度取 LifetimeCompletion 的平方），并按同进度插值颜色、施加微光
        /// </summary>
        public override void AI()
        {
            if (Owner == null || !Owner.active || Owner.dead)
                return;
            Vector2 setPosition = OverridePosition != default ? OverridePosition : Owner.MountedCenter;
            Position = setPosition + Rotation.ToRotationVector2() * MathHelper.Lerp(StartDistanceFromPlayer, FinalDistanceFromPlayer, (float)Math.Pow(LifetimeCompletion, 2));
            Color = Color.Lerp(StartColor, FadeOut ? EndColor with { A = 0 } : EndColor, LifetimeCompletion);
            Lighting.AddLight(Position, Color.ToVector3() * 0.2f);
        }
        /// <summary>
        /// 自定义绘制：只画"上一段"走过的这一小截（当前距离与 0.2 进度前距离之差），
        /// 于是整条线随着进度推进被一截截画出来，形成流动的拉线
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            float currentDisplace = MathHelper.Lerp(StartDistanceFromPlayer, FinalDistanceFromPlayer, (float)Math.Pow(LifetimeCompletion, 2));
            float earlierDisplace = MathHelper.Lerp(StartDistanceFromPlayer, FinalDistanceFromPlayer, (float)Math.Pow(Math.Clamp(LifetimeCompletion - 0.2f, 0f, 1f), 2));
            float length = (currentDisplace - earlierDisplace) / tex.Height;
            Vector2 scale = new Vector2(Scale, length);
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height);
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color, Rotation - MathHelper.PiOver2, origin, scale, SpriteEffects.None, 0);
        }
    }
}
