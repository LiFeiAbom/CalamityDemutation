using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 女妖爆裂（BansheeHookBoom） - 女妖之爪命中时在目标处炸开的隐形爆炸弹幕（移植自灾厄本体 2.0.3.9）。
    /// 无贴图，靠 6 帧内逐步放大 + 补一次 <see cref="Projectile.Damage"/> 结算范围伤害，同时刷两组鬼火尘。
    /// </summary>
    internal class BansheeHookBoom : ModProjectile
    {
        /// <summary>复用本工程共用的隐形占位贴图（位置在 Projectiles 根目录，不在 Melee 子目录）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.alpha = 255;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 6;                        // 只存在 6 帧
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesIDStaticNPCImmunity = true;      // 同一目标 10 帧内只吃一次
            Projectile.idStaticNPCHitCooldown = 10;
        }
        /// <summary>
        /// ai[1] 为放大倍率（生成时由调用方随机），第 1 帧把命中盒撑到 52×scale 后补一次伤害结算
        /// </summary>
        public override void AI()
        {
            Projectile.ai[1] += 0.01f;
            Projectile.scale = Projectile.ai[1];
            Projectile.ai[0] += 1f;
            Projectile.alpha -= 63;
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            Lighting.AddLight(Projectile.Center, 1.5f, 0f, 0.15f);
            if (Projectile.ai[0] == 1f)
            {
                Projectile.position = Projectile.Center;
                Projectile.width = Projectile.height = (int)(52f * Projectile.scale);
                Projectile.Center = Projectile.position;
                Projectile.Damage();
                for (int i = 0; i < 2; i++)
                {
                    int bansheeDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.RedTorch, 0f, 0f, 100, default, 1.5f);
                    Main.dust[bansheeDust].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                }
                for (int j = 0; j < 5; j++)
                {
                    int bansheeDust2 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.RedTorch, 0f, 0f, 200, default, 2.7f);
                    Main.dust[bansheeDust2].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                    Main.dust[bansheeDust2].noGravity = true;
                    Main.dust[bansheeDust2].velocity *= 3f;
                    bansheeDust2 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.RedTorch, 0f, 0f, 100, default, 1.5f);
                    Main.dust[bansheeDust2].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                    Main.dust[bansheeDust2].velocity *= 2f;
                    Main.dust[bansheeDust2].noGravity = true;
                    Main.dust[bansheeDust2].fadeIn = 2.5f;
                }
                for (int k = 0; k < 2; k++)
                {
                    int bansheeDust3 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.RedTorch, 0f, 0f, 0, default, 2.7f);
                    Main.dust[bansheeDust3].position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 2f;
                    Main.dust[bansheeDust3].noGravity = true;
                    Main.dust[bansheeDust3].velocity *= 3f;
                }
                for (int l = 0; l < 5; l++)
                {
                    int spiritDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.DungeonSpirit, 0f, 0f, 0, default, 1.5f);
                    Main.dust[spiritDust].position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 2f;
                    Main.dust[spiritDust].noGravity = true;
                    Main.dust[spiritDust].velocity *= 3f;
                }
            }
        }
    }
}
