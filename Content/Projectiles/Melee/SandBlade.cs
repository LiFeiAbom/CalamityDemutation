using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>遗弃之刃射出的沙刃：先减速、后加速，循环 60 帧。</summary>
    internal class SandBlade : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Melee;
        }
        public override void AI()
        {
            Projectile.rotation += 0.5f;
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] <= 30f)
            {
                Projectile.velocity *= 0.925f;
            }
            else if (Projectile.ai[1] <= 59f)
            {
                Projectile.velocity *= 1.15f;
            }
            else if (Projectile.ai[1] == 60f)
            {
                Projectile.ai[1] = 0f;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile,
                ProjectileID.Sets.TrailingMode[Type], lightColor, 2);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width,
                    Projectile.height, DustID.Teleporter);
        }
    }
}
