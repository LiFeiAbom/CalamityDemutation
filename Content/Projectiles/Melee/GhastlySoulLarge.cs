using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 聚魂弹（GhastlySoulLarge，移植自 CalamityEntropy，CE 侧标明是灾厄同名弹幕的搬运版）：
    /// 聚魂分形投掷式中沿剑尖撒出的追踪灵魂，伤害是手持弹幕的 1/7（由生成方传进来）。
    /// 出手前 20 帧只是散开，之后以 1 的追踪系数贴向 900 范围内最近的敌人（追得越久惯性越大、
    /// 越难转向——<c>ai[1]</c> 为追踪强度，生成方传 1）；离主人超过 900 像素则往回收，
    /// 主人不在或倒下时寿命压到 30 帧。命中过一次后每次命中伤害 ×0.88。
    /// 本体不绘制（贴图是工程通用的隐形占位，表现全交给 Plum 色星屑），
    /// 死亡时按 1/3 伤害对半径 230 内的敌人补一次爆炸判定，同时炸一圈 25 道细线粒子与 17 颗彩焰尘。
    /// </summary>
    internal class GhastlySoulLarge:ModProjectile
    {
        /// <summary>本体无贴图，使用工程通用的 InvisibleProj 占位（CE 用的是它自己的 Invisible）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>寿命（CE 的 TimeLeft 常量，AI 里用它划分「散开期」与「离屏回收」）</summary>
        private const int TimeLeftMax = 660;
        /// <summary>追踪速度（CE 的 VoidEdge.ShootSpeed 内联值）</summary>
        private const float ShootSpeed = 10f;
        /// <summary>出手后只散开、不追踪的帧数</summary>
        private const int SpreadOutTime = 20;
        /// <summary>死亡爆炸的伤害除数（CE 的 VoidEdge.SoulsPerSwing 内联值）</summary>
        private const int SoulsPerSwing = 3;
        /// <summary>追踪强度自衰减：每帧 -0.01，从 1 降到 0 的过程中惯性 20 → 90，越追越难转向</summary>
        private float homingBuff = 1f;
        /// <summary>追踪前的 extraUpdates（CE 存于 EGlobalProjectile.StoredEU，这里改为本类私有字段，-1 表示还没存过）</summary>
        private int storedExtraUpdates = -1;
        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;                     // 只穿透一次，命中后由 OnKill 补爆炸
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = TimeLeftMax;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
        }
        public override void AI()
        {
            if (homingBuff > 0)
            {
                homingBuff -= 0.01f;
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.9f);
            // 散开期过后每两帧在身后撒一颗 Plum 色星屑（CE 的 PRT_SparkCal，本模组等价物 DRK_Spark）
            if (Projectile.timeLeft % 2 == 0 && Projectile.timeLeft < TimeLeftMax - 10)
            {
                DRKLoader.AddParticle(new DRK_Spark(Projectile.Center + Main.rand.NextVector2Circular(15, 15) - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 10,
                    -Projectile.velocity * Main.rand.NextFloat(0.5f, 1.5f), false, Main.rand.Next(9, 13), 0.4f, Color.Plum));
            }
            float inertia = MathHelper.Lerp(20f, 90f, homingBuff) * Projectile.ai[1];
            float velocity = ShootSpeed * Projectile.ai[1];
            if (Main.player[Projectile.owner].active && !Main.player[Projectile.owner].dead)
            {
                float homingDistance = 900f;
                NPC target = Projectile.FindTargetWithinRange(homingDistance);
                if (Projectile.timeLeft < TimeLeftMax - SpreadOutTime && target != null)
                {
                    // 已过散开期且找到目标：锁定追踪（追踪期间 extraUpdates +1，失锁还原）
                    HomeInOnTarget(target, homingDistance, velocity, inertia, 1, true);
                }
                else if (Projectile.Distance(Main.player[Projectile.owner].Center) > homingDistance)
                {
                    // 超出范围又没目标：往回飞向主人
                    Vector2 moveDirection = Projectile.SafeDirectionTo(Main.player[Projectile.owner].Center, Vector2.UnitY);
                    Projectile.velocity = (Projectile.velocity * (inertia - 1f) + moveDirection * velocity) / inertia;
                }
            }
            else if (Projectile.timeLeft > 30)
            {
                Projectile.timeLeft = 30;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }
        /// <summary>
        /// 内联 CE 的 <c>CEUtils.HomingNPCBetter</c>（CE 把「追踪前的 extraUpdates」暂存在
        /// EGlobalProjectile.StoredEU，本模组没有那个全局类，改为本类私有字段）：命中索敌条件时把
        /// extraUpdates 提到「原值 + giveExtraUpdate」并按惯性式
        /// <c>(velocity * inertia + 朝向 * speed) / (inertia + 1)</c> 转向，失锁则把 extraUpdates 还原。
        /// CE 原方法还有 <c>maxAngleChage</c>/<c>forceSpeed</c> 两个可选参数，本弹幕用不到，没有一并搬。
        /// </summary>
        private void HomeInOnTarget(NPC target, float distRequired, float speed, float inertia, int giveExtraUpdate, bool ignoreDist)
        {
            if (!Projectile.friendly || target == null || !target.active)
            {
                return;
            }
            float curDist = Vector2.Distance(target.Center, Projectile.Center);
            if (storedExtraUpdates == -1)
            {
                storedExtraUpdates = Projectile.extraUpdates;
            }
            if (target.chaseable && (ignoreDist || curDist <= distRequired))
            {
                Projectile.extraUpdates = storedExtraUpdates + giveExtraUpdate;
                Vector2 home = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = (Projectile.velocity * inertia + home * speed) / (inertia + 1f);
            }
            else
            {
                Projectile.extraUpdates = storedExtraUpdates;
            }
        }
        /// <summary>命中过之后每次命中伤害 ×0.88（CE 原式的 <c>numHits</c> 判据）</summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
            {
                Projectile.damage = (int)(Projectile.damage * 0.88f);
            }
            if (Projectile.damage < 1)
            {
                Projectile.damage = 1;
            }
        }
        public override void OnKill(int timeLeft)
        {
            // 死亡补一次半径 230 的爆炸判定：伤害降到 1/3，穿透置为无限（只影响这一次 Damage 调用）
            Projectile.damage /= SoulsPerSwing;
            Projectile.penetrate = -1;
            Vector2 center = Projectile.Center;
            Projectile.width += 230;
            Projectile.height += 230;
            Projectile.Center = center;
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item100 with { Volume = 0.5f, Pitch = -0.3f }, Projectile.Center);
            // 炸一圈 25 道细线（含随机整体旋转，避免每次都长一个样）
            const int points = 25;
            float radians = MathHelper.TwoPi / points;
            Vector2 spinningPoint = Vector2.Normalize(new Vector2(-1f, -1f));
            float rotRando = Main.rand.NextFloat(0.1f, 2.5f);
            for (int k = 0; k < points; k++)
            {
                Vector2 velocity = spinningPoint.RotatedBy(radians * k).RotatedBy(-0.45f * rotRando);
                LineParticleCal line = new LineParticleCal();
                DRKLoader.NewParticle(line, Projectile.Center + velocity * 20.5f, velocity * 15, Color.Plum, 0.75f);
                line.Configure(false, 30);
            }
            for (int k = 0; k < 17; k++)
            {
                Dust dust2 = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowTorch, new Vector2(14, 14).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.8f));
                dust2.scale = Main.rand.NextFloat(1.15f, 1.45f);
                dust2.noGravity = true;
                dust2.color = Color.Plum;
            }
        }
    }
}
