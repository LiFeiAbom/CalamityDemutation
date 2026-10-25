using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 渎神核弹留下的伤害粉尘——照 CI ProfanedNukeDust：短寿命、静态逐敌冷却，命中施加圣火。
    /// </summary>
    internal class ProfanedNukeDust : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 15;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.5f, 0f));
            float activeTime = 25f;
            if (Projectile.ai[0] > 60f)
                activeTime -= (Projectile.ai[0] - 60f) / 2f;
            if (activeTime <= 0f)
            {
                Projectile.Kill();
                return;
            }
            Projectile.ai[0] += 2f + Projectile.ai[1];
            int dustCount = 0;
            while (dustCount < activeTime)
            {
                float vx = Main.rand.Next(-8, 9);
                float vy = Main.rand.Next(-8, 9);
                float speed = Main.rand.Next(2, 7);
                float length = (float)Math.Sqrt(vx * vx + vy * vy);
                if (length != 0f)
                {
                    vx *= speed / length;
                    vy *= speed / length;
                }
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.CopperCoin,
                    new Vector2(vx, vy), 100, default, 1.5f);
                dust.noGravity = true;
                dustCount++;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => CalamityDemutationPlayer.ApplyHolyFlamesWithFallback(target, 120);
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
            => CalamityDemutationPlayer.ApplyHolyFlamesWithFallback(target, 120);
    }
}
