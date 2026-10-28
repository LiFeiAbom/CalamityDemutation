using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>绯红恶魔 Public 2.0.4 命中爆炸：500×500，持续 30 帧，同目标 10 帧冷却。</summary>
    internal class ScarletBlast : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 500;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.55f, 0.25f, 0f);
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                Projectile.localAI[0]++;
            }
            bool crossedX = (Projectile.velocity.X < 0f && Projectile.position.X < Projectile.ai[0])
                || (Projectile.velocity.X > 0f && Projectile.position.X > Projectile.ai[0]);
            bool crossedY = (Projectile.velocity.Y < 0f && Projectile.position.Y < Projectile.ai[1])
                || (Projectile.velocity.Y > 0f && Projectile.position.Y > Projectile.ai[1]);
            if (crossedX && crossedY)
                Projectile.Kill();
            float dustTimer = 25f;
            if (Projectile.ai[0] > 180f)
                dustTimer -= (Projectile.ai[0] - 180f) / 2f;
            if (dustTimer <= 0f)
            {
                dustTimer = 0f;
                Projectile.Kill();
            }
            dustTimer *= 0.7f;
            Projectile.ai[0] += 4f;
            int dustCount = 0;
            while (dustCount < dustTimer)
            {
                float randomX = Main.rand.Next(-30, 35);
                float randomY = Main.rand.Next(-30, 35);
                float randomSpeed = Main.rand.Next(13, 18);
                float adjustment = (float)Math.Sqrt(randomX * randomX + randomY * randomY);
                adjustment = randomSpeed / adjustment;
                randomX *= adjustment;
                randomY *= adjustment;
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Firework_Red, 0f, 0f, 100, default, 1.25f);
                Dust dust = Main.dust[dustIndex];
                dust.noGravity = true;
                dust.position = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                dust.velocity = new Vector2(randomX, randomY);
                dustCount++;
            }
        }
    }
}
