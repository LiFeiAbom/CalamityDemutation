using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 灾厄 2.0 的追踪幻影弹幕；供幻影投矛及后续幻魂归墟复现旧版支援弹。
    /// 生成后可被调用方改写成盗贼伤害类型。
    /// </summary>
    internal class Phantom : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = 2;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Magic;
        }
        public override void AI()
        {
            Projectile.rotation += 0.01f;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.2f, 0.2f);
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 1f);
                Main.dust[dust].noGravity = true;
            }
            CDUtil.HomeInOnNPC(Projectile, true, 300f, 12f, 20f);
        }
    }
}
