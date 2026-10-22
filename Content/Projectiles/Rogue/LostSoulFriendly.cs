using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 灾厄 2.0 的友好迷失灵魂，供幻魂归墟普通投矛复现旧版漂移灵魂。
    /// </summary>
    internal class LostSoulFriendly : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.extraUpdates = 1;
            Projectile.alpha = 150;
        }
        public override bool? CanHitNPC(NPC target) => (Projectile.timeLeft < 210 || Projectile.ai[0] == 2f) && target.CanBeChasedBy(Projectile, false);
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 8)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Type])
                Projectile.frame = 0;
            Lighting.AddLight(Projectile.Center, 0.25f, 0.25f, 0.25f);
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);
            if (Projectile.ai[0] != 1f && (Projectile.timeLeft < 210 || Projectile.ai[0] == 2f))
                CDUtil.HomeInOnNPC(Projectile, true, 600f, 12f, 20f);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.CountsAsClass<ThrowingDamageClass>())
                return;
            int excess = Main.player[Projectile.owner].ownedProjectileCounts[Type] - 5;
            modifiers.SourceDamage *= MathHelper.Clamp(1f - excess * 0.05f, 0f, 1f);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 10; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.SpectreStaff, 0f, 0f, 100, default, 1f);
        }
        public override Color? GetAlpha(Color lightColor) => new Color(150, 150, 150, 150);
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
