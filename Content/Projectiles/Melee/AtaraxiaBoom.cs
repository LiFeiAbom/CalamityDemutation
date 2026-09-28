using System;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 禅心剑 · 爆炸区（AtaraxiaBoom）—— 隐形的高伤爆炸判定（照搬灾厄 2.0.4 的 <c>AtaraxiaBoom</c>）。
    /// 靠 <c>ai[0]</c> 分成两种用法（本武器的两处生成点各用一种）：
    /// · <c>ai[0] = 0</c>：真近战命中生成 —— 半径 200 的**大型爆发**，并在 OnKill 里画 6 瓣玫瑰线花瓣尘 + 5 颗火花；
    /// · <c>ai[0] = 1</c>：主弹消亡生成 —— 半径 130 的**小型爆发**，完全不画尘。
    /// 源里这个「按 ai[0] 决定画不画尘」没有任何注释，看着像有意为之（真近战给足视觉反馈、弹幕命中只给伤害），照抄。
    /// 存活只有 1 帧，判定在消亡前那一帧由 <c>Damage()</c> 的碰撞结出。
    /// </summary>
    internal class AtaraxiaBoom : ModProjectile
    {
        /// <summary>无贴图：借用工程内的隐形贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>基础属性：8×8、友方近战、穿透无限、存活 1 帧、不碰撞物块、本地免疫 -1（同一目标只命中一次）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        /// <summary>每帧只发紫光（灾厄原码里的 Owner / targetDist 两个局部量声明后从未使用，按本工程惯例略去）</summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.3f, 0.1f, 0.45f);
        }
        /// <summary>
        /// 消亡时：只有 <c>ai[0] == 0</c>（真近战生成的那种）才画 **6 瓣玫瑰线花瓣尘** + 5 颗火花。
        /// 花瓣的半径按 <c>sin(θ₀ + θ × 瓣数)</c> 调制，θ 以 0.05 弧度步进绕满一圈；
        /// 半径里那个 +0.5 的补偿项是让花瓣首尾接回原点、而不是往回折（灾厄原注释如此说明）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] == 0)
            {
                int flowerPetalCount = 6;
                float thetaDelta = new Vector2(3, 3).RotatedByRandom(100).ToRotation();
                float weaveDistanceMin = 0.5f;
                float weaveDistanceOutwardMax = 10f;
                float weaveDistanceInner = 0.5f;
                for (float theta = 0f; theta < MathHelper.TwoPi; theta += 0.05f)
                {
                    float colorRando = Main.rand.NextFloat(0, 1);
                    Vector2 velocity = theta.ToRotationVector2() *
                        (weaveDistanceMin +
                        // 0.5 是为了防止花瓣绕回自身；加上它之后恰好回到 (0,0) 而不是往回折
                        (float)(Math.Sin(thetaDelta + theta * flowerPetalCount) + 0.5f + weaveDistanceInner) *
                        weaveDistanceOutwardMax);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, velocity);
                    dust.noGravity = true;
                    dust.scale = 1.15f;
                    dust.color = Color.Lerp(Color.DarkOrchid, Color.IndianRed, colorRando);
                }
                for (int k = 0; k < 5; k++)
                {
                    Vector2 velocity = new Vector2(15, 15).RotatedByRandom(100) * Main.rand.NextFloat(0.6f, 1.2f);
                    float colorRando = Main.rand.NextFloat(0, 1);
                    DRKLoader.AddParticle(new DRK_Spark(Projectile.Center + velocity, velocity, true, 50, Main.rand.NextFloat(0.7f, 0.95f), Color.Lerp(Color.DarkOrchid, Color.IndianRed, colorRando)));
                }
            }
        }
        /// <summary>
        /// 多段命中衰减（**2.0.4 版**，用户点名）：每命中一个新目标就把弹幕自身伤害乘 0.88（下限 1），无硬地板。
        /// 1.4.4 版写的是按 numHits 线性插值到最低 20%，是本工程刻意偏离的一处。
        /// </summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = (int)(Projectile.damage * 0.88f);
            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }
        /// <summary>
        /// 圆形判定（内联灾厄 <c>CollisionUtils.CircularHitboxCollision</c>，写法照本工程 AbaddonCrit）：
        /// 圆心落在目标框内直接算命中，否则取圆心到目标框四角的最小距离与半径比较（四角近似，与灾厄一致）。
        /// 半径按 <c>ai[0]</c> 取 130（主弹消亡的小爆发）或 200（真近战命中的大爆发）。
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = Projectile.ai[0] == 1 ? 130f : 200f;
            Rectangle center = new Rectangle((int)Projectile.Center.X, (int)Projectile.Center.Y, 1, 1);
            if (center.Intersects(targetHitbox))
                return true;
            float closest = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.TopRight()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomRight()));
            return closest <= radius;
        }
    }
}
