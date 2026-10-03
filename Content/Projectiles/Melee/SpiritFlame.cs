using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 鬼火（SpiritFlame） - 女妖之爪重制版（CalamityOverhaul 0.4.0.1.3）的配套弹幕，逐行移植。
    /// 4 帧循环动画；<c>ai[0]</c> 决定行为：0 = 随主人飘（跟随玩家速度上飘）、1/2 = 原地加速旋转扩散、
    /// 3 = 上飘 + 左右摆动（用于蓄能期在鼠标处刷出的鬼火簇）。
    /// </summary>
    internal class SpiritFlame : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/SpiritFlame";
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Default;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        /// <summary>出场随机帧与随机缩放（照源）</summary>
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.frameCounter = Main.rand.Next(4);
            Projectile.scale = Main.rand.NextFloat(0.2f, 0.8f);
        }
        public override void AI()
        {
            // 走表：每 10 帧推进一帧，超过第 3 帧回到 0（对应源的 CWRUtils.ClockFrame）
            if (Main.GameUpdateCount % 10 == 0)
            {
                Projectile.frameCounter++;
            }
            if (Projectile.frameCounter > 3)
            {
                Projectile.frameCounter = 0;
            }
            if (Projectile.ai[0] == 0)
            {
                Player owner = Main.player[Projectile.owner];
                if (owner != null && owner.active)
                {
                    Projectile.velocity = owner.velocity * 0.9f + new Vector2(0, -2);
                }
                else
                {
                    Projectile.velocity = new Vector2(0, -2);
                }
            }
            if (Projectile.ai[0] == 1)
            {
                if (Projectile.ai[1] == 0)
                {
                    Projectile.timeLeft = 120;
                    Projectile.scale = 0.6f;
                    Projectile.ai[1] = 1;
                }
                Projectile.scale *= 1.01f;
                Projectile.velocity = Projectile.velocity.RotatedBy(0.03f);
                Projectile.velocity *= 0.99f;
                Projectile.position += Main.player[Projectile.owner].velocity;   // 与玩家保持相对静止
            }
            if (Projectile.ai[0] == 2)
            {
                if (Projectile.ai[1] == 0)
                {
                    Projectile.timeLeft = 150;
                    Projectile.scale = 0.9f;
                    Projectile.ai[1] = 1;
                }
                Projectile.scale *= 1.015f;
                Projectile.velocity = Projectile.velocity.RotatedBy(0.04f);
                Projectile.velocity *= 0.995f;
                Projectile.position += Main.player[Projectile.owner].velocity;
            }
            if (Projectile.ai[0] == 3)
            {
                if (Projectile.ai[1] == 0)
                {
                    Projectile.timeLeft = Main.rand.Next(32, 64);
                    Projectile.scale = Main.rand.NextFloat(0.5f, 0.7f);
                    Projectile.ai[1] = 1;
                }
                Projectile.scale *= 1.003f;
                Projectile.velocity.Y = -3;
                Projectile.velocity.X += MathF.Sin(Main.GameUpdateCount / 60 * MathHelper.Pi) * 3;
                Projectile.position += Main.player[Projectile.owner].velocity;
            }
        }
        /// <summary>按帧索引取 4 帧竖排图的一格绘制</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / 4;
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frameCounter, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f);
            float alp = Projectile.timeLeft / 30f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White * alp,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
