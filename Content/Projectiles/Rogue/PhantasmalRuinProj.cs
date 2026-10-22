using CalamityDemutation.Content.Projectiles.Magic;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 幻魂归墟主投矛——沿飞行轨迹持续生成普通或潜伏灵魂，命中时炸出扇形灵魂群。
    /// </summary>
    internal class PhantasmalRuinProj : ModProjectile
    {
        private const int Lifetime = 600;
        private const int FramesPerSubProjectile = 13;
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/PhantasmalRuin";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void AI()
        {
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.SpectreStaff, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f, 0, default, 0.85f);
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft % 18 == 0)
            {
                if (stealthStrike)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * 0.25f, ModContent.ProjectileType<PhantasmalRuinGhost>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner);
                else
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0f, Main.rand.NextFloat(-2f, 2f)), ModContent.ProjectileType<LostSoulFriendly>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner, 1f);
            }
            bool fireSubProjectile = (Lifetime - Projectile.timeLeft) % (Projectile.MaxUpdates * FramesPerSubProjectile) == 8;
            if (Projectile.owner == Main.myPlayer && fireSubProjectile)
            {
                int phantom = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Phantom>(), (int)(Projectile.damage * 0.7f), Projectile.knockBack, Projectile.owner);
                if (phantom >= 0 && phantom < Main.maxProjectiles)
                {
                    Main.projectile[phantom].DamageType = CDUtil.GetRogueDamageClass();
                    Main.projectile[phantom].usesLocalNPCImmunity = true;
                    Main.projectile[phantom].localNPCHitCooldown = -2;
                }
                float knockback = Projectile.knockBack * (stealthStrike ? 1f : 0.25f);
                Vector2 velocity = stealthStrike
                    ? (Projectile.velocity * 0.4f).RotatedBy(Main.rand.NextFloat(-0.04f, 0.04f))
                    : Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.4f, 0.4f);
                int soulType = stealthStrike ? ModContent.ProjectileType<PhantasmalRuinGhost>() : ModContent.ProjectileType<LostSoulFriendly>();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, soulType, (int)(Projectile.damage * 0.25f), knockback, Projectile.owner);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1, texture);
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => OnHitEffects();
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => OnHitEffects();
        private void OnHitEffects()
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            SoundEngine.PlaySound(SoundID.NPCDeath39 with { PitchVariance = 0.4f }, Projectile.position);
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            int soulCount = Main.rand.Next(10, 12) + (stealthStrike ? 10 : 0);
            float spreadAngle = MathHelper.ToRadians(Main.rand.Next(25, 30));
            float baseAngle = Projectile.velocity.ToRotation();
            float angleStep = spreadAngle / (soulCount - 1);
            for (int i = 0; i < soulCount; i++)
            {
                float offset = Main.rand.NextFloat(-MathHelper.ToRadians(2f), MathHelper.ToRadians(1f));
                float angle = baseAngle - spreadAngle / 2f + angleStep * i + offset;
                float angleFromBase = Math.Abs(MathHelper.ToDegrees(angle - baseAngle));
                float speed = angleFromBase < 8f ? Main.rand.NextFloat(30f, 45f) : Main.rand.NextFloat(15f, 25f);
                Vector2 velocity = angle.ToRotationVector2() * -speed;
                int soul = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<PhantasmalSoul>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner);
                if (stealthStrike && soul >= 0 && soul < Main.maxProjectiles)
                    CDUtil.SetStealthStrike(Main.projectile[soul]);
            }
        }
    }
}
