using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Healing
{
    /// <summary>
    /// 金之特斯拉治疗球（Auric Orb） - 治疗用弹幕
    /// 本体使用隐形贴图，飞行中追踪 ai[0] 指定的玩家索引，进入 50 像素内即为其回复 ai[1] 点生命并销毁；
    /// 治疗与同步仅在弹幕主人端执行，且主人处于吸血减益（moonLeech）时跳过。
    /// </summary>
    internal class AuricOrb:ModProjectile
    {
        // 使用隐形贴图，视觉效果由 GetAlpha 的动态染色与消亡尘埃承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：14x14 碰撞箱；友方、初始全透明、单次穿透、存活 360 帧（6 秒）、
        /// 3 倍额外更新（追踪更平滑）、微弱照明
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.alpha = 255;        // 初始全透明
            Projectile.penetrate = 1;      // 命中一次即消失
            Projectile.timeLeft = 360;
            Projectile.extraUpdates = 3;   // 每帧额外更新 3 次，提高追踪平滑度
            Projectile.light = 0.2f;
        }
        /// <summary>
        /// AI：逐帧降低 alpha；按 localAI[0] 在放大（上限 1.2）与缩小（下限 0.8）之间往复脉动；
        /// 随后朝 ai[0] 指定的玩家加速（收敛速度上限 6），进入 50 像素内则治疗并销毁
        /// </summary>
        public override void AI()
        {
            Projectile.alpha -= 2;   // 逐渐显现（贴图本身透明，仅影响染色通道）
            // 脉动：localAI[0] 作为方向开关，0 表示放大阶段、1 表示缩小阶段
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale += 0.05f;
                if ((double)Projectile.scale > 1.2)
                {
                    Projectile.localAI[0] = 1f;
                }
            }
            else
            {
                Projectile.scale -= 0.05f;
                if ((double)Projectile.scale < 0.8)
                {
                    Projectile.localAI[0] = 0f;
                }
            }
            int num487 = (int)Projectile.ai[0];   // ai[0]：目标玩家索引（由生成方写入）
            float num488 = 6f;                    // 追踪速度上限
            Vector2 vector36 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
            float num489 = Main.player[num487].Center.X - vector36.X;
            float num490 = Main.player[num487].Center.Y - vector36.Y;
            float num491 = (float)Math.Sqrt((double)(num489 * num489 + num490 * num490));
            // 与目标玩家碰撞箱重叠且距离小于 50 像素：治疗并消失
            if (num491 < 50f && Projectile.position.X < Main.player[num487].position.X + (float)Main.player[num487].width && Projectile.position.X + (float)Projectile.width > Main.player[num487].position.X && Projectile.position.Y < Main.player[num487].position.Y + (float)Main.player[num487].height && Projectile.position.Y + (float)Projectile.height > Main.player[num487].position.Y)
            {
                // 仅主人端执行治疗，且处于吸血减益时不治疗，避免联机重复触发
                if (Projectile.owner == Main.myPlayer && !Main.LocalPlayer.moonLeech)
                {
                    int num492 = (int)Projectile.ai[1];   // ai[1]：治疗量
                    Main.player[num487].HealEffect(num492, false);
                    Main.player[num487].statLife += num492;
                    if (Main.player[num487].statLife > Main.player[num487].statLifeMax2)
                    {
                        Main.player[num487].statLife = Main.player[num487].statLifeMax2;
                    }
                    NetMessage.SendData(MessageID.SpiritHeal, -1, -1, null, num487, (float)num492, 0f, 0f, 0, 0, 0);
                }
                Projectile.Kill();
            }
            // 朝目标加速：每帧把速度按 15/16 衰减后叠加目标方向的分量
            num491 = num488 / num491;
            num489 *= num491;
            num490 *= num491;
            Projectile.velocity.X = (Projectile.velocity.X * 15f + num489) / 16f;
            Projectile.velocity.Y = (Projectile.velocity.Y * 15f + num490) / 16f;
            return;
        }
        /// <summary>
        /// 动态染色：绿（固定 255）→ 暗黄（DiscoG 随时间变化）→ 橙（53）的流动色，并叠加当前 alpha
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, Main.DiscoG, 53, Projectile.alpha);
        }
        /// <summary>
        /// 消亡：爆出 5 枚叶绿武器色尘埃，无重力、速度与尺寸放大 1.5 倍
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            for (int num407 = 0; num407 < 5; num407++)
            {
                int num408 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 0, new Color(255, Main.DiscoG, 53), 1f);
                Main.dust[num408].noGravity = true;
                Main.dust[num408].velocity *= 1.5f;
                Main.dust[num408].scale = 1.5f;
            }
        }
    }
}
