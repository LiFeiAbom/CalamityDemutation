using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>CI 2.0.4.19 龙炎投矛链：长矛命中时立刻喷出三枚追踪火球，终点再结算 160 范围伤害。</summary>
    internal class WrathwingSpear : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 62;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.timeLeft = 300;
        }
        public override bool? CanHitNPC(NPC target) => Projectile.localAI[0] > 10f;
        public override void AI()
        {
            Projectile.localAI[0] += 2f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 4)
                Projectile.frame = 0;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            CDUtil.HomeInOnNPC(Projectile, false, 800f, 25f, 20f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 360);
            SpawnFireballs();
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Daybreak, 360);
            SpawnFireballs();
        }
        private void SpawnFireballs()
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            int fireballType = ModContent.ProjectileType<WrathwingFireball>();
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = new Vector2(0f, -26f)
                    .RotatedBy(Main.rand.NextFloat(-0.6f + i / 10f, 0.7f + i / 10f)) * 1.1f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, velocity,
                    fireballType, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
        }
        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = 160;
            Projectile.position = center - Projectile.Size * 0.5f;
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item14, center);
            for (int i = 0; i < 20; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.CopperCoin, 0f, 0f, 100, default, 1.2f);
                Main.dust[dustIndex].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[dustIndex].scale = 0.5f;
                    Main.dust[dustIndex].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 30; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.CopperCoin, 0f, 0f, 100, default, 1.7f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 5f;
                dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.CopperCoin, 0f, 0f, 100, default, 1f);
                Main.dust[dustIndex].velocity *= 2f;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
