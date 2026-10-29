using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 灵魂收割者发射的灵魂镰刀；按经典 1.4.2.101 保留闪烁、全局无敌帧与低血爆炸。
    /// </summary>
    internal class SoulScythe : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.aiStyle = ProjAIStyleID.Sickle;
            Projectile.alpha = 55;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 420;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            AIType = ProjectileID.DeathSickle;
        }
        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale -= 0.02f;
                Projectile.alpha += 30;
                if (Projectile.alpha >= 250)
                {
                    Projectile.alpha = 255;
                    Projectile.localAI[0] = 1f;
                }
            }
            else
            {
                Projectile.scale += 0.02f;
                Projectile.alpha -= 30;
                if (Projectile.alpha <= 0)
                {
                    Projectile.alpha = 0;
                    Projectile.localAI[0] = 0f;
                }
            }
            Lighting.AddLight(Projectile.Center, 0f, (255 - Projectile.alpha) * 0.5f / 255f, 0f);
            if (Main.rand.NextBool(2))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.CursedTorch, Projectile.velocity.X * 1.5f, Projectile.velocity.Y * 1.5f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 6;
            target.AddBuff(BuffID.OnFire, 200);
            SoulHarvester.ApplyPlague(target, 200);
            if (target.life > target.lifeMax * 0.15f)
                return;
            SoundEngine.PlaySound(SoundID.Item14, target.position);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<HiveBombExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 60;
            Projectile.position.X -= Projectile.width / 2f;
            Projectile.position.Y -= Projectile.height / 2f;
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemEmerald, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 50; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemEmerald, 0f, 0f, 100, default, 3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemEmerald, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 2f;
            }
        }
    }
}
