using CalamityDemutation.Particles;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 纳米激光（Nano Beam） - 逐行照搬灾厄 **2.1** 的 <c>Projectiles/Magic/NanoPurgeLaser.cs</c>
    /// （用户 2026-10-15 点名：手持弹幕取 2.0.4、这颗激光取 2.1 的重做版）。
    /// <para>
    /// 机制：本体是一根**会伸缩的光束条**（贴图是工程共用的 6×10 白条 `LaserProjectile`，
    /// 靠 <c>DrawBeam</c> 沿速度盖印成长条）——出场时 <c>localAI[0]</c> 每帧涨 1.5 直到 40
    /// （`ai[1]` 置 1 时反向缩回、缩到 0 自毁）。每 2 帧在身后撒一枚 <see cref="CustomSpark"/>
    /// 火花、撞墙时撒两枚光晕粒子与 5 颗彩色尘。
    /// </para>
    /// <para>
    /// **与 2.0.x 版本的实质差别**：撞墙后会**朝准星最近的敌人预测反弹**（40 格内找目标，
    /// 用 <see cref="CDUtil.PredictiveAimToTarget"/> 算提前量；找不到目标就走普通镜面反弹），
    /// 且反弹后**伤害打 8 折**（`HasBounced` 保证只智能反弹一次，第二次撞墙就正常消失）。
    /// </para>
    /// <para>
    /// **两处工程侧适配**：① 源读 <c>owner.ClampedMouseWorld()</c>，本工程按既有口径换成跨端可见的
    /// <see cref="CalamityDemutationPlayer.GetMouseWorld"/>；② 源用灾厄的 `LightDust` 发光点尘，
    /// 本工程没有这个尘，改用原版同族的 `DustID.FireworksRGB`（一样吃 `dust.color` 赋值）。
    /// 另：源的预测瞄准速度直接读 <c>owner.HeldItem.shootSpeed</c>，本工程加了一道"手持件速度非正时
    /// 退回弹幕自身速度"的保护（防止切武器那一瞬间算出 NaN 速度）。
    /// </para>
    /// </summary>
    internal class NanoPurgeLaser:ModProjectile
    {
        /// <summary>光束的最大长度（照源 40）</summary>
        private const float LaserLength = 40f;
        /// <summary>每帧的长度变化量（照源 1.5）</summary>
        private const float LaserLengthChangeRate = 1.5f;
        /// <summary>反弹时的索敌半径：40 格（照源 640）</summary>
        private const float BounceSearchDistance = 640f;
        /// <summary>是否已经用过那次"智能反弹"（照源用普通字段记状态）</summary>
        public bool HasBounced = false;
        /// <summary>撞墙后的短暂"不画光束"计时器（照源 10 帧）</summary>
        public int bounceTimer = 0;
        /// <summary>光束条贴图走工程共用的 `LaserProjectile`（照源同样借灾厄的共用贴图）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>拜月教教徒对它抗性更高（照 2.1 新增的这条集合标记）</summary>
        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        /// <summary>判定 5×5、穿透 1、额外更新 2 帧、寿命 600 帧、法师伤害（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Magic;
        }
        /// <summary>淡入、发光、长度伸缩、尾迹火花（照源，一行不动）</summary>
        public override void AI()
        {
            // 极快地淡入
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 25;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }

            Lighting.AddLight(Projectile.Center, 0f, 0.7f, 0.1f);

            // 长度伸缩：localAI[0] 是当前长度，ai[1] 为 0 表示还在变长、为 1 表示正在消散
            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += LaserLengthChangeRate;
                if (Projectile.localAI[0] > LaserLength)
                {
                    Projectile.localAI[0] = LaserLength;
                }
            }
            else
            {
                Projectile.localAI[0] -= LaserLengthChangeRate;
                if (Projectile.localAI[0] <= 0f)
                {
                    Projectile.Kill();
                }
            }

            // 每 2 帧在身后撒一枚青柠色火花
            if (Projectile.timeLeft % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, -Projectile.velocity * 0.05f, "CalamityDemutation/Particles/GlowSpark", false, 4, 0.02f, Color.Lime, new Vector2(0.6f, 1.4f), true, false, shrinkSpeed: 0.95f));
            }
            if (bounceTimer > 0)
            {
                bounceTimer--;
            }
        }
        /// <summary>
        /// 撞墙处理（照源）：先撒两枚光晕粒子与 5 颗彩色尘并让光束短暂隐去；
        /// 第一次撞墙（<see cref="HasBounced"/> 为假）时朝 40 格内离准星最近的目标**预测反弹**、
        /// 伤害乘 0.8 并返回 false（不退场）；第二次撞墙返回 true 让它正常消失。
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bounceTimer = 10;
            for (int i = 0; i < 2; i++)
            {
                Color color = i == 0 ? Color.White : Color.Lime;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, oldVelocity * 0.1f, "CalamityDemutation/Particles/BloomCircle", false, 15, Main.rand.NextFloat(0.15f, 0.25f) * (i == 0 ? 2 : 3), color, new Vector2(0.7f, 1.1f), true, false));
            }
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, Vector2.One.RotatedByRandom(100) * Main.rand.NextFloat(0.8f, 2.5f));
                dust.noGravity = false;
                dust.scale = Main.rand.NextFloat(0.45f, 1.1f);
                dust.color = Color.Lime;
                dust.noLightEmittence = true;
            }

            Player owner = Main.player[Projectile.owner];

            if (!HasBounced)
            {
                HasBounced = true;

                // 在 40 格内找离玩家准星最近的可追踪目标
                float npcDistCheck = BounceSearchDistance;
                int index = -1;
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (!n.CanBeChasedBy(Projectile))
                    {
                        continue;
                    }
                    float currentNPCDist = Vector2.Distance(n.Center, owner.GetModPlayer<CalamityDemutationPlayer>().GetMouseWorld());
                    if (currentNPCDist < npcDistCheck)
                    {
                        npcDistCheck = currentNPCDist;
                        index = n.whoAmI;
                    }
                }
                // 有目标就朝它未来位置打，没有就走普通镜面反弹（照源）
                if (index != -1)
                {
                    float predictiveSpeed = owner.HeldItem.shootSpeed > 0f ? owner.HeldItem.shootSpeed : Projectile.velocity.Length();
                    Projectile.velocity = CDUtil.PredictiveAimToTarget(Projectile.Center, Main.npc[index].Center, Main.npc[index].velocity, predictiveSpeed, 3);
                }
                else
                {
                    if (Projectile.velocity.X != oldVelocity.X)
                    {
                        Projectile.velocity.X = -oldVelocity.X;
                    }
                    if (Projectile.velocity.Y != oldVelocity.Y)
                    {
                        Projectile.velocity.Y = -oldVelocity.Y;
                    }
                }

                // 反弹后伤害打 8 折（照源）
                Projectile.damage = (int)(Projectile.damage * 0.8f);
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>本体恒为青绿色（照源 <c>(96, 255, 96, 0)</c>）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(96, 255, 96, 0);
        /// <summary>撞墙后的 10 帧里不画光束（照源），其余时候走 <c>DrawBeam</c> 盖印成长条</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (bounceTimer == 0)
            {
                Projectile.DrawBeam(LaserLength, 2f, lightColor);
            }
            return false;
        }
        /// <summary>消散时撒一枚火花 + 3 颗顺着速度方向散的彩色尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Projectile.velocity * 0.3f, "CalamityDemutation/Particles/GlowSpark", false, 14, 0.018f, Color.Lime, new Vector2(1f, 1f), true, false, shrinkSpeed: 0.65f));
            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.4f, 0.9f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.45f, 1.1f);
                dust.color = Color.Lime;
                dust.noLightEmittence = true;
            }
        }
    }
}
