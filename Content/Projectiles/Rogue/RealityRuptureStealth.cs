using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>潜行现实粉碎长矛：持续加速，命中时生成 200 像素半径的现实爆炸。</summary>
    internal class RealityRuptureStealth : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/RealityRupture";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 43;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 900;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 50;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 7;
        }
        public override void AI()
        {
            Projectile.velocity *= 1.003f;
            Projectile.scale = 1.3f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.209f, 0.140f, 0.202f) * 8f);
            if (Projectile.ai[0]++ > 10f && Vector2.Distance(Main.player[Projectile.owner].Center, Projectile.Center) < 1400f)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity, DustID.PurpleTorch, Projectile.velocity * 0.01f, 0, Color.Plum * 0.6f, 3.4f);
                dust.noGravity = true;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => HitEffects();
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => HitEffects();
        private void HitEffects()
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.RealityRuptureStealthHit, Projectile.Center);
            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(20f));
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, velocity, 0, Color.Orchid, Main.rand.NextFloat(1.8f, 2.3f));
                dust.noGravity = true;
            }
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<RealityRuptureStealthExplosion>(),
                Projectile.damage / 2,
                Projectile.knockBack * 2f,
                Projectile.owner);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 15; i++)
            {
                Vector2 velocity = Projectile.velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.6f, 1.5f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity, DustID.PurpleTorch, velocity.RotatedByRandom(0.1f), 0, default, Main.rand.NextFloat(1.2f, 1.5f));
                dust.noGravity = true;
                Dust.NewDustPerfect(Projectile.Center, DustID.WitherLightning, velocity, 0, Color.Plum * 0.8f, Main.rand.NextFloat(2.2f, 3f)).noGravity = true;
            }
        }
    }
}
