using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>现实粉碎长矛的 200 像素圆形爆炸判定。</summary>
    internal class RealityRuptureStealthExplosion : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        private const float ExplosionRadius = 200f;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float targetRadius = System.Math.Max(targetHitbox.Width, targetHitbox.Height) * 0.5f;
            Vector2 targetCenter = new(targetHitbox.Center.X, targetHitbox.Center.Y);
            return Vector2.Distance(Projectile.Center, targetCenter) <= ExplosionRadius + targetRadius;
        }
    }
}
