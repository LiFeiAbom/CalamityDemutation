using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 泡泡（Typhoon Bubble） - 台风箭消亡时留下的过渡件，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/TyphoonBubble.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 30×30、穿透 -1、**<c>timeLeft</c> 只有 1 帧**（靠 AI 自己决定何时收场）、3 帧动画；
    /// 贴图**借用**灾厄的 <c>Projectiles/Melee/BrinyTyphoonBubble.png</c>（本工程已拷进同路径）。
    /// </para>
    /// <para>
    /// 两段式行为：<c>ai[1] == 0</c> 时先"摇头"飞行（每帧按余弦微调 Y 速度）；
    /// <c>ai[1] &gt; 0</c> 时掉头朝 <c>ai[1]-1</c> 号玩家飞、并在 50 像素内自毁（照源）。
    /// 消亡播 `NPCDeath19`、撒 36 颗 <see cref="DustID.DungeonWater"/> 水尘，
    /// 并在主人端托出一根水柱（<see cref="WaterSpout"/>）。
    /// </para>
    /// <para>
    /// **照源保留的一处死分支**：源在 <c>localAI[1] == 1f</c> 时改用 <see cref="SeasSearingSpout"/>（带硫磺毒），
    /// 但 2.0 全工程没有任何地方给泡泡写过 <c>localAI[1]</c> ⇒ 实际永远走 <see cref="WaterSpout"/>。
    /// 本工程照源两个分支都留着（那也正是 <see cref="SeasSearingSpout"/> 存在的唯一理由）。
    /// </para>
    /// </summary>
    internal class TyphoonBubble : ModProjectile
    {
        /// <summary>动画帧数（照源 3）</summary>
        private const int AnimationFrames = 3;
        /// <summary>飞行阶段的摇头角速度（照源 <c>0.209439516f</c>）</summary>
        private const float WobbleRate = 0.209439516f;
        /// <summary>摇头幅度（照源 4）</summary>
        private const float WobbleAmplitude = 4f;
        /// <summary>追玩家阶段的基准速度（照源 4，另加 <c>localAI[0] / 20</c>）</summary>
        private const float ChaseSpeed = 4f;
        /// <summary>追到多近就自毁（照源 50 像素）</summary>
        private const float ChaseShutdownDistance = 50f;
        /// <summary>每轮摇头 / 追人时撒的水尘数（照源 6）</summary>
        private const int TravelDustCount = 6;
        /// <summary>消亡时撒的水尘数（照源 36）</summary>
        private const int BurstDustCount = 36;
        /// <summary>水柱生成时的初参：<c>ai[0] = 16</c>（链条长度）、<c>ai[1] = 8</c>（起始档位）（照源）</summary>
        private const float SpoutInitialTimer = 16f;
        private const float SpoutInitialScaleStep = 8f;
        /// <summary>借用灾厄的泡泡贴图（本工程拷到同相对路径，见类型注释）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/BrinyTyphoonBubble";
        /// <summary>三帧动画（照源）</summary>
        public override void SetStaticDefaults() => Main.projFrames[Type] = AnimationFrames;
        /// <summary>基础属性：30×30 判定、穿透 -1、初始全透明、<c>timeLeft</c> 仅 1 帧（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1;
        }
        /// <summary>两段式行为 + 入水即炸（照源；改动只在把尘编号写成命名常量）</summary>
        public override void AI()
        {
            if (Projectile.ai[1] > 0f)
            {
                // 第二段：掉头朝玩家飞，靠近后自毁
                int chasePlayerIndex = (int)Projectile.ai[1] - 1;
                if (chasePlayerIndex < 255)
                {
                    Projectile.localAI[0] += 1f;
                    if (Projectile.localAI[0] > 10f)
                    {
                        for (int i = 0; i < TravelDustCount; i++)
                        {
                            Vector2 ringPos = Vector2.Normalize(Projectile.velocity) * new Vector2(Projectile.width / 2f, Projectile.height) * 0.75f;
                            ringPos = ringPos.RotatedBy((i - (TravelDustCount / 2 - 1)) * MathHelper.Pi / TravelDustCount) + Projectile.Center;
                            Vector2 scatter = ((float)(Main.rand.NextDouble() * Math.PI) - MathHelper.PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                            int dust = Dust.NewDust(ringPos + scatter, 0, 0, DustID.DungeonWater, scatter.X * 2f, scatter.Y * 2f, 100, default, 1.4f);
                            Main.dust[dust].noGravity = true;
                            Main.dust[dust].noLight = true;
                            Main.dust[dust].velocity /= 4f;
                            Main.dust[dust].velocity -= Projectile.velocity;
                        }
                        Projectile.alpha -= 5;
                        if (Projectile.alpha < 100)
                            Projectile.alpha = 100;
                        Projectile.rotation += Projectile.velocity.X * 0.1f;
                        Projectile.frame = (int)(Projectile.localAI[0] / 3f) % AnimationFrames;
                    }

                    Vector2 toPlayer = Main.player[chasePlayerIndex].Center - Projectile.Center;
                    float speed = ChaseSpeed + Projectile.localAI[0] / 20f;
                    Projectile.velocity = Vector2.Normalize(toPlayer) * speed;
                    if (toPlayer.Length() < ChaseShutdownDistance)
                        Projectile.Kill();
                }
            }
            else
            {
                // 第一段：摇头飞行
                float offset = (float)(Math.Cos(WobbleRate * Projectile.ai[0]) - 0.5) * WobbleAmplitude;
                Projectile.velocity.Y -= offset;
                Projectile.ai[0] += 1f;
                offset = (float)(Math.Cos(WobbleRate * Projectile.ai[0]) - 0.5) * WobbleAmplitude;
                Projectile.velocity.Y += offset;

                Projectile.localAI[0] += 1f;
                if (Projectile.localAI[0] > 10f)
                {
                    Projectile.alpha -= 5;
                    if (Projectile.alpha < 100)
                        Projectile.alpha = 100;
                    Projectile.rotation += Projectile.velocity.X * 0.1f;
                    Projectile.frame = (int)(Projectile.localAI[0] / 3f) % AnimationFrames;
                }
            }

            // 入水即爆（照源：先把位置抬高 16 像素再 Kill）
            if (Projectile.wet)
            {
                Projectile.position.Y -= 16f;
                Projectile.Kill();
            }
        }
        /// <summary>消亡：水花音 + 36 颗水尘 + 主人端托出一根水柱（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath19, Projectile.position);

            for (int i = 0; i < BurstDustCount; i++)
            {
                Vector2 ringPos = Vector2.Normalize(Projectile.velocity) * new Vector2(Projectile.width / 2f, Projectile.height) * 0.75f;
                ringPos = ringPos.RotatedBy((i - (BurstDustCount / 2 - 1)) * MathHelper.TwoPi / BurstDustCount) + Projectile.Center;
                Vector2 outward = ringPos - Projectile.Center;
                int dust = Dust.NewDust(ringPos + outward, 0, 0, DustID.DungeonWater, outward.X * 2f, outward.Y * 2f, 100, default, 1.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = true;
                Main.dust[dust].velocity = outward;
            }

            if (Projectile.owner != Main.myPlayer || Projectile.ai[1] >= 1f)
                return;

            // 源在此分两支：localAI[1] == 1 时用带硫磺毒的灼海喷流，否则用普通水柱（2.0 里前者永远触发不到）
            int spoutType = Projectile.localAI[1] == 1f
                ? ModContent.ProjectileType<SeasSearingSpout>()
                : ModContent.ProjectileType<WaterSpout>();
            int spout = Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                Projectile.Center.X - Projectile.direction * 30, Projectile.Center.Y - 4f,
                -Projectile.direction * 0.01f, 0f, spoutType, Projectile.damage, 3f, Projectile.owner,
                SpoutInitialTimer, SpoutInitialScaleStep);
            if (Main.projectile.IndexInRange(spout))
                Main.projectile[spout].netUpdate = true;
        }
    }
}
