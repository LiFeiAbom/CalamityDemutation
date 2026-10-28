using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>CI 诅咒短剑弹幕：可多次反弹，并按当前速度间隔释放咒火。</summary>
    internal class CursedDaggerProj : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/CursedDagger";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.aiStyle = ProjAIStyleID.ThrownProjectile;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.timeLeft = 240;
            Projectile.penetrate = 8;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            if (!stealthStrike)
            {
                Projectile.penetrate--;
                if (Projectile.penetrate <= 0)
                {
                    Projectile.Kill();
                    return false;
                }
                Projectile.ai[0] += 0.1f;
            }
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;
            return false;
        }
        public override void AI()
        {
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            float speed = Projectile.velocity.Length();
            int spawnInterval = (int)System.Math.Round(8f * MathHelper.Clamp(12f / speed, 4f, 14f));
            if (Main.rand.NextBool(4))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.CursedTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            if (Projectile.owner != Main.myPlayer || Projectile.timeLeft % spawnInterval != 0)
                return;
            if (!stealthStrike)
            {
                SpawnCursedFlame(ProjectileID.CursedDartFlame, new Vector2(0f, Main.rand.NextFloat(10f, 14f)));
                return;
            }
            int flameType = Main.rand.NextBool(2) ? ProjectileID.CursedFlameFriendly : ProjectileID.CursedDartFlame;
            SpawnCursedFlame(flameType, Main.rand.NextVector2Square(-14f, 14f));
        }
        private void SpawnCursedFlame(int flameType, Vector2 velocity)
        {
            int flame = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                velocity,
                flameType,
                Projectile.damage,
                Projectile.knockBack * 0.5f,
                Projectile.owner);
            if (flame < 0 || flame >= Main.maxProjectiles)
                return;
            Main.projectile[flame].DamageType = CDUtil.GetRogueDamageClass();
            Main.projectile[flame].usesLocalNPCImmunity = true;
            Main.projectile[flame].localNPCHitCooldown = 10;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.CursedInferno, 600);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 10; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.CursedTorch, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
