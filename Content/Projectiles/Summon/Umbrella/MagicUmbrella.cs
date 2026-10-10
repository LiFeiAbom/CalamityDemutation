using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon.Umbrella
{
    /// <summary>
    /// 青色魔法伞（MagicUmbrella，按 CI 的 MagicUmbrellaOld 移植；类名去掉 Old 后缀）——
    /// 光阴时流伞的礼帽随机抛出的近战工具：14×14、占 0 栏、穿透 10、穿地形、
    /// 每名敌人 10 帧局部无敌、存活 180 帧、开 10 格残影（模式 1）。
    /// 开场 30 帧只自旋与淡入，之后在 <see cref="MagicHat.Range"/> 内追敌（速度 9）；
    /// 贴到 500 像素内且在冷却结束时发动一次速度 9 的冲刺（`ai[0] = 2`，持续 40 帧）。
    /// </summary>
    /// <remarks>
    /// 命中时 **1/4 概率**从目标上方砸下 1~2 根魔法球棒（<see cref="MagicBat"/>），
    /// 球棒伤害 = 本弹幕伤害 × rand(0.3, 0.6)、击退 × rand(0.7, 1)（源原样）。
    /// </remarks>
    internal class MagicUmbrella:ModProjectile
    {
        /// <summary>开场计时（源里的实例字段）</summary>
        private int counter = 0;
        /// <summary>开 10 格残影（模式 1）+ 仆从射弹（源原样）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>基础属性（源原样）：14×14、占 0 栏、穿透 10、穿地形、180 帧、每名敌人 10 帧局部无敌、起始全透明</summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 0f;
            Projectile.timeLeft = 180;
            Projectile.penetrate = 10;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.alpha = 255;
        }
        /// <summary>
        /// AI：自旋 + 淡入 → 与同类互相排斥 → 开场 30 帧只记时 → 索敌 / 回位（同其它工具那套
        /// "远 200 像素外贴近、以内后撤"的写法）→ 冷却结束后贴到 500 像素内即冲刺。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.rotation += 0.075f;
            Projectile.alpha -= 50;
            float homingRange = MagicHat.Range;
            const float separationPush = 0.05f;
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile other = Main.projectile[i];
                bool sameType = other.type == ModContent.ProjectileType<MagicUmbrella>();
                if (i != Projectile.whoAmI && other.active && other.owner == Projectile.owner && sameType
                    && Math.Abs(Projectile.position.X - other.position.X) + Math.Abs(Projectile.position.Y - other.position.Y) < Projectile.width)
                {
                    Projectile.velocity.X += Projectile.position.X < other.position.X ? -separationPush : separationPush;
                    Projectile.velocity.Y += Projectile.position.Y < other.position.Y ? -separationPush : separationPush;
                }
            }
            if (counter <= 30)
            {
                counter++;
            }
            bool dashing = false;
            if (Projectile.ai[0] == 2f)
            {
                Projectile.ai[1] += 1f;
                Projectile.extraUpdates = 1;
                if (Projectile.ai[1] > 40f)
                {
                    Projectile.ai[1] = 1f;
                    Projectile.ai[0] = 0f;
                    Projectile.extraUpdates = 0;
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
            bool hasTarget = false;
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
                        hasTarget = true;
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
                        hasTarget = true;
                    }
                }
            }
            float leashDistance = hasTarget ? 2200f : 1600f;
            if (Vector2.Distance(player.Center, Projectile.Center) > leashDistance)
            {
                Projectile.ai[0] = 1f;
                Projectile.netUpdate = true;
            }
            if (hasTarget && Projectile.ai[0] == 0f)
            {
                Vector2 toTarget = targetCenter - Projectile.Center;
                float dist = toTarget.Length();
                toTarget.Normalize();
                if (dist > 200f)
                {
                    toTarget *= 9f;
                    Projectile.velocity = (Projectile.velocity * 40f + toTarget) / 41f;
                }
                else
                {
                    toTarget *= -4f;
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
                if (playerDist < 150f && returning && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
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
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += Main.rand.Next(1, 4);
            }
            if (Projectile.ai[1] > 40f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 0f && Projectile.ai[1] == 0f && hasTarget && homingRange < 500f)
            {
                Projectile.ai[1] += 1f;
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.ai[0] = 2f;
                    Vector2 dash = targetCenter - Projectile.Center;
                    dash.Normalize();
                    Projectile.velocity = dash * 9f;
                    Projectile.netUpdate = true;
                }
            }
        }
        /// <summary>绘制色固定为青蓝（源原样）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(75, 255, 255, Projectile.alpha);
        /// <summary>用残影绘制替代默认绘制（源原样）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
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
        /// <summary>命中 NPC：1/4 概率从上方砸下 1~2 根魔法球棒（源原样）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnBats(target);
        }
        /// <summary>PvP 命中玩家：与 OnHitNPC 同构</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            SpawnBats(target);
        }
        /// <summary>
        /// 从目标上方 500~800 像素、水平 ±400 像素处生成 1~2 根魔法球棒，按约 29 像素/帧的速度砸向目标；
        /// 伤害与击退都按随机比例折算（源原样）。
        /// </summary>
        private void SpawnBats(Entity target)
        {
            if (!Main.rand.NextBool(4))
            {
                return;
            }
            for (int n = 0; n < Main.rand.Next(1, 3); n++)
            {
                float spawnX = target.position.X + Main.rand.Next(-400, 400);
                float spawnY = target.position.Y - Main.rand.Next(500, 800);
                float dirX = target.position.X + target.width / 2 - spawnX;
                float dirY = target.position.Y + target.height / 2 - spawnY;
                dirX += Main.rand.Next(-100, 101);
                const float speed = 29f;
                float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                float scale = speed / dist;
                dirX *= scale;
                dirY *= scale;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnX, spawnY, dirX, dirY,
                    ModContent.ProjectileType<MagicBat>(),
                    (int)(Projectile.damage * Main.rand.NextFloat(0.3f, 0.6f)),
                    Projectile.knockBack * Main.rand.NextFloat(0.7f, 1f), Projectile.owner, 0f, 0f);
            }
        }
    }
}
