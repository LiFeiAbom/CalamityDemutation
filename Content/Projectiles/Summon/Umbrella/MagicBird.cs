using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon.Umbrella
{
    /// <summary>
    /// 魔法鸟（MagicBird，按 CI 的 MagicBirdOld 移植；类名去掉 Old 后缀）——
    /// 光阴流时伞的礼帽随机抛出的追踪工具之一：14×14、占 0 栏、穿地形、存活 180 帧，
    /// 7 帧动画（每 6 帧一换）、按速度翻面；开场 30 帧只直飞并留下冰杖尘轨，
    /// 30 帧后在 <see cref="MagicHat.Range"/> 内追敌（速度 32、按 15:1 插值）。
    /// </summary>
    internal class MagicBird:ModProjectile
    {
        /// <summary>7 帧动画 + 4 格残影（模式 0）+ 仆从射弹（源原样）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 7;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>基础属性（源原样）：14×14、占 0 栏、穿地形、额外 1 次更新、起始全透明、180 帧</summary>
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 180;
        }
        /// <summary>
        /// AI：按速度翻面与旋转 → 推进 7 帧动画 → 每 20 帧洒一粒冰杖尘 → 30 帧后开始追敌 → 逐帧淡入
        /// </summary>
        public override void AI()
        {
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 7)
            {
                Projectile.frame = 0;
            }
            Projectile.ai[0]++;
            Projectile.ai[1]++;
            if (Projectile.ai[0] >= 20f)
            {
                Vector2 speed = Projectile.velocity * Main.rand.NextFloat(0.3f, 0.6f);
                int dust = Dust.NewDust(Projectile.Center, 1, 1, DustID.IceRod, speed.X, speed.Y, 50, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Projectile.ai[0] = 0f;
            }
            if (Projectile.ai[1] > 30f)
            {
                HomingAI();
            }
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 15;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
        }
        /// <summary>
        /// 追踪：优先取玩家用召唤武器标记的目标，否则取 <see cref="MagicHat.Range"/> 内最近的敌人，
        /// 以速度 32、按 15:1 插值转向（源原样）。
        /// </summary>
        private void HomingAI()
        {
            Player player = Main.player[Projectile.owner];
            int targetIdx = -1;
            float homingRange = MagicHat.Range;
            bool hasTarget = false;
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float dist = (Projectile.Center - npc.Center).Length();
                    if (dist < homingRange)
                    {
                        targetIdx = player.MinionAttackTargetNPC;
                        homingRange = dist;
                        hasTarget = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Main.npc.Length; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc == null || !npc.active || !npc.CanBeChasedBy(Projectile, false))
                    {
                        continue;
                    }
                    float dist = (Projectile.Center - npc.Center).Length();
                    if (dist < homingRange)
                    {
                        targetIdx = i;
                        homingRange = dist;
                        hasTarget = true;
                    }
                }
            }
            if (hasTarget)
            {
                NPC target = Main.npc[targetIdx];
                Vector2 homingVector = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 32f;
                const float homingRatio = 15f;
                Projectile.velocity = (Projectile.velocity * homingRatio + homingVector) / (homingRatio + 1f);
            }
        }
        /// <summary>消亡：喷 10 粒冰杖尘（源原样）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 speed = new Vector2(Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-7f, 7f));
                int dust = Dust.NewDust(Projectile.Center, 1, 1, DustID.IceRod, speed.X, speed.Y, 50, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
        /// <summary>绘制色固定为亮黄（源原样）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(255, 239, 0, Projectile.alpha);
        /// <summary>用残影绘制替代默认绘制（源原样）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
