using System;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 远古之怒的追踪弹 —— 移植自灾厄 2.0 的 <c>Projectiles/Magic/GraniteEnergy.cs</c>。
    /// 12×12 判定、穿透 1、90 帧寿命；前 30 帧不能命中，后 60 帧在 600 像素内追踪。
    /// 源的 <c>CalamityGlobalProjectile.HomeInOnNPC</c> 换成本工程同签名的 <c>CDUtil.HomeInOnNPC</c>。
    /// </summary>
    internal class GraniteEnergy : ModProjectile
    {
        /// <summary>基础属性照 2.0</summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 90;
        }
        /// <summary>前 30 帧只是飞行，后 60 帧才允许命中（照 2.0）</summary>
        public override bool? CanHitNPC(NPC target) => Projectile.timeLeft < 60 && target.CanBeChasedBy(Projectile);
        /// <summary>朝向速度方向、少量 229 尘、最后 60 帧追踪</summary>
        public override void AI()
        {
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + MathHelper.ToRadians(90);
            if (Main.rand.NextBool(2))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0f, 0f, 100, default, 0.6f);
            }
            if (Projectile.timeLeft < 60)
            {
                CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, 600f, 12f, 20f);
            }
        }
    }
}
