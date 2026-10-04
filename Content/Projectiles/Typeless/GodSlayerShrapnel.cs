using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 弑神者破片（GodSlayerShrapnel） - GodSlayerShrapnelRound 炸开后飞散的小破片
    /// （移植自经典版灾厄同名弹幕）。
    /// 行为照经典版：前 5 帧平飞，之后受重力（每帧 +0.2）、水平速度缓慢衰减；
    /// 弹体会随水平速度转动，并持续撒紫色尘；撞到地形不消失（源里 OnTileCollide 返回 false）。
    /// </summary>
    internal class GodSlayerShrapnel:ModProjectile
    {
        /// <summary>基础属性：6x12 碰撞箱、友方、穿透 1、存活 90 帧（照经典版）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
        }
        /// <summary>AI：前 5 帧平飞 → 之后受重力、水平速度衰减；随速度转动并撒紫色尘（对齐源写法）</summary>
        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 5f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X *= 0.97f;
                    if (Math.Abs(Projectile.velocity.X) < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;
            // 主尘：位置略偏左下、带上飘速度
            int godDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 0.5f);
            Main.dust[godDust].position.X -= 2f;
            Main.dust[godDust].position.Y += 2f;
            Main.dust[godDust].scale += Main.rand.Next(50) * 0.01f;
            Main.dust[godDust].noGravity = true;
            Main.dust[godDust].velocity.Y -= 2f;
            // 50% 概率追加一颗（更慢、向上飘）
            if (Main.rand.NextBool(2))
            {
                int godDust2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 0.5f);
                Main.dust[godDust2].position.X -= 2f;
                Main.dust[godDust2].position.Y += 2f;
                Main.dust[godDust2].scale += 0.3f + Main.rand.Next(50) * 0.01f;
                Main.dust[godDust2].noGravity = true;
                Main.dust[godDust2].velocity *= 0.1f;
            }
            // 上升末段减速与贴图朝向（源写法）
            if (Projectile.velocity.Y < 0.25f && Projectile.velocity.Y > 0.15f)
                Projectile.velocity.X *= 0.8f;
            Projectile.rotation = -Projectile.velocity.X * 0.05f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;
        }
        /// <summary>撞到地形不消失（源里直接返回 false；穿透耗尽时由原版逻辑处理）</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.penetrate == 0)
                Projectile.Kill();
            return false;
        }
    }
}
