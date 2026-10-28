using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>现实撕裂大型长矛：高速、八穿、命中后每次降低 5% 伤害。</summary>
    internal class RealityRuptureLance : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/RealityRupture";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 8;
            Projectile.timeLeft = 900;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 50;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 5;
        }
        public override void AI()
        {
            Projectile.ai[0]++;
            Lighting.AddLight(Projectile.Center + Projectile.velocity * 0.6f, 0.6f, 0.2f, 0.9f);
            float radiusFactor = MathHelper.Lerp(0f, 1f, Utils.GetLerpValue(10f, 50f, Projectile.ai[0], true));
            for (int i = 0; i < 9; i++)
            {
                float offsetRotationAngle = Projectile.velocity.ToRotation() + Projectile.ai[0] / 20f;
                float radius = (20f + (float)System.Math.Cos(Projectile.ai[0] / 3f) * 12f) * radiusFactor;
                Vector2 dustPosition = Projectile.Center + offsetRotationAngle.ToRotationVector2().RotatedBy(i / 5f * MathHelper.TwoPi) * radius;
                Dust dust = Dust.NewDustPerfect(dustPosition, Main.rand.NextBool() ? DustID.PurpleTorch : DustID.WitherLightning);
                dust.noGravity = true;
                dust.velocity = Projectile.velocity * 0.8f;
                dust.scale = Main.rand.NextFloat(1.1f, 1.7f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => HitEffects();
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => HitEffects();
        private void HitEffects()
        {
            for (int i = 0; i <= 13; i++)
            {
                Vector2 sparkVelocity = Projectile.velocity.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.3f, 1.8f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, Main.rand.NextBool(4) ? DustID.PurpleTorch : DustID.WitherLightning, sparkVelocity, 0, default, Main.rand.NextFloat(2.2f, 3.5f));
                dust.noGravity = true;
                dust.fadeIn = 0.5f;
            }
            SoundEngine.PlaySound(CalamityDemutationSounds.DevourerSegmentBreak2, Projectile.Center);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.95f));
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 9; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.WitherLightning, Projectile.oldVelocity.X * 0.3f, Projectile.oldVelocity.Y * 0.3f, 0, default, Main.rand.NextFloat(1.2f, 1.6f));
        }
    }
}
