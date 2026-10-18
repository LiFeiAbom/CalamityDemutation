using System;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 远古之怒的能量脉冲 —— 逐行照搬灾厄 2.0 的 <c>Projectiles/Magic/GranitePulse.cs</c>。
    /// <para>
    /// 40×46 判定、6 帧动画、1200 帧寿命、本体不造成伤害（<see cref="CanDamage"/> 恒 false）；
    /// 每 50 帧向四周放出 4 对 <see cref="GraniteEnergy"/>。源码的
    /// <c>CalamityGlobalProjectile.ExpandHitboxBy</c> 换成本工程同签名的 <c>Projectile.ExpandHitboxBy</c>。
    /// </para>
    /// </summary>
    internal class GranitePulse : ModProjectile
    {
        /// <summary>是否播放过出场音与烟尘</summary>
        private bool initialized;
        /// <summary>贴图共 6 帧（40×276，每帧 40×46）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }
        /// <summary>基础属性照 2.0</summary>
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200;
        }
        /// <summary>上下漂浮、6 帧轮播、防止同主人多个脉冲堆叠、每 50 帧放一圈追踪弹</summary>
        public override void AI()
        {
            Projectile.velocity = new Vector2(0f, (float)Math.Sin(MathHelper.TwoPi * Projectile.ai[0] / 300f) * 0.5f);
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] >= 300f)
            {
                Projectile.ai[0] = 0f;
                Projectile.netUpdate = true;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Type])
            {
                Projectile.frame = 0;
            }

            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] >= 7200f)
            {
                Projectile.alpha += 5;
                if (Projectile.alpha > 255)
                {
                    Projectile.alpha = 255;
                    Projectile.Kill();
                }
            }

            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] >= 10f)
            {
                Projectile.localAI[0] = 0f;
                int projectileCount = 0;
                int oldestIndex = 0;
                float oldestAge = 0f;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile other = Main.projectile[i];
                    if (other.active && other.owner == Projectile.owner && other.type == Projectile.type && other.ai[1] < 3600f)
                    {
                        projectileCount++;
                        if (other.ai[1] > oldestAge)
                        {
                            oldestIndex = i;
                            oldestAge = other.ai[1];
                        }
                    }
                }
                if (projectileCount > 1)
                {
                    Main.projectile[oldestIndex].netUpdate = true;
                    Main.projectile[oldestIndex].ai[1] = 36000f;
                    return;
                }
            }

            if (!initialized)
            {
                PlayPulseEffects();
                initialized = true;
            }

            if (Projectile.timeLeft % 50 == 1 && Projectile.alpha <= 0)
            {
                PlayPulseEffects();
                float spread = 45f * 0.0174f;
                double startAngle = Math.Atan2(Projectile.velocity.X, Projectile.velocity.Y) - spread / 2;
                double deltaAngle = spread / 8f;
                if (Projectile.owner == Main.myPlayer)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        double offsetAngle = startAngle + deltaAngle * (i + i * i) / 2f + 32f * i;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y,
                            (float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f),
                            ModContent.ProjectileType<GraniteEnergy>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y,
                            (float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f),
                            ModContent.ProjectileType<GraniteEnergy>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
                    }
                }
            }
        }
        /// <summary>出场与脉冲音效、烟尘（照 2.0 的 229 + 206 两种尘）</summary>
        private void PlayPulseEffects()
        {
            SoundEngine.PlaySound(SoundID.NPCHit53, Projectile.Center);
            Projectile.ExpandHitboxBy(20);
            for (int i = 0; i < 5; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0f, 0f, 100, default, 0.5f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.UnusedWhiteBluePurple, 0f, 0f, 100, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.UnusedWhiteBluePurple, 0f, 0f, 100, default, 0.5f);
                Main.dust[dust].velocity *= 2f;
            }
        }
        /// <summary>按 6 帧图集自绘（照 2.0，避免默认绘制把 40×276 当成单帧）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle frame = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                frame, Projectile.GetAlpha(lightColor), Projectile.rotation,
                new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>脉冲本体不造成伤害（伤害由追踪弹承担）</summary>
        public override bool? CanDamage() => false;
    }
}
