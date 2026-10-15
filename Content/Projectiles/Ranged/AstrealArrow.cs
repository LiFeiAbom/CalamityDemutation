using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 星穹箭（Astreal Arrow） - 星穹悲愿射出的箭，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/AstrealArrow.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 18×18（贴图 18×44）、初始全透明、额外更新 1 帧、穿透 1、寿命 600 帧、算作"箭"、不受水阻；
    /// 10 帧残影（模式 0、抽稀 1）。
    /// **明灭脉动**：先每帧 `scale -= 0.02` 同时 `alpha += 30`（缩着淡出到看不见），
    /// 再反过来每帧 `scale += 0.02`／`alpha -= 30` 淡入回来，如此循环（源用 <c>localAI[0]</c> 记相位）。
    /// **加速**：只要速度还不到 16，就按出手时随机到的 <c>ai[0]</c>（0~3）继续加速——
    /// 0 = X 速度 ×1.02、1 = Y 速度 ×1.02、2 = X + 0.1 且 Y ×1.01、3 = Y + 0.1 且 X ×1.01。
    /// 每 1/5 帧撒一颗暗影束尘。
    /// </para>
    /// <para>
    /// **喷火（tooltip 里那句"飞行途中喷出火焰"）**：<c>ai[1]</c> 每帧累加 1~2，累计到 135 就在原地
    /// 生成一枚 <see cref="AstrealFlame"/>（伤害 ×0.5），然后清零并 `netUpdate`；只有主人端生成。
    /// </para>
    /// <para>
    /// 命中挂**暗影烈焰 180 帧**；消亡撒 5 颗暗影束尘。
    /// 与源的一点差别：源的 <c>pointBlankShotDuration</c>（灾厄贴脸射击）本工程不做。
    /// </para>
    /// </summary>
    internal class AstrealArrow : ModProjectile
    {
        /// <summary>残影缓存帧数（照源 10）</summary>
        private const int TrailCacheLength = 10;
        /// <summary>超过这个速度就不再加速（照源 16）</summary>
        private const float SpeedUpThreshold = 16f;
        /// <summary>喷火的计时阈值（照源 <c>ai[1] &gt;= 135f</c>）</summary>
        private const float FlameSpawnThreshold = 135f;
        /// <summary>喷出的烈焰的伤害倍率（照源 ×0.5）</summary>
        private const float FlameDamageMult = 0.5f;
        /// <summary>尾迹尘的触发频率（照源 <c>Main.rand.NextBool(5)</c>）</summary>
        private const int TrailDustChance = 5;
        /// <summary>消亡时的尘埃枚数（照源 5）</summary>
        private const int DeathDustCount = 5;
        /// <summary>命中挂暗影烈焰的帧数（照源 180）</summary>
        private const int ShadowFlameFrames = 180;
        /// <summary>明灭脉动：每帧的缩放变化量与 alpha 变化量（照源 0.02 / 30）</summary>
        private const float PulseScaleStep = 0.02f;
        private const int PulseAlphaStep = 30;
        /// <summary>脉动相位：0 = 正在淡出、1 = 正在淡入（源 <c>localAI[0]</c>）</summary>
        private ref float PulsePhase => ref Projectile.localAI[0];
        /// <summary>喷火计时器（源 <c>ai[1]</c>）</summary>
        private ref float FlameTimer => ref Projectile.ai[1];
        /// <summary>残影：缓存 10 帧、模式 0（普通渐隐，配合下面的 <c>PreDraw</c>）——照源</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailCacheLength;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>
        /// 基础属性：18×18 判定、初始全透明、额外更新 1 帧、穿透 1、寿命 600 帧、算作"箭"、不受水阻（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.alpha = 255;
            Projectile.ignoreWater = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
        }
        /// <summary>
        /// AI：摆正贴图 → 明灭脉动 → 按 <c>ai[0]</c> 继续加速 → 撒尾迹尘 →
        /// 累计到阈值就喷一枚 <see cref="AstrealFlame"/>（照源）
        /// </summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (PulsePhase == 0f)
            {
                Projectile.scale -= PulseScaleStep;
                Projectile.alpha += PulseAlphaStep;
                if (Projectile.alpha >= 250)
                {
                    Projectile.alpha = 255;
                    PulsePhase = 1f;
                }
            }
            else if (PulsePhase == 1f)
            {
                Projectile.scale += PulseScaleStep;
                Projectile.alpha -= PulseAlphaStep;
                if (Projectile.alpha <= 0)
                {
                    Projectile.alpha = 0;
                    PulsePhase = 0f;
                }
            }

            if (Projectile.velocity.Length() < SpeedUpThreshold)
            {
                switch ((int)Projectile.ai[0])
                {
                    case 0:
                        Projectile.velocity.X *= 1.02f;
                        break;
                    case 1:
                        Projectile.velocity.Y *= 1.02f;
                        break;
                    case 2:
                        Projectile.velocity.X += 0.1f;
                        Projectile.velocity.Y *= 1.01f;
                        break;
                    case 3:
                        Projectile.velocity.Y += 0.1f;
                        Projectile.velocity.X *= 1.01f;
                        break;
                    default:
                        break;
                }
            }

            if (Main.rand.NextBool(TrailDustChance))
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height,
                    DustID.ShadowbeamStaff, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            }

            FlameTimer += Main.rand.Next(2) + 1;
            if (FlameTimer >= FlameSpawnThreshold)
            {
                FlameTimer = 0f;
                Projectile.netUpdate = true;
                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity,
                        ModContent.ProjectileType<AstrealFlame>(), (int)(Projectile.damage * FlameDamageMult), Projectile.knockBack, Projectile.owner);
                }
            }
        }
        /// <summary>自绘：按残影缓存画一串位置残影（照源用灾厄的 DrawAfterimagesCentered，本工程用同名移植件）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>消亡：撒 5 颗暗影束尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < DeathDustCount; i++)
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height,
                    DustID.ShadowbeamStaff, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
            }
        }
        /// <summary>命中：挂暗影烈焰 180 帧（照源，走原版减益）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, ShadowFlameFrames);
        }
    }
}
