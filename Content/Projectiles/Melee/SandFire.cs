using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>红日分裂出的隐形追踪沙火。</summary>
    internal class SandFire : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 200;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.25f, 0.25f, 0f);
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > 4f)
            {
                for (int i = 0; i < 5; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width,
                        Projectile.height, DustID.Teleporter, 0f, 0f, 100, default, 0.5f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0f;
                }
            }
            CDUtil.HomeInOnNPC(Projectile, true, 2000f, 10f, 20f);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width,
                    Projectile.height, DustID.Teleporter);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
        }
    }
}
