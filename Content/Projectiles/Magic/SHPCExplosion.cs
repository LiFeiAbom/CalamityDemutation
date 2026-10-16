using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// SHPC 等离子球的爆炸（源：本体 2.0 <c>Projectiles/Magic/SHPExplosion</c>）——本体不可见，只靠尘表现。
    /// <para>
    /// 判定 500×500 的大范围、穿透 -1、寿命 60 帧、`usesLocalNPCImmunity` + 每目标 10 帧冷却（照源）。
    /// 每帧向内喷射 246 / 73 / 187 三种尘，`ai[0]` 每帧 +4，过 180 帧后喷射量逐步归零并自毁。
    /// </para>
    /// </summary>
    internal class SHPCExplosion : ModProjectile
    {
        /// <summary>本体不绘制，复用工程共用的隐形占位贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>判定 500×500、穿透 -1、60 帧寿命、每目标 10 帧本地无敌（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 500;
            Projectile.height = 500;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        /// <summary>喷尘表现（照源）</summary>
        public override void AI()
        {
            float lightScale = Main.rand.Next(90, 111) * 0.01f;
            lightScale *= Main.essScale;
            Lighting.AddLight(Projectile.Center, 5f * lightScale, 1f * lightScale, 4f * lightScale);
            float particleCount = 25f;
            if (Projectile.ai[0] > 180f)
                particleCount -= (Projectile.ai[0] - 180f) / 2f;
            if (particleCount <= 0f)
            {
                particleCount = 0f;
                Projectile.Kill();
            }
            particleCount *= 0.7f;
            Projectile.ai[0] += 4f;
            int spawned = 0;
            while (spawned < particleCount)
            {
                float velX = Main.rand.Next(-40, 41);
                float velY = Main.rand.Next(-40, 41);
                float radius = Main.rand.Next(12, 36);
                float normalizer = (float)Math.Sqrt(velX * velX + velY * velY);
                normalizer = radius / normalizer;
                velX *= normalizer;
                velY *= normalizer;
                int dustType = 246;
                if (Main.rand.NextBool())
                    dustType = 73;
                if (Main.rand.NextBool())
                    dustType = 187;
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 2f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].position = Projectile.Center + new Vector2(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                Main.dust[dustIndex].velocity = new Vector2(velX, velY);
                spawned++;
            }
        }
    }
}
