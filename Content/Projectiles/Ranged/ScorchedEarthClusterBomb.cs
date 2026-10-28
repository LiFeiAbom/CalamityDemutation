using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>焦土的集束炸弹：散开后减速，终爆时再拆出 2-3 枚火箭火焰。</summary>
    internal class ScorchedEarthClusterBomb : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -2;
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.95f;
            if (Projectile.velocity.Length() < 0.5f && Projectile.timeLeft > 10)
                Projectile.timeLeft = 10;
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            for (int i = 0; i < 10; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.CopperCoin, 0f, 0f, 0, default, 1f);
            if (Projectile.owner == Main.myPlayer)
            {
                int count = Main.rand.Next(2, 4);
                for (int i = 0; i < count; i++)
                {
                    Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                        ModContent.ProjectileType<RocketFire>(), Projectile.damage, 0f, Projectile.owner);
                }
            }
        }
    }
}
