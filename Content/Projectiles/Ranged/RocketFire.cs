using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>焦土集束炸弹拆出的火焰碎片，复用本工程 TotalityFire 三帧贴图。</summary>
    internal class RocketFire : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/Rogue/TotalityFire";
        private bool initialized;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 2;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
        }
        public override void AI()
        {
            if (Projectile.ai[1] > 0f)
                Projectile.rotation = -Projectile.velocity.X * 0.05f + MathHelper.PiOver2;
            else
                Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.ai[1]--;
            if (!initialized)
            {
                initialized = true;
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Type])
                Projectile.frame = 0;
            if (float.IsNaN(Projectile.velocity.X))
                Projectile.velocity.X *= -0.1f;
            if (float.IsNaN(Projectile.velocity.Y) && Projectile.velocity.Y > 1f)
                Projectile.velocity.Y *= -0.5f;
            Projectile.ai[0]++;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 5f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X *= 0.97f;
                    if (Projectile.velocity.X > -0.01f && Projectile.velocity.X < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.2f;
            }
            if (Projectile.velocity.Y < 0.25f && Projectile.velocity.Y > 0.15f)
                Projectile.velocity.X *= 0.8f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;
            if (Main.rand.NextBool(4))
            {
                int index = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1f);
                Dust dust = Main.dust[index];
                dust.position.X -= 2f;
                dust.position.Y += 2f;
                dust.scale += Main.rand.Next(50) * 0.01f;
                dust.noGravity = true;
                dust.velocity.Y -= 2f;
            }
            if (Main.rand.NextBool(10))
            {
                int index = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1f);
                Dust dust = Main.dust[index];
                dust.position.X -= 2f;
                dust.position.Y += 2f;
                dust.scale += 0.3f + Main.rand.Next(50) * 0.01f;
                dust.noGravity = true;
                dust.velocity *= 0.1f;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.ai[1] = 10f;
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(BuffID.OnFire, 120);
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
            => target.AddBuff(BuffID.OnFire, 120);
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
