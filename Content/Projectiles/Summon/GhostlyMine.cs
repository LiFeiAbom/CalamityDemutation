using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 幽灵地雷（GhostlyMine） - 血炎召唤头（BloodflareHelmet）套装每 15 秒召唤的环绕地雷
    ///（按经典版灾厄 Projectiles/Summon/GhostlyMine.cs 1:1 移植）。
    /// 行为：以主人为中心、半径 550 像素，按 ai[1]（初值取生成方给的 ai[0]）每帧 +1 度的极坐标环绕；
    /// 不落地、穿透 1、存活 900 帧；首帧播一次入场音并喷一圈 DungeonSpirit 尘（尘 180 的实名）；
    /// 命中敌人时把判定框撑到 150×150、播爆炸音并喷尘（伤害由原版结算）。
    /// 与源的差异：源里那段依赖 CalamityGlobalProjectile 的"仆从伤害变化时重算 Projectile.damage"本工程没有该全局，
    /// 故略去；伤害由生成方一次性算好，并用 originalDamage + DamageType.Generic 防止二次缩放。
    /// </summary>
    public class GhostlyMine : ModProjectile
    {
        /// <summary>首帧闩锁：播放入场音并初始化环绕角度</summary>
        private bool start = true;
        /// <summary>首帧闩锁：只喷一次入场尘</summary>
        private bool spawnDust = true;
        /// <summary>
        /// 基础属性：30×30 碰撞箱；友方、入水不减速、不占仆从栏、存活 900 帧、穿透 1、穿地形
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;       // 友方弹幕
            Projectile.ignoreWater = true;    // 入水不减速
            Projectile.minionSlots = 0f;      // 不占仆从栏
            Projectile.timeLeft = 900;        // 存活 900 帧（15 秒）
            Projectile.penetrate = 1;         // 穿透 1
            Projectile.tileCollide = false;   // 穿地形
            Projectile.minion = true;
        }
        /// <summary>
        /// AI：首帧播入场音并把环绕角度取成生成方给的 ai[0]；随后每帧按极坐标把自身钉在主人周围 550 像素处，
        /// 角度每帧 +1 度（即缓慢绕圈），首帧额外喷一圈入场尘
        /// </summary>
        public override void AI()
        {
            if (start)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                Projectile.ai[1] = Projectile.ai[0];   // ai[0] = 生成方给的初始角度
                start = false;
            }
            Player player = Main.player[Projectile.owner];
            double rad = Projectile.ai[1] * (Math.PI / 180);
            const float dist = 550f;
            Projectile.position.X = player.Center.X - (float)(Math.Cos(rad) * dist) - Projectile.width / 2;
            Projectile.position.Y = player.Center.Y - (float)(Math.Sin(rad) * dist) - Projectile.height / 2;
            Projectile.ai[1] += 1f;   // 每帧转 1 度
            if (spawnDust)
            {
                // 入场尘：10 粒普通 + 15 组"本体 + 拖尾"的 DungeonSpirit 尘
                for (int i = 0; i < 10; i++)
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DungeonSpirit, 0f, 0f, 100, default(Color), 2f);
                    Main.dust[d].velocity *= 3f;
                    if (Main.rand.Next(2) == 0)
                    {
                        Main.dust[d].scale = 0.5f;
                        Main.dust[d].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int i = 0; i < 15; i++)
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DungeonSpirit, 0f, 0f, 100, default(Color), 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 5f;
                    d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DungeonSpirit, 0f, 0f, 100, default(Color), 2f);
                    Main.dust[d].velocity *= 2f;
                }
                spawnDust = false;
            }
        }
        /// <summary>
        /// 绘制颜色：散场前 85 帧随剩余寿命渐隐（亮度与 alpha 同步下降），其余时间保持半透明白
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 85)
            {
                byte brightness = (byte)(Projectile.timeLeft * 3);
                byte alpha = (byte)(100f * (brightness / 255f));
                return new Color(brightness, brightness, brightness, alpha);
            }
            return new Color(255, 255, 255, 100);
        }
        /// <summary>
        /// 命中敌人：播爆炸音，把判定框从 30×30 撑到 150×150（以原中心为锚点），并喷一大圈 DungeonSpirit 尘
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            Projectile.position.X = Projectile.position.X + Projectile.width / 2;
            Projectile.position.Y = Projectile.position.Y + Projectile.height / 2;
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.position.X = Projectile.position.X - Projectile.width / 2;
            Projectile.position.Y = Projectile.position.Y - Projectile.height / 2;
            for (int i = 0; i < 30; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DungeonSpirit, 0f, 0f, 100, default(Color), 1.2f);
                Main.dust[d].velocity *= 3f;
                if (Main.rand.Next(2) == 0)
                {
                    Main.dust[d].scale = 0.5f;
                    Main.dust[d].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 60; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DungeonSpirit, 0f, 0f, 100, default(Color), 1.7f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 5f;
                d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DungeonSpirit, 0f, 0f, 100, default(Color), 1f);
                Main.dust[d].velocity *= 2f;
            }
        }
    }
}
