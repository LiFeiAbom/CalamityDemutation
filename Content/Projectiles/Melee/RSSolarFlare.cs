using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>红日召下的四帧太阳耀斑：先缩小淡出、再放大，消失时分裂两枚追踪沙火。</summary>
    internal class RSSolarFlare : ModProjectile
    {
        public override void SetStaticDefaults() => Main.projFrames[Type] = 4;
        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 150;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 120;
        }
        public override void AI()
        {
            if (Projectile.timeLeft < 60)
                Projectile.tileCollide = true;
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale -= 0.02f;
                Projectile.alpha += 15;
                if (Projectile.alpha >= 150)
                {
                    Projectile.alpha = 150;
                    Projectile.localAI[0] = 1f;
                }
            }
            else
            {
                Projectile.scale += 0.02f;
                Projectile.alpha -= 15;
                if (Projectile.alpha <= 0)
                {
                    Projectile.alpha = 0;
                    Projectile.localAI[0] = 0f;
                }
            }
            Projectile.velocity *= 0.975f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            Lighting.AddLight(Projectile.Center, 0.75f, 0.75f, 0f);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle frame = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition
                + new Vector2(0f, Projectile.gfxOffY), frame, Projectile.GetAlpha(lightColor),
                Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f),
                Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            float spread = 180f * 0.0174f;
            double startAngle = Math.Atan2(Projectile.velocity.X, Projectile.velocity.Y) - spread / 2;
            double deltaAngle = spread / 8f;
            if (Projectile.owner == Main.myPlayer)
            {
                double offsetAngle = startAngle + deltaAngle / 2f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                    new Vector2((float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f)),
                    ModContent.ProjectileType<SandFire>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                    new Vector2((float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f)),
                    ModContent.ProjectileType<SandFire>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            Projectile.position.X += Projectile.width / 2f;
            Projectile.position.Y += Projectile.height / 2f;
            Projectile.width = Projectile.height = 50;
            Projectile.position.X -= Projectile.width / 2f;
            Projectile.position.Y -= Projectile.height / 2f;
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Teleporter, 0f, 0f, 100, default, 0.5f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 35; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Sand, 0f, 0f, 100);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Teleporter, 0f, 0f, 100, default, 0.5f);
                Main.dust[dust].velocity *= 2f;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 240);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 240);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 240);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 240);
        }
    }
}
