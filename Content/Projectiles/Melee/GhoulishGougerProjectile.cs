using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 食尸鬼凿刃的近战回旋镰刀；按经典 1.4.2.101 保留 20 帧去程、28 像素/帧回程与逐帧 0.5 旋转。
    /// </summary>
    internal class GhoulishGougerProjectile : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/GhoulishGouger";
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 2;
            Projectile.DamageType = DamageClass.Melee;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.7f / 255f, 0f, (255 - Projectile.alpha) * 0.15f / 255f);
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = 8;
                SoundEngine.PlaySound(SoundID.Item7, Projectile.position);
            }
            if (Projectile.ai[0] == 0f)
            {
                Projectile.ai[1] += 1f;
                if (Projectile.ai[1] >= 20f)
                {
                    Projectile.ai[0] = 1f;
                    Projectile.ai[1] = 0f;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                float returnSpeed = 28f;
                float acceleration = 5f;
                Player owner = Main.player[Projectile.owner];
                Vector2 delta = owner.Center - Projectile.Center;
                float distance = delta.Length();
                if (distance > 3000f)
                    Projectile.Kill();
                if (distance > 0f)
                {
                    Vector2 targetVelocity = delta * (returnSpeed / distance);
                    if (Projectile.velocity.X < targetVelocity.X)
                    {
                        Projectile.velocity.X += acceleration;
                        if (Projectile.velocity.X < 0f && targetVelocity.X > 0f)
                            Projectile.velocity.X += acceleration;
                    }
                    else if (Projectile.velocity.X > targetVelocity.X)
                    {
                        Projectile.velocity.X -= acceleration;
                        if (Projectile.velocity.X > 0f && targetVelocity.X < 0f)
                            Projectile.velocity.X -= acceleration;
                    }
                    if (Projectile.velocity.Y < targetVelocity.Y)
                    {
                        Projectile.velocity.Y += acceleration;
                        if (Projectile.velocity.Y < 0f && targetVelocity.Y > 0f)
                            Projectile.velocity.Y += acceleration;
                    }
                    else if (Projectile.velocity.Y > targetVelocity.Y)
                    {
                        Projectile.velocity.Y -= acceleration;
                        if (Projectile.velocity.Y > 0f && targetVelocity.Y < 0f)
                            Projectile.velocity.Y -= acceleration;
                    }
                }
                if (Main.myPlayer == Projectile.owner && Projectile.Hitbox.Intersects(owner.Hitbox))
                    Projectile.Kill();
            }
            Projectile.rotation += 0.5f;
        }
        public override Color? GetAlpha(Color lightColor) => new Color(250, 250, 250, 50);
    }
}
