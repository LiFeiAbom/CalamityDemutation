using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 月影投矛的星尘碎片——前 30 次更新不能命中，随后开始追踪。
    /// 潜伏形态通过 ai[1] 使用更短索敌距离与更强转向。
    /// </summary>
    internal class LumiShard : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool? CanHitNPC(NPC target) => Projectile.timeLeft < 90 && target.CanBeChasedBy(Projectile, false);
        public override void AI()
        {
            if (Projectile.ai[0] == 0f && Projectile.velocity == Vector2.Zero)
                Projectile.localAI[0] = 1f;
            Projectile.ai[0] = 1f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.localAI[0] == 1f)
                Projectile.velocity.Y *= 1.05f;
            if (Projectile.timeLeft >= 90)
                return;
            if (Projectile.ai[1] == 1f)
                CDUtil.HomeInOnNPC(Projectile, true, 450f, 14f, 15f);
            else
                CDUtil.HomeInOnNPC(Projectile, true, 600f, 16f, 20f);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 2; i++)
            {
                int blue = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BubbleBurst_Blue, Projectile.oldVelocity.X / 4f, Projectile.oldVelocity.Y / 4f, 0, default, 0.75f);
                Main.dust[blue].noGravity = true;
                Main.dust[blue].velocity *= 3f;
                int pink = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BubbleBurst_Pink, Projectile.oldVelocity.X / 4f, Projectile.oldVelocity.Y / 4f, 0, default, 0.75f);
                Main.dust[pink].noGravity = true;
                Main.dust[pink].velocity *= 3f;
            }
        }
    }
}
