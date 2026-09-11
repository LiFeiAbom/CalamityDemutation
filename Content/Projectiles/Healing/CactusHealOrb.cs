using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Healing
{
    /// <summary>
    /// 仙人掌治疗球 - 靠近玩家时为其回复生命的追踪弹幕
    /// 本体不可见，仅以绿色光尘表现；在玩家 50 像素内即被吸收并治疗
    /// </summary>
    internal class CactusHealOrb:ModProjectile
    {
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：8x8 全透明碰撞箱；友方、入水不减速、单次穿透、存活 180 帧（约 3 秒）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;   // 全透明，仅靠粉尘表现
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
        }
        /// <summary>
        /// AI：纵向速度每帧衰减模拟漂浮下坠；一旦与主人玩家重叠（距离 50 像素内）即被吸收，为其回复 15 点生命并销毁；持续生成绿色光尘作外观
        /// </summary>
        public override void AI()
        {
            Projectile.velocity.Y *= 0.985f;   // 缓慢减速，模拟漂浮下坠
            int num487 = Projectile.owner;
            Vector2 vector36 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
            float num489 = Main.player[num487].Center.X - vector36.X;
            float num490 = Main.player[num487].Center.Y - vector36.Y;
            float num491 = (float)Math.Sqrt((double)(num489 * num489 + num490 * num490));
            // 与主人玩家重叠且距离小于 50 像素时：治疗 15 点生命并消失
            if (num491 < 50f && Projectile.position.X < Main.player[num487].position.X + (float)Main.player[num487].width && Projectile.position.X + (float)Projectile.width > Main.player[num487].position.X && Projectile.position.Y < Main.player[num487].position.Y + (float)Main.player[num487].height && Projectile.position.Y + (float)Projectile.height > Main.player[num487].position.Y)
            {
                // 仅主人端执行治疗，避免联机重复触发
                if (Projectile.owner == Main.myPlayer)
                {
                    int num492 = 15;
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
            // 生成静止的绿色光尘作为外观
            float num498 = Projectile.velocity.X * 0.2f * 1f;
            float num499 = -(Projectile.velocity.Y * 0.2f) * 1f;
            int num500 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Terra, 0f, 0f, 100, new Color(0, 200, 0), 1.5f);
            Main.dust[num500].noGravity = true;
            Main.dust[num500].velocity *= 0f;
            Dust expr_154F9_cp_0 = Main.dust[num500];
            expr_154F9_cp_0.position.X = expr_154F9_cp_0.position.X - num498;
            Dust expr_15518_cp_0 = Main.dust[num500];
            expr_15518_cp_0.position.Y = expr_15518_cp_0.position.Y - num499;
            return;
        }
    }
}
