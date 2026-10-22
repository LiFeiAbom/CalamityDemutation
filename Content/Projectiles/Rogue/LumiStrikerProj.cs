using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 月影投矛实体——普通形态沿途生成上升星尘碎片；潜伏形态维持更久、多次追踪并在命中间重新索敌。
    /// 逐弹状态全部放在 ai/localAI，避免同类弹幕共享字段。
    /// </summary>
    internal class LumiStrikerProj : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/LumiStriker";
        private ref float Timer => ref Projectile.ai[0];
        private ref float RotateTimer => ref Projectile.ai[1];
        private ref float StealthInitialized => ref Projectile.localAI[0];
        private ref float HitCounter => ref Projectile.localAI[1];
        private ref float CanRotate => ref Projectile.localAI[2];
        private ref float HomingDistanceInitialized => ref Projectile.localAI[3];
        public override void SetDefaults()
        {
            Projectile.width = 86;
            Projectile.height = 102;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 120;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 13;
        }
        public override void AI()
        {
            if (CDUtil.IsStealthStrike(Projectile, out _))
                StealthAI();
            else
                NormalAI();
        }
        private void NormalAI()
        {
            if (Main.rand.NextBool(4))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.BubbleBurst_Blue, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft % 4 == 0)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0f, -2f), ModContent.ProjectileType<LumiShard>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack * 0.25f, Projectile.owner);
        }
        private void StealthAI()
        {
            if (StealthInitialized == 0f)
            {
                Projectile.timeLeft = 1200;
                Projectile.localNPCHitCooldown = 60;
                Projectile.extraUpdates = 3;
                Projectile.penetrate = 4;
                StealthInitialized = 1f;
            }
            if (Main.rand.NextBool(4))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.BubbleBurst_Blue, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft % 15 == 0)
            {
                Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(15f, 15f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, Projectile.velocity, ModContent.ProjectileType<LumiShard>(), (int)(Projectile.damage * 0.2f), Projectile.knockBack * 0.25f, Projectile.owner, 1f, 1f);
            }
            Timer++;
            if (Timer > 2f)
            {
                Timer = 10f;
                if (CanRotate == 1f)
                {
                    RotateTimer++;
                    if (RotateTimer > 30f)
                    {
                        HitCounter = 0f;
                        Projectile.velocity = Projectile.velocity.RotatedBy(0.5f);
                        CanRotate = 0f;
                        RotateTimer = 0f;
                    }
                }
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
                if (HitCounter != 1f)
                {
                    float homingDistance = HomingDistanceInitialized == 0f ? 3600f : 1800f;
                    CDUtil.HomeInOnNPC(Projectile, true, homingDistance, 24f, 30f);
                    HomingDistanceInitialized = 1f;
                }
            }
            SpawnDust();
        }
        private void SpawnDust()
        {
            if (Main.rand.NextBool())
            {
                Vector2 offset = new Vector2(12f, 0f).RotatedByRandom(MathHelper.TwoPi);
                Vector2 velocity = Projectile.velocity * 0.4f + new Vector2(4f, 0f).RotatedBy(offset.ToRotation()) * 1.25f;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset * 1.05f, DustID.BubbleBurst_Blue, velocity, 100, default, 1.2f);
                dust.noGravity = true;
            }
            if (Main.rand.NextBool(6))
            {
                Vector2 offset = new Vector2(12f, 0f).RotatedByRandom(MathHelper.TwoPi);
                Vector2 velocity = Projectile.velocity * 0.5f + new Vector2(4f, 0f).RotatedBy(offset.ToRotation()) * 1.25f;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset * 1.05f, DustID.BubbleBurst_Purple, velocity, 100, default, 1.2f);
                dust.noGravity = true;
            }
        }
        public override bool? CanDamage()
        {
            if (!CDUtil.IsStealthStrike(Projectile, out _))
                return null;
            return Timer > 5f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CDUtil.IsStealthStrike(Projectile, out _))
            {
                HitCounter++;
                if (Projectile.extraUpdates > 1)
                    Projectile.extraUpdates--;
                Projectile.velocity *= 0.94f;
                CanRotate = 1f;
                target.immune[Projectile.owner] = 5;
            }
            SpawnHitShards(0.5f);
            SoundEngine.PlaySound(CalamityDemutationSounds.WulfrumKnifeTileHit2, target.Center);
            SpawnHitSparks();
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            SpawnHitShards(0.5f);
            SoundEngine.PlaySound(CalamityDemutationSounds.WulfrumKnifeTileHit2, target.Center);
        }
        private void SpawnHitShards(float damageMultiplier)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            for (int i = 0; i < 7; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(50f, 50f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(6f, 12f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<LumiShard>(), (int)(Projectile.damage * damageMultiplier), Projectile.knockBack * 0.25f, Projectile.owner);
            }
        }
        private void SpawnHitSparks()
        {
            if (Main.dedServ)
                return;
            int sparkCount = Main.rand.Next(4, 6);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 velocity = Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.6f, 1.1f);
                Color color = Color.Lerp(Color.DarkSlateBlue, Color.DarkBlue, Main.rand.NextFloat(0.7f));
                color = Color.Lerp(color, Color.Gold, Main.rand.NextFloat());
                GlowSparkCal spark = new();
                DRKLoader.NewParticle(spark, Projectile.Center, velocity, color, Main.rand.NextFloat(0.8f, 1f) * 0.955f);
                spark.Configure(false, Main.rand.Next(23, 25), new Vector2(0.5f, 1.6f));
            }
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.BubbleBurst_Blue, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
        }
    }
}
