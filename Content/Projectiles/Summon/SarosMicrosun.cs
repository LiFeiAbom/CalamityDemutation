using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 微缩太阳（SarosMicrosun，按灾厄 2.0 的 `Projectiles/Summon/SarosMicrosun` 移植）——
    /// 星律之握览那道辐光光环定期召出的重击弹（<see cref="SarosAura"/>）。
    /// 判定 62×64、贴图 6 帧循环、穿地形、命中即消失（穿透 1）、每名敌人 4 帧局部无敌；
    /// 前 30 帧只轻微加速（×1.01/帧），之后追 2000 像素内的敌人（速度 19、7:1 插值）。
    /// </summary>
    internal class SarosMicrosun:ModProjectile
    {
        /// <summary>贴图为 6 帧竖直序列、标记为仆从射弹（照源）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：62×64 判定、穿地形、友方、仆从射弹但**不占召唤栏**（minionSlots = 0）、
        /// 免受水影响、穿透 1、存活 600 帧、起始全透明（AI 里淡入）、每名敌人 4 帧局部无敌（源原样）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 64;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>
        /// AI：逐帧淡入 → 每 8 帧推进一帧动画（6 帧循环）→ 前 30 帧按 ×1.01 轻微加速，
        /// 之后在 2000 像素内追敌（速度 19、按 7:1 插值平滑转向）。
        /// </summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 9;
                if (Projectile.alpha < 0)
                {
                    Projectile.alpha = 0;
                }
            }
            Player player = Main.player[Projectile.owner];
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 8)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Projectile.type])
            {
                Projectile.frame = 0;
            }
            if (Projectile.ai[0]++ < 30f)
            {
                Projectile.velocity *= 1.01f;
            }
            else
            {
                NPC potentialTarget = Projectile.Center.MinionHoming(2000f, player);
                if (potentialTarget != null)
                {
                    Projectile.velocity = (Projectile.velocity * 7f + Projectile.SafeDirectionTo(potentialTarget.Center) * 19f) / 8f;
                }
            }
        }
    }
}
