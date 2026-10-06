using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Healing
{
    /// <summary>
    /// 弑神者治疗球（GodSlayerHealOrb） - 弑神者法师头（GodSlayerVisage）套装魔法命中时生成的追踪治疗弹幕
    ///（按经典版灾厄 Projectiles/Healing/GodSlayerHealOrb.cs 1:1 移植）。
    /// 4×4、穿透 1、穿地形、存活 240 帧、额外 3 次更新；飞行中朝 ai[0] 指定的玩家加速（上限 6.5 像素/帧），
    /// 进入 50 像素并与目标碰撞箱重叠时为其回复 ai[1] 点生命并销毁；治疗与同步只在弹幕主人端执行，
    /// 且主人处于吸血减益（moonLeech）时跳过。每帧在原地喷 1 粒 ShadowbeamStaff 尘（源裸数字 173 已 Cecil 反查）。
    /// </summary>
    public class GodSlayerHealOrb : ModProjectile
    {
        /// <summary>
        /// 基础属性：4×4 碰撞箱、友方、穿透 1、穿地形、存活 240 帧、额外 3 次更新（经典版原样）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 3;
        }
        /// <summary>
        /// AI：朝目标玩家加速；接触即治疗并销毁；每帧喷 1 粒尘
        /// </summary>
        public override void AI()
        {
            int targetPlayer = (int)Projectile.ai[0];   // ai[0]：目标玩家索引（由生成方写入）
            const float speed = 6.5f;
            Vector2 center = new Vector2(Projectile.position.X + Projectile.width * 0.5f, Projectile.position.Y + Projectile.height * 0.5f);
            float dirX = Main.player[targetPlayer].Center.X - center.X;
            float dirY = Main.player[targetPlayer].Center.Y - center.Y;
            float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            // 进入 50 像素且与目标碰撞箱重叠：治疗并消失
            if (dist < 50f && Projectile.position.X < Main.player[targetPlayer].position.X + Main.player[targetPlayer].width && Projectile.position.X + Projectile.width > Main.player[targetPlayer].position.X && Projectile.position.Y < Main.player[targetPlayer].position.Y + Main.player[targetPlayer].height && Projectile.position.Y + Projectile.height > Main.player[targetPlayer].position.Y)
            {
                if (Projectile.owner == Main.myPlayer && !Main.LocalPlayer.moonLeech)
                {
                    int healAmount = (int)Projectile.ai[1];   // ai[1]：治疗量
                    Main.player[targetPlayer].HealEffect(healAmount, false);
                    Main.player[targetPlayer].statLife += healAmount;
                    if (Main.player[targetPlayer].statLife > Main.player[targetPlayer].statLifeMax2)
                    {
                        Main.player[targetPlayer].statLife = Main.player[targetPlayer].statLifeMax2;
                    }
                    NetMessage.SendData(MessageID.SpiritHeal, -1, -1, null, targetPlayer, healAmount, 0f, 0f, 0, 0, 0);
                }
                Projectile.Kill();
            }
            // 朝目标加速：每帧把速度按 15/16 衰减后叠加目标方向的分量
            dist = speed / dist;
            dirX *= dist;
            dirY *= dist;
            Projectile.velocity.X = (Projectile.velocity.X * 15f + dirX) / 16f;
            Projectile.velocity.Y = (Projectile.velocity.Y * 15f + dirY) / 16f;
            // 每帧 1 粒尘（源里那层 for (i = 0; i < 1) 的偏移量恒为 0，故直接写成单粒）
            int healDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default(Color), 2f);
            Main.dust[healDust].noGravity = true;
            Main.dust[healDust].velocity *= 0f;
        }
    }
}
