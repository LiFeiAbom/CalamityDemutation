using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon.Umbrella
{
    /// <summary>
    /// 魔法锤（MagicHammer，按 CI 的 MagicHammerOld 移植；类名去掉 Old 后缀）——
    /// 光阴流时伞的礼帽随机抛出的重击工具之一：60×60、穿透 6、穿地形、
    /// 每名敌人 6 帧局部无敌、存活 180 帧；起始全透明，10 帧后按 1/3 概率逐次淡入到 alpha 50。
    /// 开场 30 帧只自旋，之后锁定一个目标（记住其索引 `ai[0]`，目标失效则重挑），
    /// 航向按每帧 25% 的角速度对准目标，并**持续缓慢加速**（速度 +0.0025/帧）。
    /// </summary>
    internal class MagicHammer:ModProjectile
    {
        /// <summary>开场计时（源里的实例字段 counter，只在本弹幕自己的 AI 里用）</summary>
        private int counter = 0;
        /// <summary>开 4 格残影（模式 0）+ 仆从射弹（源原样）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>基础属性（源原样）：60×60、起始全透明、穿透 6、穿地形、180 帧、每名敌人 6 帧局部无敌</summary>
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.alpha = 255;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.penetrate = 6;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }
        /// <summary>
        /// AI：淡入 → 自旋 → 开场 30 帧只记时 → 锁定目标（`ai[0]` 存目标索引 + 1，0 表示无目标；
        /// `localAI[0]` 是重挑目标的冷却）→ 目标在 <see cref="MagicHat.Range"/> × 1.25 内则每帧把航向
        /// 朝目标转 25%，并持续加速。
        /// </summary>
        public override void AI()
        {
            Projectile.localAI[1] += 1f;
            if (Projectile.localAI[1] > 10f && Main.rand.NextBool(3))
            {
                Projectile.alpha -= 5;
                if (Projectile.alpha < 50)
                {
                    Projectile.alpha = 50;
                }
            }
            Projectile.rotation += 0.075f;
            if (counter <= 30)
            {
                counter++;
                return;
            }
            int targetIdx = -1;
            Vector2 targetCenter = Projectile.Center;
            float homingRange = MagicHat.Range;
            if (Projectile.localAI[0] > 0f)
            {
                Projectile.localAI[0] -= 1f;
            }
            Player player = Main.player[Projectile.owner];
            if (Projectile.ai[0] == 0f && Projectile.localAI[0] == 0f)
            {
                if (player.HasMinionAttackTargetNPC)
                {
                    NPC npc = Main.npc[player.MinionAttackTargetNPC];
                    if (npc.CanBeChasedBy(Projectile, false)
                        && (Projectile.ai[0] == 0f || Projectile.ai[0] == player.MinionAttackTargetNPC + 1))
                    {
                        float dist = Vector2.Distance(npc.Center, targetCenter);
                        if (dist < homingRange)
                        {
                            homingRange = dist;
                            targetCenter = npc.Center;
                            targetIdx = player.MinionAttackTargetNPC;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < Main.npc.Length; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.CanBeChasedBy(Projectile, false) && (Projectile.ai[0] == 0f || Projectile.ai[0] == i + 1))
                        {
                            float dist = Vector2.Distance(npc.Center, targetCenter);
                            if (dist < homingRange)
                            {
                                homingRange = dist;
                                targetCenter = npc.Center;
                                targetIdx = i;
                            }
                        }
                    }
                }
                if (targetIdx >= 0)
                {
                    Projectile.ai[0] = targetIdx + 1;
                    Projectile.netUpdate = true;
                }
            }
            if (Projectile.localAI[0] == 0f && Projectile.ai[0] == 0f)
            {
                Projectile.localAI[0] = 30f;
            }
            bool hasTarget = false;
            if (Projectile.ai[0] != 0f)
            {
                int idx = (int)(Projectile.ai[0] - 1f);
                if (Main.npc[idx].active && !Main.npc[idx].dontTakeDamage && Main.npc[idx].immune[Projectile.owner] == 0)
                {
                    float npcX = Main.npc[idx].position.X + Main.npc[idx].width / 2;
                    float npcY = Main.npc[idx].position.Y + Main.npc[idx].height / 2;
                    float manhattan = Math.Abs(Projectile.position.X + Projectile.width / 2 - npcX)
                        + Math.Abs(Projectile.position.Y + Projectile.height / 2 - npcY);
                    if (manhattan < MagicHat.Range * 1.25f)
                    {
                        hasTarget = true;
                        targetCenter = Main.npc[idx].Center;
                    }
                }
                else
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
            }
            if (hasTarget)
            {
                Vector2 toTarget = targetCenter - Projectile.Center;
                float currentAngle = Projectile.velocity.ToRotation();
                float wantedAngle = toTarget.ToRotation();
                float angleDelta = MathHelper.WrapAngle(wantedAngle - currentAngle);
                Projectile.velocity = Projectile.velocity.RotatedBy(angleDelta * 0.25, default);
            }
            float speed = Projectile.velocity.Length();
            Projectile.velocity.Normalize();
            Projectile.velocity *= speed + 0.0025f;
        }
        /// <summary>用残影绘制替代默认绘制（源原样）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>绘制色固定为炽橙（源原样）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(255, 56, 0, Projectile.alpha);
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
    }
}
