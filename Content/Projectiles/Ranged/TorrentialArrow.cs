using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 洪流箭（TorrentialArrow） - 潮涌把木箭转化出的虹彩光带箭，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/TorrentialArrow.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 5×5、**穿透 -1（无限穿透）**、额外更新 2 帧、寿命 300 帧、本地无敌 10 帧、算作"箭"。
    /// 它和四色星流箭是同一族的观感：**本体不可见**（alpha 从 255 起每帧 -25 淡入），
    /// 画面是一条光带——由共用贴图 `Content/Projectiles/LaserProj`（白条）沿反向速度每 1.5 像素盖印一次叠成
    /// （<c>CDUtil.DrawBeam</c>，长度计数器走 <c>localAI[0]</c>，最多 40 像素长）。
    /// 颜色是 <c>(53, Main.DiscoG, 255)</c>——**绿通道跟着彩虹循环走**。
    /// </para>
    /// <para>
    /// 照源保留的一处**上游死分支**：<c>ai[1]</c> 非 0 时光带反向收缩、缩到 0 自毁——
    /// 但 2.0 全工程**没有任何地方**给洪流箭写过 <c>ai[1]</c>（全 2.0 里它只被潮涌一处引用），
    /// 所以实际永远是"涨到 40 就一直挂着"。
    /// </para>
    /// <para>
    /// 与源的一点差别：源的 <c>pointBlankShotDuration</c>（灾厄贴脸射击）本工程不做。
    /// </para>
    /// </summary>
    internal class TorrentialArrow : ModProjectile
    {
        /// <summary>光带渐隐总长（像素），照源 <c>DrawBeam(40f, 1.5f, …)</c> 的第一个参数</summary>
        private const float BeamLength = 40f;
        /// <summary>相邻两次盖印的间距（像素），同时是 <c>localAI[0]</c> 每帧的增长量（照源第二个参数）</summary>
        private const float BeamStep = 1.5f;
        /// <summary>淡入速度（照源每帧 -25 alpha）</summary>
        private const int AlphaFadeStep = 25;
        /// <summary>本地命中冷却（照源 10 帧）</summary>
        private const int LocalHitCooldown = 10;
        /// <summary>共用光束贴图（四色星流箭与洪流箭共用这一张，源同样写 <c>Texture => ".../LaserProj"</c>）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>
        /// 基础属性：5×5 判定、无限穿透、本地命中冷却 10 帧、额外更新 2 帧、寿命 300 帧、
        /// 射手伤害类型、算作"箭"、初始全透明（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = LocalHitCooldown;
            Projectile.arrow = true;
        }
        /// <summary>
        /// AI：淡入 + 光带长度增长到 <see cref="BeamLength"/>；
        /// <c>ai[1]</c> 非 0 时反向收缩、缩到 0 自毁（照源，含那处上游死分支）
        /// </summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= AlphaFadeStep;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += BeamStep;
                if (Projectile.localAI[0] > BeamLength)
                    Projectile.localAI[0] = BeamLength;
            }
            else
            {
                Projectile.localAI[0] -= BeamStep;
                if (Projectile.localAI[0] <= 0f)
                    Projectile.Kill();
            }
        }
        /// <summary>虹彩：蓝底 + 绿通道随彩虹循环（照源 <c>new Color(53, Main.DiscoG, 255, alpha)</c>）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(53, Main.DiscoG, 255, Projectile.alpha);
        /// <summary>自绘光带（顶掉默认绘制）</summary>
        public override bool PreDraw(ref Color lightColor) => Projectile.DrawBeam(BeamLength, BeamStep, lightColor);
    }
}
