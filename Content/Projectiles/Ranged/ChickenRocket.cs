using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 小鸡火箭——取灾厄 2.0.4.3：轻微重力、持续加速，撞墙后向上飞并延迟爆炸。
    /// </summary>
    internal class ChickenRocket : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
        }
        public override void AI()
        {
            float speed = Projectile.velocity.Length();
            if (speed >= 12f)
            {
                for (int i = 0; i < 2; i++)
                {
                    float offsetX = i == 1 ? Projectile.velocity.X * 0.5f : 0f;
                    float offsetY = i == 1 ? Projectile.velocity.Y * 0.5f : 0f;
                    Vector2 dustPosition = new Vector2(Projectile.position.X + 3f + offsetX, Projectile.position.Y + 3f + offsetY) - Projectile.velocity * 0.5f;
                    int dust = Dust.NewDust(dustPosition, Projectile.width - 8, Projectile.height - 8, DustID.Torch, 0f, 0f, 100, default, 1f);
                    Main.dust[dust].scale *= 2f + Main.rand.NextFloat();
                    Main.dust[dust].velocity *= 0.2f;
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(dustPosition, Projectile.width - 8, Projectile.height - 8, DustID.CopperCoin, 0f, 0f, 100, default, 0.5f);
                    Main.dust[dust].fadeIn = 1f + Main.rand.NextFloat(0.5f);
                    Main.dust[dust].velocity *= 0.05f;
                }
                if (speed < 18f)
                    Projectile.velocity *= 1.006f;
                else if (Main.rand.NextBool())
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.CopperCoin, 0f, 0f, 100, default, 1f);
                    Main.dust[dust].scale = 0.1f + Main.rand.NextFloat(0.5f);
                    Main.dust[dust].fadeIn = 1.5f + Main.rand.NextFloat(0.5f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].position = Projectile.Center + new Vector2(0f, -Projectile.height / 2f).RotatedBy(Projectile.rotation) * 1.1f;
                    dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1f);
                    Main.dust[dust].scale = 1f + Main.rand.NextFloat(0.5f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].position = Projectile.Center + new Vector2(0f, -(Projectile.height / 2f + 6f)).RotatedBy(Projectile.rotation) * 1.1f;
                }
            }
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y += 0.09f;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity.X = 0f;
            Projectile.velocity.Y = -15f;
            if (Projectile.timeLeft > 20)
                Projectile.timeLeft = 20;
            else
                return true;
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 1040;
            Projectile.position -= Projectile.Size * 0.5f;
            if (Projectile.owner == Main.myPlayer)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ChickenExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            for (int i = 0; i < 40; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.NextFloat();
                }
            }
            for (int i = 0; i < 70; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 2f;
            }
        }
    }
}
