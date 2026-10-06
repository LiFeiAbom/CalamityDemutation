using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 弑神者光球（GodSlayerOrb） - 弑神者法师头（GodSlayerVisage）套装「魔法攻击命中敌人时
    /// 释放弑神者烈焰与治疗烈焰」的追踪球（按经典版灾厄 Projectiles/Typeless/GodSlayerOrb.cs 1:1 移植）。
    /// 4×4、穿透 1、穿地形、存活 200 帧、额外 1 次更新；每帧在自身位置喷一粒 ShadowbeamStaff 尘
    /// （源裸数字 173 已 Cecil 反查，速度清零所以尘堆在原地），并以 12 像素/帧的上限追踪
    /// 600 像素内最近的、有视线的可追击敌人（速度按 20:1 插值逼近目标方向）；命中附加 200 帧弑神者地狱火。
    /// </summary>
    public class GodSlayerOrb : ModProjectile
    {
        /// <summary>
        /// 基础属性：4×4 碰撞箱、友方、穿透 1、穿地形、存活 200 帧、额外 1 次更新（经典版原样）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 1;
        }
        /// <summary>
        /// AI：喷尘 + 追踪 600 像素内最近的有视线敌人（限速 12，按 20:1 插值）
        /// </summary>
        public override void AI()
        {
            int orbDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default(Color), 2f);
            Main.dust[orbDust].noGravity = true;
            Main.dust[orbDust].velocity *= 0f;
            float targetX = Projectile.Center.X;
            float targetY = Projectile.Center.Y;
            float nearest = 600f;
            bool found = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(Projectile, false) && Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                {
                    float npcX = npc.position.X + npc.width / 2;
                    float npcY = npc.position.Y + npc.height / 2;
                    float manhattan = Math.Abs(Projectile.position.X + Projectile.width / 2 - npcX) + Math.Abs(Projectile.position.Y + Projectile.height / 2 - npcY);
                    if (manhattan < nearest)
                    {
                        nearest = manhattan;
                        targetX = npcX;
                        targetY = npcY;
                        found = true;
                    }
                }
            }
            if (found)
            {
                const float speed = 12f;
                Vector2 center = new Vector2(Projectile.position.X + Projectile.width * 0.5f, Projectile.position.Y + Projectile.height * 0.5f);
                float dirX = targetX - center.X;
                float dirY = targetY - center.Y;
                float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                dist = speed / dist;
                dirX *= dist;
                dirY *= dist;
                Projectile.velocity.X = (Projectile.velocity.X * 20f + dirX) / 21f;
                Projectile.velocity.Y = (Projectile.velocity.Y * 20f + dirY) / 21f;
            }
        }
        /// <summary>
        /// 命中敌人：附加 200 帧弑神者地狱火（按现代/经典两版分别查找，未安装则不生效）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 200);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 200);
        }
        /// <summary>
        /// PvP 命中：与 OnHitNPC 同构，给玩家挂 200 帧弑神者地狱火
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 200);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 200);
        }
    }
}
