using CalamityDemutation.Graphics.Primitives;
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
    /// 绝学法杖的分裂光弹——判定、加速和淡出逻辑照灾厄 2.0 <c>FabBolt</c>。
    /// </summary>
    internal class FabBolt : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        private bool FadingOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 23;
        }
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.extraUpdates = 2;
            Projectile.penetrate = 12;
            Projectile.timeLeft = 90 * Projectile.extraUpdates;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
        }
        public override void AI()
        {
            if (FadingOut)
            {
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 0f, 0.27f);
                if (Projectile.Opacity <= 0.05f)
                    Projectile.Kill();
            }
            else
            {
                Projectile.velocity *= 1.004f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Vector3.One * Projectile.Opacity * 0.45f);
        }
        private Color ColorFunction(float completionRatio, Vector2 _)
        {
            float fadeToEnd = MathHelper.Lerp(0.25f, 0.5f, (float)Math.Cos(-Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
            fadeToEnd *= 1f - Utils.GetLerpValue(0.35f, 0f, completionRatio, true);
            Color endColor = Color.Lerp(Color.Cyan, Color.HotPink, Projectile.identity % 2);
            return Color.Lerp(Color.White, endColor, fadeToEnd) * Projectile.Opacity * 0.7f;
        }
        private float WidthFunction(float completionRatio, Vector2 _)
        {
            float expansion = 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0f, 0.2f, completionRatio, true), 2D);
            return MathHelper.Lerp(0f, 22f, expansion) * Projectile.Opacity;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityDemutation:TrailStreak"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/Trails/FabstaffStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, (float _, Vector2 _) => Projectile.Size * 0.5f, smoothen: false, pixelate: false, GameShaders.Misc["CalamityDemutation:TrailStreak"]), 80);
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!FadingOut && Projectile.penetrate < 2)
            {
                FadingOut = true;
                Projectile.velocity *= 0.1f;
                Projectile.extraUpdates = 0;
                Projectile.netUpdate = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!FadingOut)
            {
                FadingOut = true;
                Projectile.velocity *= 0.1f;
                Projectile.extraUpdates = 0;
                Projectile.netUpdate = true;
            }
            return false;
        }
        public override bool? CanDamage() => FadingOut ? false : null;
    }
}
