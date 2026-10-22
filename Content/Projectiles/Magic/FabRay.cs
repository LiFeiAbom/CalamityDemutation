using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 绝学法杖的反弹射线——判定与分裂节奏照灾厄 2.0 <c>FabRay</c>。
    /// 每 12 次更新检测射线本体与轨迹采样点，并在附近有敌人时向目标发射 <see cref="FabBolt"/>。
    /// </summary>
    internal class FabRay : ModProjectile
    {
        private const int ShootRate = 12;
        private const float DirectTargetingRange = 180f;
        private const float TrailTargetingRange = 280f;
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 54;
        }
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 70;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 60 * Projectile.extraUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.2f, 0.01f, 0.1f);
            Projectile.ai[0]++;
            Projectile.Opacity = Utils.GetLerpValue(0f, 10f * Projectile.MaxUpdates, Projectile.timeLeft, true);
            if (Projectile.owner == Main.myPlayer && Projectile.ai[0] % ShootRate == 0f)
            {
                NPC target = Projectile.Center.FindClosestNPC(DirectTargetingRange, false);
                if (target != null)
                {
                    Vector2 velocity = Projectile.SafeDirectionTo(target.Center) * 13f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center - velocity * 2.5f, velocity, ModContent.ProjectileType<FabBolt>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                for (int i = 0; i < Projectile.oldPos.Length / 4; i += 3)
                {
                    target = Projectile.oldPos[i].FindClosestNPC(TrailTargetingRange, false);
                    if (target == null)
                        continue;
                    Vector2 velocity = (target.Center - Projectile.oldPos[i]).SafeNormalize(Vector2.UnitY) * 13f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.oldPos[i] - velocity * 2.5f, velocity, ModContent.ProjectileType<FabBolt>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    break;
                }
            }
            Lighting.AddLight(Projectile.Center, Vector3.One * Projectile.Opacity * 0.7f);
        }
        private Color ColorFunction(float completionRatio, Vector2 _)
        {
            float fadeToEnd = MathHelper.Lerp(0.65f, 1f, (float)Math.Cos(-Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
            float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio, true) * Projectile.Opacity;
            Color endColor = Color.Lerp(Color.Cyan, Color.HotPink, (float)Math.Sin(completionRatio * MathHelper.Pi * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
        }
        private float WidthFunction(float completionRatio, Vector2 _)
        {
            float expansion = 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0f, 0.3f, completionRatio, true), 2D);
            return MathHelper.Lerp(0f, 32f * Projectile.Opacity, expansion);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityDemutation:TrailStreak"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, (float _, Vector2 _) => Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:TrailStreak"]), 32);
            return false;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.penetrate--;
            if (Projectile.penetrate <= 0)
            {
                Projectile.Kill();
                return false;
            }
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;
            return false;
        }
    }
}
