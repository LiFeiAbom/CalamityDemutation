using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Content.Projectiles.Healing;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>精华剥离者的追踪魂镰；按经典 1.4.2.101 保留 600 像素索敌与击杀治疗火焰。</summary>
    internal class EssenceScythe : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.aiStyle = ProjAIStyleID.Sickle;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.alpha = 55;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            AIType = ProjectileID.DeathSickle;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f, 0f, (255 - Projectile.alpha) * 0.65f / 255f);
            if (Main.rand.NextBool(3))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            if (!TryHomeIn())
                return;
        }
        private bool TryHomeIn()
        {
            float bestDistance = 600f;
            Vector2 targetCenter = Projectile.Center;
            bool foundTarget = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile, false) || !Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;
                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                targetCenter = npc.Center;
                foundTarget = true;
            }
            if (!foundTarget)
                return false;
            Vector2 desiredVelocity = (targetCenter - Projectile.Center).SafeNormalize(Vector2.Zero) * 18f;
            Projectile.velocity = (Projectile.velocity * 20f + desiredVelocity) / 21f;
            return true;
        }
        public override Color? GetAlpha(Color lightColor) => new Color(150, 0, 200, 0);
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 2;
            EssenceFlayer.ApplyGodSlayerInferno(target, 500);
            if (target.life > 0 || Projectile.owner != Main.myPlayer)
                return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * 0.1f,
                ModContent.ProjectileType<EssenceFlame>(), 0, 0f, Projectile.owner, Projectile.owner, 0f);
        }
    }
}
