using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>绯红恶魔 Public 2.0.4 的追踪弹：先减速，60 帧后在 1000 像素内追踪。</summary>
    internal class ScarletDevilBullet : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.timeLeft = 140;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void AI()
        {
            Projectile.ai[0]++;
            if (Projectile.ai[0] <= 60f)
            {
                Projectile.velocity *= 0.975f;
                return;
            }
            Vector2 targetCenter = Projectile.Center;
            bool hasTarget = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;
                float extraDistance = npc.width / 2f + npc.height / 2f;
                if (Vector2.Distance(npc.Center, Projectile.Center) >= 1000f + extraDistance)
                    continue;
                targetCenter = npc.Center;
                hasTarget = true;
                break;
            }
            if (!hasTarget)
            {
                Projectile.velocity = Vector2.Zero;
                return;
            }
            Vector2 moveDirection = Projectile.SafeDirectionTo(targetCenter, Vector2.UnitY);
            Projectile.velocity = (Projectile.velocity * 10f + moveDirection * 30f) / 11f;
        }
        public override Color? GetAlpha(Color lightColor) => new Color(250, 250, 250);
        public override bool? CanDamage()
        {
            if (CDUtil.IsStealthStrike(Projectile, out _) && Projectile.ai[0] < 60f)
                return false;
            return null;
        }
    }
}
