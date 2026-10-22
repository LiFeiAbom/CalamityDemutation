using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 命中爆发的幻影灵魂——伤害从低比例逐步恢复，延迟后追踪敌人，消亡时扩大判定再炸一次。
    /// </summary>
    internal class PhantasmalSoul : ModProjectile
    {
        private ref float OriginDamage => ref Projectile.localAI[0];
        private ref float Divider => ref Projectile.localAI[1];
        private ref float Initialized => ref Projectile.localAI[2];
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.extraUpdates = 2;
            Projectile.penetrate = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 600;
        }
        public override void AI()
        {
            if (Initialized == 0f)
            {
                OriginDamage = Projectile.damage;
                Divider = 10f;
                Projectile.damage = 0;
                Initialized = 1f;
            }
            else if (Projectile.timeLeft % 5 == 0 && Divider > 0f)
            {
                Projectile.damage = (int)(OriginDamage / Divider);
                Divider--;
            }
            else if (Divider <= 0f)
            {
                Projectile.damage = (int)OriginDamage;
            }
            if (CDUtil.IsStealthStrike(Projectile, out _))
                Projectile.extraUpdates = 4;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            if (Projectile.timeLeft <= 560)
                Projectile.ai[0] = 0f;
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 0, default, 1f);
            Main.dust[dust].velocity *= 0.1f;
            Main.dust[dust].scale = 1.3f;
            Main.dust[dust].noGravity = true;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.9f);
            if (Main.player[Projectile.owner].active && !Main.player[Projectile.owner].dead)
            {
                if (Projectile.ai[0] == 0f)
                    CDUtil.HomeInOnNPC(Projectile, true, 3600f, 10f, 20f);
            }
            else if (Projectile.timeLeft > 30)
            {
                Projectile.timeLeft = 30;
            }
        }
        public override bool? CanHitNPC(NPC target) => Projectile.timeLeft < 590 && target.CanBeChasedBy(Projectile, false);
        public override bool CanHitPvp(Player target) => Projectile.timeLeft < 590;
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 595)
                return false;
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 85)
            {
                byte brightness = (byte)(Projectile.timeLeft * 3);
                byte alpha = (byte)(100f * (brightness / 255f));
                return new Color(brightness, brightness, brightness, alpha);
            }
            return new Color(255, 255, 255, 100);
        }
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 64;
            Projectile.position -= Projectile.Size * 0.5f;
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.damage /= 4;
            Projectile.Damage();
            for (int i = 0; i < 36; i++)
            {
                Vector2 offset = (Vector2.UnitY * new Vector2(Projectile.width * 0.5f, Projectile.height * 0.75f)).RotatedBy(MathHelper.TwoPi * i / 36f);
                int dust = Dust.NewDust(Projectile.Center + offset * 2f, 0, 0, DustID.ShadowbeamStaff, offset.X * 1.5f, offset.Y * 1.5f, 100, default, 2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = true;
                Main.dust[dust].velocity = offset;
            }
            if (!Main.dedServ && Main.rand.NextBool(6))
                SoundEngine.PlaySound(SoundID.NPCDeath39, Projectile.Center);
        }
    }
}
