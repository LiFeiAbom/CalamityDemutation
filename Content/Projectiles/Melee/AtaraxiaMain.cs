using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 禅心剑 · 主弹（AtaraxiaMain）—— 挥砍时射出的那枚「爆炸导弹」（照搬灾厄 2.0.4 的 <c>AtaraxiaMain</c>）。
    /// 贴图是 5 帧横向动画；因为穿透只有 1，命中即亡，消亡时在原地生成 <see cref="AtaraxiaBoom"/>（ai[0]=1、
    /// 半径 130 的小爆发，伤害取本弹的一半），同时洒两轮尘、一圈发光火花与三层火焰脉冲环。
    /// 两处偏离：① 灾厄原码的 DustID 写的是魔法数字（267 = RainbowMk2、278 = FireworksRGB），此处改用具名常量；
    /// ② 补写了 PvP 用的 OnHitPlayer（本工程要求命中钩子成对）。
    /// </summary>
    internal class AtaraxiaMain : ModProjectile
    {
        // ── 静态字段 ──
        /// <summary>贴图横向帧数</summary>
        private const int NumAnimationFrames = 5;
        /// <summary>换帧间隔（帧计数器超过它就进一帧）</summary>
        private const int AnimationFrameTime = 9;
        // ── 实例字段 ──
        /// <summary>存活帧计数：前 8 帧不撒粒子，避免刚出膛时效果堆在玩家手里</summary>
        public int time = 0;
        // ── 生命周期方法 ──
        /// <summary>贴图切成 5 帧横向动画</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = NumAnimationFrames;
        }
        /// <summary>基础属性：12×12、友方近战、穿透 1、每帧更新 6 格（extraUpdates 5）、不碰撞物块（改用圆形判定）、存活 300 帧、本地免疫 -1</summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 300;
        }
        /// <summary>
        /// 每帧：按速度摆正旋转角与绘制偏移、发红光、离玩家 1400 像素内持续撒火花与彩虹尘、推进 5 帧动画。
        /// 两个火花块的触发条件完全相同，只有寿命（8 / 2）与缩放（0.8 / 1.9）不同——灾厄原码如此，未合并。
        /// </summary>
        public override void AI()
        {
            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);
            DrawOffsetX = -40;
            DrawOriginOffsetY = -3;
            DrawOriginOffsetX = 18;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.45f, 0.1f, 0.1f);
            if (time > 8f && targetDist < 1400f)
            {
                DRKLoader.AddParticle(new DRK_Spark(Projectile.Center - Projectile.velocity * 1.5f, -Projectile.velocity * Main.rand.NextFloat(0.3f, 1.5f), false, 8, 0.8f, Color.Lerp(Color.DarkOrchid, Color.IndianRed, Main.rand.NextFloat(0, 1)) * 0.7f));
            }
            if (time > 8f && targetDist < 1400f)
            {
                DRKLoader.AddParticle(new DRK_Spark(Projectile.Center + Projectile.velocity * 2f, Projectile.velocity, false, 2, 1.9f, Color.Lerp(Color.DarkOrchid, Color.IndianRed, Main.rand.NextFloat(0, 1)) * 0.85f));
            }
            if (time > 8 && Main.rand.NextBool())
            {
                Vector2 dustvel = -Projectile.velocity;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, dustvel * Main.rand.NextFloat(0.1f, 1.2f), 0, default, Main.rand.NextFloat(0.7f, 0.9f));
                dust.noGravity = true;
                dust.color = Color.Lerp(Color.DarkOrchid, Color.IndianRed, Main.rand.NextFloat(0, 1));
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter > AnimationFrameTime)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= NumAnimationFrames)
                Projectile.frame = 0;
            time++;
        }
        /// <summary>命中敌人：挂原版暗影焰 180 帧（敌怪用的就是原版那条；玩家侧的增强版暗影焰另见 Buffs/NegativeBuffs/Shadowflame）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
        }
        /// <summary>命中玩家（PvP）：与 OnHitNPC 同构，挂原版暗影焰</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
        }
        /// <summary>
        /// 消亡（命中或到寿）：播死亡音、原地炸出半径 130 的小爆发，再撒两轮尘（10 颗火彩尘 + 10 颗发光火花）
        /// 与三层「火焰爆炸」脉冲环。小爆发由 <see cref="AtaraxiaBoom"/> 带 ai[0]=1 生成，故不画花瓣尘。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath55, Projectile.Center);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<AtaraxiaBoom>(), Projectile.damage / 2, 0, Projectile.owner, 1f, 0f, 0f);
            }
            for (int k = 0; k < 10; k++)
            {
                Vector2 velocity = new Vector2(20, 20).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.2f);
                float colorRando = Main.rand.NextFloat(0, 1);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + velocity, DustID.FireworksRGB, velocity * Main.rand.NextFloat(0.2f, 1f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.3f, 0.65f);
                dust.color = Color.Lerp(Color.DarkOrchid, Color.IndianRed, colorRando);
                dust.noLight = true;
                dust.noLightEmittence = true;
            }
            for (int k = 0; k < 10; k++)
            {
                Vector2 velocity = new Vector2(15, 15).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.2f);
                float colorRando = Main.rand.NextFloat(0, 1);
                GlowSparkCal spark = new GlowSparkCal();
                DRKLoader.NewParticle(spark, Projectile.Center + velocity, velocity, Color.Lerp(Color.DarkOrchid, Color.IndianRed, colorRando), Main.rand.NextFloat(0.015f, 0.025f));
                spark.Configure(false, 11, new Vector2(2.2f, 0.9f), true);
            }
            for (float k = 0; k < 3; k++)
            {
                float colorRando = Main.rand.NextFloat(0, 1);
                int partLifetime = Main.rand.Next(13, 15 + 1);
                float scale = Main.rand.NextFloat(0.12f, 0.18f);
                Vector2 spawnPos = Projectile.Center + (Main.rand.NextVector2Circular(20, 20) * (k + 1));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(spawnPos, Vector2.Zero, Color.Lerp(Color.DarkOrchid, Color.IndianRed, colorRando) * 0.6f, "CalamityDemutation/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.07f, scale, partLifetime));
            }
        }
        /// <summary>圆形判定（内联灾厄 CollisionUtils.CircularHitboxCollision）：半径 30</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle center = new Rectangle((int)Projectile.Center.X, (int)Projectile.Center.Y, 1, 1);
            if (center.Intersects(targetHitbox))
                return true;
            float closest = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.TopRight()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomRight()));
            return closest <= 30f;
        }
    }
}
