using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 蓝色星流箭的后续箭（BlueExoArrow2） - 蓝色星流箭命中时由「弹幕齐射」撒出的那串追踪箭，＝CI 的
    /// <c>ExoArrowBlue2</c>。
    /// <para>
    /// 判定 5×5、穿透 1、**额外更新 5 帧**（比四色星流箭更快，观感是"窜出去的一串"）、
    /// 不撞地形、寿命 300 帧；没有命中副作用，伤害由撒它的人按倍率传入。
    /// 观感同样是共用贴图叠出的蓝色光束。
    /// </para>
    /// </summary>
    internal class BlueExoArrow2 : ModProjectile
    {
        /// <summary>光束渐隐总长（像素），照源 <c>DrawBeam(40f, 1.5f, …)</c> 的第一个参数</summary>
        private const float BeamLength = 40f;
        /// <summary>相邻两次盖印的间距（像素），同时是 <c>localAI[0]</c> 每帧的增长量（照源第二个参数）</summary>
        private const float BeamStep = 1.5f;
        /// <summary>共用的光束贴图（见 <see cref="TealExoArrow"/> 的说明）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>基础属性：5×5、穿透 1、额外更新 5 帧、不撞地形、寿命 300 帧、射手伤害类型（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
        }
        /// <summary>AI：淡入 + 光束长度增长到 <see cref="BeamLength"/>；<c>ai[1]</c> 非 0 时反向收缩、缩到 0 自毁（照源）</summary>
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
        /// <summary>蓝色（与蓝色星流箭同色）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(0, 0, 250, Projectile.alpha);
        /// <summary>自绘光束（顶掉默认绘制）</summary>
        public override bool PreDraw(ref Color lightColor) => Projectile.DrawBeam(BeamLength, BeamStep, lightColor);
    }
}
