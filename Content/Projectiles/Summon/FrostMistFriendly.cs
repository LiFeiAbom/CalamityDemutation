using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 友方霜雾弹：由海妖诱饵（SirenLure）发射，带蓝焰粒子，并做缩放/透明度往返的"呼吸"效果。
    /// 生成后的第 30~110 帧窗口内会把速度方向修正为朝最近的**玩家**（通常是召唤者本人）；
    /// 代码中不存在朝敌人追踪或加速的逻辑。
    /// </summary>
    internal class FrostMistFriendly:ModProjectile
    {
        /// <summary>
        /// 注册 6 帧动画。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
        }
        /// <summary>
        /// 基础属性：召唤物伤害、单次穿透、短暂存活，初始完全透明。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.alpha = 255;
        }
        /// <summary>
        /// 弹幕 AI：循环播放动画、初次喷吐粒子、透明度/尺寸呼吸、追踪最近玩家并加速。
        /// </summary>
        public override void AI()
        {
            // ai[1]：帧计数器——首帧为 0，喷完登场粒子后置 1，此后每帧自增；第 30~110 帧窗口内朝最近玩家转向
            // localAI[0]：呼吸相位，0 = 正在收缩（透明化），1 = 正在膨胀（显形）
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
            {
                Projectile.frame = 0;
            }
            if (Projectile.ai[1] == 0f)
            {
                for (int num621 = 0; num621 < 5; num621++)
                {
                    int num622 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.BlueFlare, 0f, 0f, 100, default(Color), 2f);
                    Main.dust[num622].velocity *= 3f;
                    if (Main.rand.Next(2) == 0)
                    {
                        Main.dust[num622].scale = 0.5f;
                        Main.dust[num622].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                    }
                }
                Projectile.ai[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item30, Projectile.position);
            }
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale -= 0.02f;
                Projectile.alpha += 30;
                if (Projectile.alpha >= 250)
                {
                    Projectile.alpha = 255;
                    Projectile.localAI[0] = 1f;
                }
            }
            else if (Projectile.localAI[0] == 1f)
            {
                Projectile.scale += 0.02f;
                Projectile.alpha -= 30;
                if (Projectile.alpha <= 0)
                {
                    Projectile.alpha = 0;
                    Projectile.localAI[0] = 0f;
                }
            }
            // 在存在 30~110 帧的窗口内追踪最近的玩家（通常是召唤者本人）
            int num103 = (int)Player.FindClosest(Projectile.Center, 1, 1);
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] < 110f && Projectile.ai[1] > 30f)
            {
                float scaleFactor2 = Projectile.velocity.Length();
                Vector2 vector11 = Main.player[num103].Center - Projectile.Center;
                vector11.Normalize();
                vector11 *= scaleFactor2;
                Projectile.velocity = (Projectile.velocity * 24f + vector11) / 25f;
                Projectile.velocity.Normalize();
                Projectile.velocity *= scaleFactor2;
            }
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) + 1.57f;
            Lighting.AddLight(Projectile.Center, ((255 - Projectile.alpha) * 0f) / 255f, ((255 - Projectile.alpha) * 0.35f) / 255f, ((255 - Projectile.alpha) * 0.35f) / 255f);
        }
        /// <summary>
        /// 自定义绘制：手动截取当前动画帧并绕中心旋转绘制（替换默认绘制）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = TextureAssets.Projectile[Projectile.type].Value;
            int num214 = TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y6 = num214 * Projectile.frame;
            Main.spriteBatch.Draw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(0, y6, texture2D13.Width, num214)), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2((float)texture2D13.Width / 2f, (float)num214 / 2f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
        /// <summary>
        /// 浅灰色着色，透明度跟随 alpha（实现渐隐/渐显的雾团呼吸）
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 200, 200, Projectile.alpha);
        }
    }
}
