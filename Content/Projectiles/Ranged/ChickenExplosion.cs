using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 小鸡火箭的巨型爆炸——取灾厄 2.0.4.3 的 1000×1000 判定，并附加 300 帧龙焰。
    /// </summary>
    internal class ChickenExplosion : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 1000;
            Projectile.height = 1000;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.75f / 255f, (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0.01f / 255f);
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                Projectile.localAI[0] = 1f;
            }
            EmitDust();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "Dragonfire", 300, BuffID.OnFire);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "Dragonfire", 300, BuffID.OnFire);
        }
        private void EmitDust()
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 70; i++)
            {
                Vector2 offset = Main.rand.NextVector2Unit() * (float)Math.Pow(Main.rand.NextFloat(), 2.4D) * Projectile.Size * 0.5f;
                Vector2 velocity = offset.SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.PiOver2 * Main.rand.NextFloatDirection());
                velocity *= MathHelper.Lerp(5f, 15f, Utils.GetLerpValue(0.05f, 0.85f, (offset / Projectile.Size * 2f).Length()));
                int dustType = Main.rand.NextBool(4) ? DustID.CopperCoin : DustID.Torch;
                if (Main.rand.NextBool(7))
                    dustType = DustID.Smoke;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, dustType, velocity);
                dust.scale = Main.rand.NextFloat(0.85f, 1.3f);
                dust.noGravity = true;
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float radius = Projectile.Size.Length() * 0.5f;
            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }
    }
}
