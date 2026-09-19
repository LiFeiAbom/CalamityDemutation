using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 虚空新星（VoidStarF，移植自 CalamityEntropy）：无星之夜命中敌人后朝四面八方炸出的追踪星辰。
    /// 出手先把速度砍半，此后一路微减速并朝 4000 范围内的目标加速偏转，贴近到 180 像素内改走直线扑上去；
    /// 寿命剩 40 帧起淡出。落地 16 帧内不参与命中判定。
    /// 本体不绘制（<see cref="PreDraw"/> 恒假），表现全交给粒子：<c>ai[2] &gt; 0</c> 时拖两条红色星屑
    /// （<see cref="HeavenfallStarCal"/>），否则冒蓝紫浓烟（<see cref="HeavySmokeParticle"/>）。
    /// <para>
    /// 与 CE 原版的差异：① <c>odp</c> 位置历史在 CE 里只写不读（没有任何绘制方），删掉；
    /// ② <c>CEUtils.getDistance</c> 内联为 <c>Vector2.Distance</c>；③ CE 的 <c>Hue</c> 常量属性内联为 0.55f。
    /// </para>
    /// </summary>
    internal class VoidStarF:ModProjectile
    {
        /// <summary>星屑的竖向拉伸系数（CE 的 PRT_HeavenfallStar.xScale，VoidStarF 生成时设成 0.14f）</summary>
        private const float TrailXScale = 0.14f;
        /// <summary>首帧标记：出手时把初速砍半，制造"炸开后骤停再加速"的手感</summary>
        private bool setv = true;
        /// <summary>落地后的帧数计数，前 16 帧不参与命中判定</summary>
        private int counter = 0;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Magic;   // 由近战武器生成时会覆写成近战
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 400;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.extraUpdates = 1;
            Projectile.ArmorPenetration = 40;
        }
        /// <summary>落地 16 帧内不判命中，避免刚炸开就贴脸全吃</summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (counter < 16)
            {
                return false;
            }
            return null;
        }
        public override void AI()
        {
            if (Projectile.ai[2] > 0)
            {
                // 拖尾：两颗红色星屑（CE 的 PRT_HeavenfallStar，已精确移植为 HeavenfallStarCal）
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
                for (int i = 0; i < 2; i++)
                {
                    HeavenfallStarCal star = new HeavenfallStarCal();
                    DRKLoader.NewParticle(star, i == 0 ? Projectile.Center : Projectile.Center - Projectile.velocity / 2f,
                        dir, new Color(255, 120, 120), Main.rand.NextFloat(0.6f, 1.3f) * 1.6f);
                    star.Configure(dir.ToRotation(), 14);
                    star.xScale = TrailXScale;
                }
            }
            else
            {
                // 形体：蓝→紫随全局时间来回渐变的浓烟，另有 1/3 概率再叠一层发光的紫色烟
                if (Main.rand.NextBool(2))
                {
                    Color smokeColor = Color.Lerp(
                        Color.DodgerBlue.MultiplyRGB(Projectile.ai[2] > 0 ? new Color(255, 80, 80) : Color.White),
                        Color.MediumVioletRed.MultiplyRGB(Projectile.DamageType == DamageClass.Magic ? new Color(255, 140, 140) : Color.White),
                        (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f));
                    HeavySmokeParticle smoke = new HeavySmokeParticle();
                    DRKLoader.NewParticle(smoke, Projectile.Center, Projectile.velocity * 0.5f, smokeColor, Main.rand.NextFloat(0.6f, 1.2f) * Projectile.scale);
                    smoke.Configure(0.28f, 20, 0, false, 0, true);
                    if (Main.rand.NextBool(3))
                    {
                        HeavySmokeParticle glow = new HeavySmokeParticle();
                        DRKLoader.NewParticle(glow, Projectile.Center, Projectile.velocity * 0.5f,
                            Main.hslToRgb(0.55f, 1, 0.7f).MultiplyRGB(Projectile.ai[2] > 0 ? new Color(255, 80, 80) : Color.White),
                            Main.rand.NextFloat(0.4f, 0.7f) * Projectile.scale);
                        glow.Configure(0.8f, 15, 0, true, 0.05f, true);
                    }
                }
            }
            counter++;
            if (setv)
            {
                setv = false;
                Projectile.velocity *= 0.5f;
            }
            Projectile.velocity *= 0.999f;
            if (Projectile.timeLeft < 360)
            {
                NPC target = Projectile.FindTargetWithinRange(4000f, false);
                if (target != null)
                {
                    Projectile.velocity *= 0.99f;
                    Vector2 v = Vector2.Normalize(target.Center - Projectile.Center);
                    Projectile.velocity += v * 0.4f;
                    // 贴近到 180 像素内就不再转向，直接沿当前方向全速扑上去
                    if (Vector2.Distance(Projectile.Center, target.Center) < 180f)
                    {
                        Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((target.Center - Projectile.Center).ToRotation());
                    }
                }
            }
            if (Projectile.timeLeft < 40)
            {
                Projectile.alpha += 255 / 40;
            }
            Projectile.rotation += 0.1f;
            Lighting.AddLight(Projectile.Center, 0.75f, 1f, 0.24f);
        }
        /// <summary>本体不绘制：全部表现交给星屑与浓烟粒子</summary>
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
