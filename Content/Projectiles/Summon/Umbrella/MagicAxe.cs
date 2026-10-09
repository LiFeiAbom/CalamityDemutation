using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon.Umbrella
{
    /// <summary>
    /// 魔法斧（MagicAxe，按 CI 的 `MagicAxeOld` 移植；类名去掉 `Old` 后缀）——
    /// 光阴流时伞的礼帽（<see cref="MagicHat"/>）随机抛出的重击工具之一。
    /// 52×52、穿地形、无限穿透、每名敌人 8 帧局部无敌、存活 180 帧，开场 30 帧只做自旋与防重叠，
    /// 之后在 <see cref="MagicHat.Range"/> 内追敌；贴到 500 像素内会**冲刺**一次
    /// （`ai[0] = 2` 状态，速度 16、额外更新 4 次持续 30 帧）。
    /// </summary>
    /// <remarks>
    /// 命中附加四味减益（照 CI）：硫火 BrimstoneFlames、寒冰 GlacialState、瘟疫 Plague、神圣火 HolyFlames。
    /// 其中 GlacialState 在灾厄 2.2.2 里已被删除，本工程按既有口径用 ApplyCalamityBuffWithFallback
    /// 退回原版冻伤 BuffID.Frozen（与 Terratomere 同一处理）。
    /// </remarks>
    internal class MagicAxe:ModProjectile
    {
        /// <summary>冲刺计时（源里的实例字段 counter，只在本弹幕自己的 AI 里用）</summary>
        private int counter = 0;
        /// <summary>开 4 格残影缓存（模式 0），并标记为仆从射弹</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>基础属性（源原样）：52×52、不占栏、180 帧、无限穿透、每名敌人 8 帧局部无敌、起始全透明</summary>
        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 0;
            Projectile.timeLeft = 180;
            Projectile.penetrate = -1;
            Projectile.minion = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.alpha = 255;
        }
        /// <summary>
        /// AI：自旋 + 淡入 → 与同类互相排斥（防重叠）→ 开场 30 帧只记时 → 索敌（优先玩家标记的目标）→
        /// 离主人过远则回位 → 追敌（远 200 像素外以 24 速度贴近、以内则以 12 速度后撤）→
        /// 贴到 500 像素内且不在冲刺状态时发动一次速度 16 的冲刺（额外更新 4 次、持续 30 帧）。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.rotation += 0.075f;
            Projectile.alpha -= 50;
            const float separationPush = 0.05f;
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile other = Main.projectile[i];
                bool sameType = other.type == ModContent.ProjectileType<MagicAxe>();
                if (i != Projectile.whoAmI && other.active && other.owner == Projectile.owner && sameType
                    && Math.Abs(Projectile.position.X - other.position.X) + Math.Abs(Projectile.position.Y - other.position.Y) < Projectile.width)
                {
                    Projectile.velocity.X += Projectile.position.X < other.position.X ? -separationPush : separationPush;
                    Projectile.velocity.Y += Projectile.position.Y < other.position.Y ? -separationPush : separationPush;
                }
            }
            if (Projectile.ai[1] <= 30)
            {
                Projectile.ai[1]++;
                return;
            }
            bool dashing = false;
            if (Projectile.ai[0] == 2f)
            {
                counter += 1;
                Projectile.extraUpdates = 4;
                if (counter > 30)
                {
                    counter = 1;
                    Projectile.ai[0] = 0f;
                    Projectile.extraUpdates = 1;
                    Projectile.numUpdates = 0;
                    Projectile.netUpdate = true;
                }
                else
                {
                    dashing = true;
                }
            }
            if (dashing)
            {
                return;
            }
            Vector2 targetCenter = Projectile.position;
            float homingRange = MagicHat.Range;
            bool homeIn = false;
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float dist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (dist < homingRange)
                    {
                        homingRange = dist;
                        targetCenter = npc.Center;
                        homeIn = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Main.npc.Length; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.CanBeChasedBy(Projectile, false))
                    {
                        continue;
                    }
                    float dist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (dist < homingRange)
                    {
                        homingRange = dist;
                        targetCenter = npc.Center;
                        homeIn = true;
                    }
                }
            }
            float leashDistance = homeIn ? 2500f : 1200f;
            if (Vector2.Distance(player.Center, Projectile.Center) > leashDistance)
            {
                Projectile.ai[0] = 1f;
                Projectile.netUpdate = true;
            }
            if (homeIn && Projectile.ai[0] == 0f)
            {
                Vector2 toTarget = targetCenter - Projectile.Center;
                float dist = toTarget.Length();
                toTarget.Normalize();
                if (dist > 200f)
                {
                    toTarget *= 24f;
                    Projectile.velocity = (Projectile.velocity * 40f + toTarget) / 41f;
                }
                else
                {
                    toTarget *= -12f;
                    Projectile.velocity = (Projectile.velocity * 40f + toTarget) / 41f;
                }
            }
            else
            {
                bool returning = Projectile.ai[0] == 1f;
                float moveSpeed = returning ? 15f : 6f;
                Vector2 toPlayer = player.Center - Projectile.Center + new Vector2(0f, -60f);
                float playerDist = toPlayer.Length();
                if (playerDist > 200f && moveSpeed < 8f)
                {
                    moveSpeed = 8f;
                }
                if (playerDist < 400f && returning && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                if (playerDist > 2000f)
                {
                    Projectile.position.X = player.Center.X - Projectile.width / 2;
                    Projectile.position.Y = player.Center.Y - Projectile.height / 2;
                    Projectile.netUpdate = true;
                }
                if (playerDist > 70f)
                {
                    toPlayer.Normalize();
                    toPlayer *= moveSpeed;
                    Projectile.velocity = (Projectile.velocity * 40f + toPlayer) / 41f;
                }
                else if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
                {
                    Projectile.velocity.X = -0.15f;
                    Projectile.velocity.Y = -0.05f;
                }
            }
            if (counter > 0)
            {
                counter += Main.rand.Next(1, 4);
            }
            if (counter > 30)
            {
                counter = 0;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 0f && counter == 0 && homeIn && homingRange < 500f)
            {
                counter += 1;
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.ai[0] = 2f;
                    Vector2 dash = targetCenter - Projectile.Center;
                    dash.Normalize();
                    Projectile.velocity = dash * 16f;
                    Projectile.netUpdate = true;
                }
            }
        }
        /// <summary>绘制色固定为亮绿（源原样）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(0, 255, 111, Projectile.alpha);
        /// <summary>用残影绘制替代默认绘制（源原样）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>命中 NPC：附加四味减益（源原样；GlacialState 走兜底）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "BrimstoneFlames", 120);
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "GlacialState", 120, BuffID.Frozen);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Plague", 120);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
        }
        /// <summary>PvP 命中玩家：与 OnHitNPC 同构</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "BrimstoneFlames", 120);
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "GlacialState", 120, BuffID.Frozen);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Plague", 120);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
        }
        /// <summary>消亡：喷 10 粒彩虹火尘（源原样）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 speed = new Vector2(Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-7f, 7f));
                int dust = Dust.NewDust(Projectile.Center, 1, 1, DustID.RainbowTorch, speed.X, speed.Y, 160, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 0.75f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
