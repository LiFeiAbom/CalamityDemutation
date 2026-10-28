using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>CI 2.0.4.19 的追踪火球，使用 CI DragonSpearFlare 单帧贴图。</summary>
    internal class WrathwingFireball : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = 84;
            Projectile.height = 96;
            Projectile.scale = 1.5f;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.penetrate = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 200;
        }
        public override bool? CanHitNPC(NPC target) => Projectile.localAI[1] > 15f;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }
        public override void AI()
        {
            if (Main.rand.NextBool(2))
                SpawnTrailDust();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y += 0.28f;
            Projectile.velocity.X *= 0.99f;
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > 30f && Projectile.velocity.Y < 0f)
                Projectile.velocity.Y += 0.42f;
            Projectile.localAI[1]++;
            if (Projectile.localAI[1] >= 10f)
                CDUtil.HomeInOnNPC(Projectile, true, 1800f, 24f, 20f);
            if (Projectile.localAI[1] < 20f)
                Projectile.timeLeft = 200;
        }
        private void SpawnTrailDust()
        {
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.Pixie, Main.rand.NextVector2Circular(1f, 1f), 0, default, 0.5f);
                dust.noGravity = true;
            }
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item34, Projectile.position);
            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 8f) * 2f;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.HeatRay, velocity,
                    100, default, 1.5f);
                dust.noGravity = true;
            }
        }
    }
}
