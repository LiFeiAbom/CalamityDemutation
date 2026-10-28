using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>现实碎片：短暂减速后在 350 像素内追踪，命中一次后后续伤害下降。</summary>
    internal class RealityRuptureMini : ModProjectile
    {
        private int framesInAir;
        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 800;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            framesInAir++;
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            if (framesInAir < 120 || Projectile.numHits > 0 || !Main.rand.NextBool(2))
            {
                if (framesInAir < 120)
                    Lighting.AddLight(Projectile.Center + Projectile.velocity * 0.6f, 0.6f, 0.2f, 0.5f);
            }
            else
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, DustID.WitherLightning, Projectile.velocity * 0.01f, 0, Color.Plum * 0.5f, 1.3f);
                dust.noGravity = true;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Vector2 targetCenter = Projectile.Center;
            float maxDistance = 350f;
            bool foundTarget = false;
            foreach (NPC target in Main.ActiveNPCs)
            {
                if (!target.CanBeChasedBy(Projectile, false))
                    continue;
                float extraDistance = target.width * 0.5f + target.height * 0.5f;
                bool canHit = stealthStrike || Collision.CanHit(Projectile.Center, 1, 1, target.Center, 1, 1);
                if (Vector2.Distance(target.Center, Projectile.Center) < maxDistance + extraDistance && canHit)
                {
                    targetCenter = target.Center;
                    foundTarget = true;
                    break;
                }
            }
            if (!foundTarget)
                return;
            Projectile.extraUpdates = 4;
            Vector2 direction = Projectile.SafeDirectionTo(targetCenter, Vector2.UnitY);
            Projectile.velocity = (Projectile.velocity * 20f + direction * 12f) / 21f;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i <= 2; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.WitherLightning, Projectile.oldVelocity.X * Main.rand.NextFloat(1.1f, 1.3f), Projectile.oldVelocity.Y * Main.rand.NextFloat(1.1f, 1.3f));
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            for (int i = 0; i <= 2; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.WitherLightning, Projectile.oldVelocity.X * Main.rand.NextFloat(1.1f, 1.3f), Projectile.oldVelocity.Y * Main.rand.NextFloat(1.1f, 1.3f));
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.8f));
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.WulfrumKnifeTileHit2, Projectile.position);
        }
    }
}
