using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 血炎之魂（BloodflareSoul） - 血炎射手套装按 [键] 释放出的波尔特加斯特迷失灵魂
    /// （移植自经典版灾厄 Projectiles/Ranged/BloodflareSoul.cs，由 CalamityDemutationPlayer 的按键块一次喷出 16 枚）。
    /// 行为照经典版：4 帧循环动画 + 血焰尘尾；离主人 600 像素以上时折返；
    /// 否则在 400 的曼哈顿距离内索敌，以速度 11、惯性 20 平滑转向（主人死亡后 30 帧内消散）。
    /// 消失时播放死亡音、把判定框撑到 110×110 并结算一次范围伤害（源的 OnKill 写法）。
    /// </summary>
    internal class BloodflareSoul:ModProjectile
    {
        /// <summary>4 帧循环动画（贴图为 32x192 = 4 张 32x48）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        /// <summary>
        /// 基础属性：30x30 碰撞箱、缩放 1.2、半透明、友方、无视水与地形、远程伤害类、穿透 1、存活 900 帧（照经典版）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.scale = 1.2f;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 900;
        }
        /// <summary>
        /// AI：推进 4 帧动画 → 撒血焰尘 → 朝向速度方向 → 点紫光；
        /// 随后按「离主人超过 600 先折返、否则 400 曼哈顿距离内索敌」两段处理（转向速率取自弹幕的 ai[1]）。
        /// </summary>
        public override void AI()
        {
            // ── 4 帧循环动画（每 6 帧换一帧）──
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            // ── 血焰尘尾 ──
            int redDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RedTorch, 0f, 0f, 0, default, 1f);
            Main.dust[redDust].velocity *= 0.1f;
            Main.dust[redDust].scale = 1.3f;
            Main.dust[redDust].noGravity = true;
            // 朝向速度方向（经典版写法：Atan2 - 90°，与其贴图朝向一致）
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.9f);
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                // 主人死亡/退场：30 帧内自然消散
                if (Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
                return;
            }
            // 转向速率取自生成时写入的 ai[1]（0.5~1.5）：值越大折返越快、吸附越紧
            float turnRate = 30f * Projectile.ai[1];
            float homeSpeed = 6f * Projectile.ai[1];
            // ── 离主人太远 → 折返 ──
            if (Projectile.Distance(owner.Center) > 600f)
            {
                Vector2 backDir = Projectile.DirectionTo(owner.Center);
                if (backDir.HasNaNs())
                    backDir = Vector2.UnitY;
                Projectile.velocity = (Projectile.velocity * (turnRate - 1f) + backDir * homeSpeed) / turnRate;
                return;
            }
            // ── 400（曼哈顿距离）内索敌：取最近且视线可达的敌人，速度 11、惯性 20 ──
            Vector2 targetCenter = Projectile.Center;
            float closestDistance = 400f;
            bool foundTarget = false;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false) || !Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;
                float manhattan = Math.Abs(Projectile.Center.X - npc.Center.X) + Math.Abs(Projectile.Center.Y - npc.Center.Y);
                if (manhattan < closestDistance)
                {
                    closestDistance = manhattan;
                    targetCenter = npc.Center;
                    foundTarget = true;
                }
            }
            if (foundTarget)
            {
                Vector2 desiredVelocity = (targetCenter - Projectile.Center).SafeNormalize(Vector2.UnitX) * 11f;
                Projectile.velocity = (Projectile.velocity * 20f + desiredVelocity) / 21f;
            }
        }
        /// <summary>
        /// 余 85 帧内淡出（经典版 GetAlpha 原样）
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 85)
            {
                byte b2 = (byte)(Projectile.timeLeft * 3);
                byte a2 = (byte)(100f * (b2 / 255f));
                return new Color(b2, b2, b2, a2);
            }
            return new Color(255, 255, 255, 100);
        }
        /// <summary>
        /// 消散时：播死亡音 → 判定框撑到 110×110 → 撒一圈血焰尘 → 结算一次范围伤害（经典版 OnKill 原样）
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath39, Projectile.position);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 110;
            Projectile.position.X -= Projectile.width / 2;
            Projectile.position.Y -= Projectile.height / 2;
            const int dustCount = 36;
            Vector2 ringBase = Projectile.velocity.SafeNormalize(Vector2.UnitY) * new Vector2(Projectile.width / 2f, Projectile.height) * 0.75f;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 ringPos = ringBase.RotatedBy((i - (dustCount / 2 - 1)) * MathHelper.TwoPi / dustCount) + Projectile.Center;
                Vector2 offset = ringPos - Projectile.Center;
                int dust = Dust.NewDust(ringPos + offset, 0, 0, DustID.RedTorch, offset.X * 1.5f, offset.Y * 1.5f, 100, default, 2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = true;
                Main.dust[dust].velocity = offset;
            }
            Projectile.Damage();
        }
    }
}
