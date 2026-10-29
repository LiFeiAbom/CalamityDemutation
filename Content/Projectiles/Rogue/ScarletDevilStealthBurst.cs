using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 绯红恶魔潜行星阵的生成控制器。
    /// 恢复 Public 2.0.4 的 256 枚弹幕口径，并按每帧 32 枚分批创建，降低单帧峰值。
    /// </summary>
    internal class ScarletDevilStealthBurst : ModProjectile
    {
        private const int RingCount = 40;
        private const int StarCount = 2;
        private const int StarPoints = 6;
        private const int PointsPerSegment = 18;
        private const int StarProjectileCount = StarCount * StarPoints * PointsPerSegment;
        private const int TotalProjectileCount = RingCount + StarProjectileCount;
        private const int SpawnPerFrame = 32;
        private const float StarSpeed = 25f;
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalProjectileCount / SpawnPerFrame + 2;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void AI()
        {
            int spawnedThisFrame = 0;
            while (Projectile.ai[0] < TotalProjectileCount && spawnedThisFrame < SpawnPerFrame)
            {
                SpawnStealthBullet((int)Projectile.ai[0]);
                Projectile.ai[0]++;
                spawnedThisFrame++;
            }
            if (Projectile.ai[0] >= TotalProjectileCount)
                Projectile.Kill();
        }
        private void SpawnStealthBullet(int index)
        {
            Vector2 velocity = GetVelocity(index);
            int bullet = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity, velocity,
                ModContent.ProjectileType<ScarletDevilBullet>(), Projectile.damage, 0f, Projectile.owner);
            if (bullet >= 0 && bullet < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[bullet]);
        }
        private static Vector2 GetVelocity(int index)
        {
            if (index < RingCount)
                return (MathHelper.TwoPi * index / RingCount).ToRotationVector2() * StarSpeed;
            int starIndex = index - RingCount;
            int starLayer = starIndex / (StarPoints * PointsPerSegment);
            int pointIndex = starIndex % (StarPoints * PointsPerSegment) / PointsPerSegment;
            int segmentProgress = starIndex % PointsPerSegment;
            float angle = MathHelper.Pi * 1.5f - pointIndex * MathHelper.TwoPi / StarPoints;
            int nextPointOffset = starLayer == 0 ? 3 : 2;
            float nextAngle = MathHelper.Pi * 1.5f
                - (pointIndex + nextPointOffset) % StarPoints * MathHelper.TwoPi / StarPoints;
            Vector2 direction = Vector2.Lerp(angle.ToRotationVector2(), nextAngle.ToRotationVector2(),
                segmentProgress / (float)PointsPerSegment);
            return direction * StarSpeed;
        }
    }
}
