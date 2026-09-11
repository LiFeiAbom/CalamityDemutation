using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Healing
{
    /// <summary>
    /// 真菌治疗珠 - 真菌团块（FungalClump）命中吸血后生成的治疗弹幕
    /// 本体不可见（隐形贴图），向 ai[0] 指定的主人玩家追踪飞行；
    /// 距离玩家 50 像素内被吸收，回复 ai[1] 点生命。主人处于月亮吸血诅咒（moonLeech）时无法回血。
    /// </summary>
    internal class FungalHeal:ModProjectile
    {
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：6x6 全透明碰撞箱；不碰撞地形、超高更新频率（每帧额外 10 次，用于精确捕捉玩家）、存活 300 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 10;
            Projectile.timeLeft = 300;
        }
        /// <summary>
        /// AI：读取目标玩家与治疗量参数，先判定是否进入吸收范围，未接触则朝玩家平滑追踪，并生成蓝色妖精粉尘作外观
        /// </summary>
        public override void AI()
        {
            // ai[0]：目标玩家编号；num488 = 追踪飞行速度（5）
            int num487 = (int)Projectile.ai[0];
            float num488 = 5f;
            Vector2 vector36 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
            float num489 = Main.player[num487].Center.X - vector36.X;
            float num490 = Main.player[num487].Center.Y - vector36.Y;
            // 计算弹幕与目标玩家的当前距离
            float num491 = (float)Math.Sqrt((double)(num489 * num489 + num490 * num490));
            // 与玩家碰撞重叠且距离小于 50 像素时：判定为吸收成功
            if (num491 < 50f && Projectile.position.X < Main.player[num487].position.X + (float)Main.player[num487].width && Projectile.position.X + (float)Projectile.width > Main.player[num487].position.X && Projectile.position.Y < Main.player[num487].position.Y + (float)Main.player[num487].height && Projectile.position.Y + (float)Projectile.height > Main.player[num487].position.Y)
            {
                // 仅在主人端、且主人未受月亮吸血诅咒（moonLeech）时执行回血，避免联机重复触发
                if (Projectile.owner == Main.myPlayer && !Main.player[Main.myPlayer].moonLeech)
                {
                    // ai[1]：本次应回复的生命值
                    int num492 = (int)Projectile.ai[1];
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
            // 未吸收：将指向玩家的向量缩放为速度 5，并与旧速度做 9:1 加权平滑转向（趋近追踪）
            num491 = num488 / num491;
            num489 *= num491;
            num490 *= num491;
            Projectile.velocity.X = (Projectile.velocity.X * 9f + num489) / 16f;
            Projectile.velocity.Y = (Projectile.velocity.Y * 9f + num490) / 16f;
            // 生成两团随速度反向偏移的蓝色妖精粉尘作为外观（大 0.5 / 小 0.7 两档）
            float num494 = Projectile.velocity.X * 0.334f;
            float num495 = -(Projectile.velocity.Y * 0.334f);
            int num496 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.BlueFairy, 0f, 0f, 100, default(Color), 0.5f);
            Main.dust[num496].noGravity = true;
            Main.dust[num496].velocity *= 0f;
            Dust expr_153E2_cp_0 = Main.dust[num496];
            expr_153E2_cp_0.position.X = expr_153E2_cp_0.position.X - num494;
            Dust expr_15401_cp_0 = Main.dust[num496];
            expr_15401_cp_0.position.Y = expr_15401_cp_0.position.Y - num495;
            float num498 = Projectile.velocity.X * 0.2f;
            float num499 = -(Projectile.velocity.Y * 0.2f);
            int num500 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.BlueFairy, 0f, 0f, 100, default(Color), 0.7f);
            Main.dust[num500].noGravity = true;
            Main.dust[num500].velocity *= 0f;
            Dust expr_154F9_cp_0 = Main.dust[num500];
            expr_154F9_cp_0.position.X = expr_154F9_cp_0.position.X - num498;
            Dust expr_15518_cp_0 = Main.dust[num500];
            expr_15518_cp_0.position.Y = expr_15518_cp_0.position.Y - num499;
        }
    }
}
