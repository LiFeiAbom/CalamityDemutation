using CalamityDemutation.Content.Items.Weapons.Melee;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>灵魂收割者的低血爆炸；按经典 1.4.2.101 原样保留伤害职业与瘟疫施加。</summary>
    internal class HiveBombExplosion : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoulHarvester.ApplyPlague(target, 600);
        }
    }
}
