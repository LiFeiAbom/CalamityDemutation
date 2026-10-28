using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>焦土的母火箭：400 像素全程追踪，终爆后生成大范围爆炸与五枚集束炸弹。</summary>
    internal class ScorchedEarthRocket : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 10;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 1f, 0.79f, 0.3f);
            Projectile.frameCounter++;
            if (Projectile.frameCounter > (Projectile.frame == 1 ? 10 : 7))
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Type])
                Projectile.frame = 6;
            CDUtil.HomeInOnNPC(Projectile, true, 400f, 5f, 30f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            float xVel = Projectile.velocity.X * 0.5f;
            float yVel = Projectile.velocity.Y * 0.5f;
            int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X + 3f + xVel, Projectile.position.Y + 3f + yVel) - Projectile.velocity * 0.5f,
                Projectile.width - 8, Projectile.height - 8, DustID.CopperCoin, 0f, 0f, 100, default, 1f);
            Main.dust[dustIndex].scale *= 2f + Main.rand.Next(10) * 0.1f;
            Main.dust[dustIndex].velocity *= 0.2f;
            Main.dust[dustIndex].noGravity = true;
            dustIndex = Dust.NewDust(new Vector2(Projectile.position.X + 3f + xVel, Projectile.position.Y + 3f + yVel) - Projectile.velocity * 0.5f,
                Projectile.width - 8, Projectile.height - 8, DustID.CopperCoin, 0f, 0f, 100, default, 0.5f);
            Main.dust[dustIndex].fadeIn = 1f + Main.rand.Next(5) * 0.1f;
            Main.dust[dustIndex].velocity *= 0.05f;
        }
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            SetHitboxSize(300);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            if (Main.netMode != NetmodeID.Server)
            {
                Vector2 source = Projectile.Center - new Vector2(24f, 24f);
                for (int i = 0; i < 10; i++)
                {
                    float velocityMultiplier = i < 3 ? 0.66f : i >= 7 ? 1f : 0.33f;
                    for (int j = 0; j < 4; j++)
                    {
                        int smoke = Gore.NewGore(Projectile.GetSource_Death(), source, Vector2.Zero, Main.rand.Next(61, 64), 1f);
                        Gore gore = Main.gore[smoke];
                        gore.velocity *= velocityMultiplier;
                        gore.velocity.X += j % 2 == 0 ? 1f : -1f;
                        gore.velocity.Y += j < 2 ? 1f : -1f;
                    }
                }
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<ScorchedEarthBlast>(), Projectile.damage, Projectile.knockBack * 2f, Projectile.owner);
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(8f, 10f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                    ModContent.ProjectileType<ScorchedEarthClusterBomb>(), (int)(Projectile.damage * 0.25f), Projectile.knockBack * 0.25f, Projectile.owner);
            }
        }
        private void SetHitboxSize(int size)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = size;
            Projectile.position = center - Projectile.Size * 0.5f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
