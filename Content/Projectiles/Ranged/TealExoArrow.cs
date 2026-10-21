using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 青色星流箭（TealExoArrow） - 天堂之风把木箭转化出的四色星流箭之一，＝旧版支线的
    /// <c>Content/Projectiles/ExoLore/ExoArrowTealExoLore</c>（本工程常驻 Lore 模式，故不留非 Lore 版）。
    /// <para>
    /// 定位是"无限穿透 + 无视无敌帧"：判定 5×5、穿透 -1、额外更新 3 帧、寿命 300 帧，
    /// 且**本地命中冷却为 0**——源注释写的 "ignore immunity frames" 指的就是它。
    /// （非 Lore 分支那套 10 帧冷却与"青色箭伤害 ×0.66"的惩罚在 Lore 分支里都没有，本工程照 Lore 分支。）
    /// </para>
    /// <para>
    /// 观感：本体不可见（alpha 从 255 起每帧 -25 淡入），画面是一条青色光束，
    /// 由共用贴图 `Content/Projectiles/LaserProj`（6×10 白条）沿反向速度每 <see cref="BeamStep"/> 像素盖印一次叠成
    /// （<c>CDUtil.DrawBeam</c>，长度计数器走 <c>localAI[0]</c>）。命中挂整套星云系减益。
    /// </para>
    /// </summary>
    internal class TealExoArrow : ModProjectile
    {
        /// <summary>光束渐隐总长（像素），照源 <c>DrawBeam(40f, 1.5f, …)</c> 的第一个参数</summary>
        private const float BeamLength = 40f;
        /// <summary>相邻两次盖印的间距（像素），同时是 <c>localAI[0]</c> 每帧的增长量（照源第二个参数）</summary>
        private const float BeamStep = 1.5f;
        /// <summary>
        /// 共用光束贴图：本工程把灾厄 / 旧版支线的 `Projectiles/LaserProj` 放在 Projectiles 根目录（与 InvisibleProj 同一处），
        /// 四色星流箭与后续箭共用这一张
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>基础属性：5×5、穿透 -1（无限）、本地命中冷却 0、额外更新 3 帧、寿命 300 帧、射手伤害类型（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
        }
        /// <summary>
        /// AI：淡入 + 光束长度增长到 <see cref="BeamLength"/>；<c>ai[1]</c> 非 0 时反向收缩、缩到 0 自毁
        /// （源给四色星流箭都写了这套同一个模板，本件也不例外）
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
        }
        /// <summary>命中敌人：挂整套星云系减益（照源 <c>target.ExoDebuffs()</c>）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.ExoDebuffs();
        /// <summary>PvP 命中：同上（源的 <c>OnHitPvp</c> 在 1.4.4 起并入此钩子）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.ExoDebuffs();
        /// <summary>青色（照源 <c>new Color(0, 100, 100)</c>）；alpha 由本体控制，盖印时会被强制清零</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(0, 100, 100, Projectile.alpha);
        /// <summary>自绘光束（顶掉默认绘制）</summary>
        public override bool PreDraw(ref Color lightColor) => Projectile.DrawBeam(BeamLength, BeamStep, lightColor);
    }
}
