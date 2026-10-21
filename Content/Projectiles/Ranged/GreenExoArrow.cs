using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 绿色星流箭（GreenExoArrow） - 天堂之风把木箭转化出的四色星流箭之一，＝旧版支线的
    /// <c>Content/Projectiles/ExoLore/ExoArrowGreenExoLore</c>（本工程常驻 Lore 模式，故不留非 Lore 版）。
    /// <para>
    /// 定位是"落点卷起星流风暴"：判定 5×5、穿透 1、额外更新 3 帧、寿命 300 帧；
    /// **Lore 分支让它一路追踪敌人**（`CDUtil.HomeInOnNPC`：1000 像素内、追踪速度 16、惯性 18——
    /// 非 Lore 分支没有这一步）。
    /// </para>
    /// <para>
    /// 消亡时原地留下一枚 <see cref="ExoMark"/>（印记再于第 60 帧放出 <see cref="ExoTornado"/> 龙卷），
    /// 因生成弹幕写在主人端判据内，联机时只有主人端会补这一枚。
    /// 观感是一条绿色光束（共用 <c>Content/Projectiles/LaserProj</c> + <c>CDUtil.DrawBeam</c>）。
    /// </para>
    /// </summary>
    internal class GreenExoArrow : ModProjectile
    {
        /// <summary>光束渐隐总长（像素），照源 <c>DrawBeam(40f, 1.5f, …)</c> 的第一个参数</summary>
        private const float BeamLength = 40f;
        /// <summary>相邻两次盖印的间距（像素），同时是 <c>localAI[0]</c> 每帧的增长量（照源第二个参数）</summary>
        private const float BeamStep = 1.5f;
        /// <summary>共用的光束贴图（见 <see cref="TealExoArrow"/> 的说明）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>基础属性：5×5、穿透 1、额外更新 3 帧、寿命 300 帧、射手伤害类型（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
        }
        /// <summary>
        /// AI：淡入 + 光束长度增长到 <see cref="BeamLength"/>；<c>ai[1]</c> 非 0 时反向收缩、缩到 0 自毁；
        /// 另外照 Lore 分支每帧转向 1000 像素内最近的敌人（追踪）
        /// </summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
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

            // Lore 分支专属：追踪（照源 CalamityUtils.HomeInOnNPC(Projectile, !tileCollide, 1000f, 16f, 18f)）
            CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, 1000f, 16f, 18f);
        }
        /// <summary>命中敌人：挂整套星云系减益（照源 <c>target.ExoDebuffs()</c>）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.ExoDebuffs();
        /// <summary>PvP 命中：同上（源的 <c>OnHitPvp</c> 在 1.4.4 起并入此钩子）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.ExoDebuffs();
        /// <summary>绿色（照源 <c>new Color(0, 250, 0)</c>）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(0, 250, 0, Projectile.alpha);
        /// <summary>自绘光束（顶掉默认绘制）</summary>
        public override bool PreDraw(ref Color lightColor) => Projectile.DrawBeam(BeamLength, BeamStep, lightColor);
        /// <summary>
        /// 消亡：在原地留下一枚 <see cref="ExoMark"/>（伤害 / 击退照弹幕自身，零速度静置），
        /// 照源只在主人端生成
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<ExoMark>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
    }
}
