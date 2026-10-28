using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>焦土母火箭的 150 半径圆形直伤区，命中附加五秒破晓。</summary>
    internal class ScorchedEarthBlast : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        private const int Lifetime = 20;
        private const float ExplosionRadius = 150f;
        private const float StartDustQuantity = 36f;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 300;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
                Projectile.localAI[1] = StartDustQuantity;
            DrawExplosionDust();
            Projectile.localAI[0]++;
        }
        private void DrawExplosionDust()
        {
            if (Projectile.localAI[0] >= Lifetime - 15)
                Projectile.localAI[1]--;
            int dustCount = (int)Projectile.localAI[1];
            for (int i = 0; i < dustCount; i++)
            {
                int dustType = Main.rand.NextBool() ? DustID.Torch : DustID.CopperCoin;
                float randX = Main.rand.NextFloat(-30f, 30f);
                float randY = Main.rand.NextFloat(-30f, 30f);
                float randVelocity = Main.rand.NextFloat(5f, 19f);
                float speed = (float)Math.Sqrt(randX * randX + randY * randY);
                if (speed != 0f)
                {
                    randX *= randVelocity / speed;
                    randY *= randVelocity / speed;
                }
                int index = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType);
                Main.dust[index].position = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                Main.dust[index].velocity = new Vector2(randX, randY);
                Main.dust[index].scale = Main.rand.NextFloat(2f, 2.5f);
                Main.dust[index].noGravity = true;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(BuffID.Daybreak, 300);
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
            => target.AddBuff(BuffID.Daybreak, 300);
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle center = new((int)Projectile.Center.X, (int)Projectile.Center.Y, 1, 1);
            if (center.Intersects(targetHitbox))
                return true;
            float closest = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.TopRight()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomRight()));
            return closest <= ExplosionRadius;
        }
        public override bool? CanCutTiles() => false;
    }
}
