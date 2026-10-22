using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 幻魂归墟的潜伏幻影：快速淡入并追踪 1300 范围内的目标，命中后消散。
    /// </summary>
    internal class PhantasmalRuinGhost : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (Projectile.timeLeft >= 207)
                Projectile.alpha -= 6;
            if (Projectile.timeLeft <= 25)
                Projectile.alpha += 9;
            CDUtil.HomeInOnNPC(Projectile, true, 1300f, 12f, 40f);
        }
        public override Color? GetAlpha(Color lightColor) => new Color(100, 200, 255, 100);
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 239)
                return false;
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor);
            return false;
        }
    }
}
